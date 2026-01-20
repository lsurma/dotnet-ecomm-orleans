using ECommerce.Contracts.Grains;
using ECommerce.Contracts.Models;

/// <summary>
/// ORLEANS CLIENT - API używające Orleans Client do komunikacji z grainami
/// 
/// W klasycznym podejściu .NET:
/// - Kontrolery używają wstrzykniętych serwisów (DI)
/// - Serwisy działają w tym samym procesie co API
/// - Każde żądanie tworzy nowy scope i instancje serwisów
/// 
/// W Orleans:
/// - API jest KLIENTEM Orleans
/// - Grainy mogą działać na innych serwerach w klastrze
/// - IClusterClient łączy się z Orleans Silo
/// - GrainFactory tworzy proxy do grainów (nie prawdziwe obiekty)
/// - Wywołania są zdalne (remote calls) ale wyglądają jak lokalne
/// 
/// IN CLASSIC .NET APPROACH:
/// - Controllers use injected services (DI)
/// - Services run in the same process as API
/// - Each request creates new scope and service instances
/// 
/// IN ORLEANS:
/// - API is an Orleans CLIENT
/// - Grains can run on other servers in the cluster
/// - IClusterClient connects to Orleans Silo
/// - GrainFactory creates proxies to grains (not actual objects)
/// - Calls are remote but look local
/// </summary>

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

// KONFIGURACJA ORLEANS CLIENT
// ORLEANS CLIENT CONFIGURATION
builder.Host.UseOrleansClient((context, clientBuilder) =>
{
    if (builder.Environment.IsDevelopment())
    {
        // W deweloperskim środowisku łączymy się z lokalnym Silo
        // In development environment connect to local Silo
        clientBuilder.UseLocalhostClustering();
    }
    else
    {
        // W produkcji łączymy się z klastrem przez AdoNet
        // In production connect to cluster via AdoNet
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
            ?? "Host=localhost;Database=ecommerce_orleans;Username=postgres;Password=postgres";
            
        clientBuilder.UseAdoNetClustering(options =>
        {
            options.Invariant = "Npgsql";
            options.ConnectionString = connectionString;
        });
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// ==================== CUSTOMER ENDPOINTS ====================

app.MapPost("/customers", async (IClusterClient client, CreateCustomerRequest request) =>
{
    // W klasycznym podejściu: _customerService.CreateAsync(...)
    // W Orleans: pobieramy grain i wywołujemy metodę
    // 
    // In classic approach: _customerService.CreateAsync(...)
    // In Orleans: get grain and call method
    
    var customerId = Guid.NewGuid();
    var customerGrain = client.GetGrain<ICustomerGrain>(customerId);
    
    await customerGrain.CreateAsync(
        request.Name, 
        request.Email, 
        request.CustomerType, 
        request.TaxId);
    
    var customerInfo = await customerGrain.GetInfoAsync();
    return Results.Created($"/customers/{customerId}", customerInfo);
})
.WithName("CreateCustomer")
.WithSummary("Tworzy nowego klienta (indywidualnego lub biznesowego)");

app.MapGet("/customers/{id:guid}", async (IClusterClient client, Guid id) =>
{
    var customerGrain = client.GetGrain<ICustomerGrain>(id);
    var customerInfo = await customerGrain.GetInfoAsync();
    
    return customerInfo != null 
        ? Results.Ok(customerInfo) 
        : Results.NotFound();
})
.WithName("GetCustomer")
.WithSummary("Pobiera informacje o kliencie");

// ==================== PRODUCT ENDPOINTS ====================

app.MapPost("/products", async (IClusterClient client, CreateProductRequest request) =>
{
    var productId = Guid.NewGuid();
    var productGrain = client.GetGrain<IProductGrain>(productId);
    
    await productGrain.CreateAsync(
        request.Name,
        request.Description,
        request.RetailPrice,
        request.WholesalePrice,
        request.InitialStock);
    
    var productInfo = await productGrain.GetInfoAsync();
    
    // Automatycznie dodaj do katalogu
    // Automatically add to catalog
    var catalogId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    var catalogGrain = client.GetGrain<IProductCatalogGrain>(catalogId);
    await catalogGrain.AddProductAsync(productId);
    
    return Results.Created($"/products/{productId}", productInfo);
})
.WithName("CreateProduct")
.WithSummary("Tworzy nowy produkt z różnymi cenami dla klientów indywidualnych i biznesowych");

app.MapGet("/products/{id:guid}", async (IClusterClient client, Guid id) =>
{
    var productGrain = client.GetGrain<IProductGrain>(id);
    var productInfo = await productGrain.GetInfoAsync();
    
    return productInfo != null 
        ? Results.Ok(productInfo) 
        : Results.NotFound();
})
.WithName("GetProduct")
.WithSummary("Pobiera informacje o produkcie");

// ==================== ORDER ENDPOINTS ====================

app.MapPost("/orders", async (IClusterClient client, CreateOrderRequest request) =>
{
    var orderId = Guid.NewGuid();
    var orderGrain = client.GetGrain<IOrderGrain>(orderId);
    
    await orderGrain.CreateAsync(request.CustomerId);
    
    var orderInfo = await orderGrain.GetInfoAsync();
    return Results.Created($"/orders/{orderId}", orderInfo);
})
.WithName("CreateOrder")
.WithSummary("Tworzy nowe zamówienie dla klienta");

app.MapPost("/orders/{id:guid}/items", async (IClusterClient client, Guid id, AddOrderItemRequest request) =>
{
    var orderGrain = client.GetGrain<IOrderGrain>(id);
    var success = await orderGrain.AddItemAsync(request.ProductId, request.Quantity);
    
    if (!success)
    {
        return Results.BadRequest("Could not add item to order");
    }
    
    var orderInfo = await orderGrain.GetInfoAsync();
    return Results.Ok(orderInfo);
})
.WithName("AddOrderItem")
.WithSummary("Dodaje produkt do zamówienia");

app.MapPost("/orders/{id:guid}/confirm", async (IClusterClient client, Guid id) =>
{
    var orderGrain = client.GetGrain<IOrderGrain>(id);
    var success = await orderGrain.ConfirmAsync();
    
    if (!success)
    {
        return Results.BadRequest("Could not confirm order - possibly insufficient stock");
    }
    
    var orderInfo = await orderGrain.GetInfoAsync();
    return Results.Ok(orderInfo);
})
.WithName("ConfirmOrder")
.WithSummary("Potwierdza zamówienie (rezerwuje produkty w magazynie)");

app.MapPost("/orders/{id:guid}/cancel", async (IClusterClient client, Guid id) =>
{
    var orderGrain = client.GetGrain<IOrderGrain>(id);
    await orderGrain.CancelAsync();
    
    var orderInfo = await orderGrain.GetInfoAsync();
    return Results.Ok(orderInfo);
})
.WithName("CancelOrder")
.WithSummary("Anuluje zamówienie (zwalnia zarezerwowane produkty)");

app.MapGet("/orders/{id:guid}", async (IClusterClient client, Guid id) =>
{
    var orderGrain = client.GetGrain<IOrderGrain>(id);
    var orderInfo = await orderGrain.GetInfoAsync();
    
    return orderInfo != null 
        ? Results.Ok(orderInfo) 
        : Results.NotFound();
})
.WithName("GetOrder")
.WithSummary("Pobiera informacje o zamówieniu");

// ==================== CATALOG ENDPOINTS (BATCH OPERATIONS) ====================

/// <summary>
/// PRZYKŁAD: Pobieranie wielu produktów naraz
/// EXAMPLE: Fetching multiple products at once
/// 
/// To pokazuje różne podejścia do operacji na listach w Orleans
/// This shows different approaches to list operations in Orleans
/// </summary>

app.MapPost("/catalog/products/batch", async (IClusterClient client, GetProductsRequest request) =>
{
    // PODEJŚCIE 1 (NAIWNE - WOLNE): Sekwencyjne wywołania
    // APPROACH 1 (NAIVE - SLOW): Sequential calls
    // ----------------------------------------------------
    // var result = new List<ProductInfo>();
    // foreach (var id in request.ProductIds)
    // {
    //     var grain = client.GetGrain<IProductGrain>(id);
    //     var info = await grain.GetInfoAsync();
    //     if (info != null) result.Add(info);
    // }
    // Czas: N * latency (~200ms dla 20 produktów)
    // Time: N * latency (~200ms for 20 products)

    // PODEJŚCIE 2 (OPTYMALNE): Fan-out pattern bezpośrednio w API
    // APPROACH 2 (OPTIMAL): Fan-out pattern directly in API
    // ----------------------------------------------------
    var tasks = request.ProductIds
        .Select(id => client.GetGrain<IProductGrain>(id).GetInfoAsync())
        .ToList();
    
    var results = await Task.WhenAll(tasks);
    var products = results.Where(p => p != null).Cast<ProductInfo>().ToList();
    
    // Czas: max(wszystkie wywołania) (~10-20ms dla 20 produktów)
    // Time: max(all calls) (~10-20ms for 20 products)
    
    return Results.Ok(new { 
        count = products.Count,
        products = products 
    });
})
.WithName("GetProductsBatch")
.WithSummary("Pobiera wiele produktów naraz (fan-out pattern)");

app.MapPost("/catalog/products/batch-via-catalog", async (IClusterClient client, GetProductsRequest request) =>
{
    // PODEJŚCIE 3 (NAJLEPSZE dla dużych list): Przez grain katalogowy
    // APPROACH 3 (BEST for large lists): Via catalog grain
    // ----------------------------------------------------
    // Zamiast wielu wywołań HTTP -> API -> Grain
    // Jedno wywołanie HTTP -> API -> CatalogGrain -> Fan-out do ProductGrains
    //
    // Zalety:
    // - Mniej wywołań przez sieć (API <-> Silo)
    // - Katalog może cache'ować referencje
    // - Łatwiejsze testowanie i monitoring
    //
    // Instead of many HTTP calls -> API -> Grain
    // One HTTP call -> API -> CatalogGrain -> Fan-out to ProductGrains
    //
    // Benefits:
    // - Fewer network calls (API <-> Silo)
    // - Catalog can cache references
    // - Easier testing and monitoring
    
    var catalogId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Singleton catalog
    var catalogGrain = client.GetGrain<IProductCatalogGrain>(catalogId);
    var products = await catalogGrain.GetProductsAsync(request.ProductIds);
    
    return Results.Ok(new { 
        count = products.Count,
        products = products 
    });
})
.WithName("GetProductsBatchViaCatalog")
.WithSummary("Pobiera wiele produktów przez grain katalogowy (optymalne)");

app.MapGet("/catalog/products/all", async (IClusterClient client) =>
{
    var catalogId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    var catalogGrain = client.GetGrain<IProductCatalogGrain>(catalogId);
    
    var productIds = await catalogGrain.GetAllProductIdsAsync();
    var products = await catalogGrain.GetProductsAsync(productIds);
    
    return Results.Ok(new { 
        count = products.Count,
        products = products 
    });
})
.WithName("GetAllProducts")
.WithSummary("Pobiera wszystkie produkty z katalogu");

app.MapGet("/catalog/products/search", async (IClusterClient client, string q, int limit = 20) =>
{
    var catalogId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    var catalogGrain = client.GetGrain<IProductCatalogGrain>(catalogId);
    
    var products = await catalogGrain.SearchProductsAsync(q, limit);
    
    return Results.Ok(new { 
        count = products.Count,
        products = products,
        query = q
    });
})
.WithName("SearchProducts")
.WithSummary("Wyszukuje produkty (demo fan-out + filtrowanie)");

// Hook do dodawania produktu do katalogu
app.MapPost("/catalog/products/register", async (IClusterClient client, RegisterProductRequest request) =>
{
    var catalogId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    var catalogGrain = client.GetGrain<IProductCatalogGrain>(catalogId);
    await catalogGrain.AddProductAsync(request.ProductId);
    
    return Results.Ok(new { message = "Product added to catalog", productId = request.ProductId });
})
.WithName("RegisterProductInCatalog")
.WithSummary("Rejestruje produkt w katalogu");

app.Run();

// ==================== REQUEST DTOs ====================

record CreateCustomerRequest(string Name, string Email, CustomerType CustomerType, string? TaxId);
record CreateProductRequest(string Name, string Description, decimal RetailPrice, decimal WholesalePrice, int InitialStock);
record CreateOrderRequest(Guid CustomerId);
record AddOrderItemRequest(Guid ProductId, int Quantity);
record GetProductsRequest(List<Guid> ProductIds);
record RegisterProductRequest(Guid ProductId);

