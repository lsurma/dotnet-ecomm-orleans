using ECommerce.Contracts.Grains;
using ECommerce.Contracts.Models;
using ECommerce.Grains.States;
using Orleans.Runtime;

namespace ECommerce.Grains;

/// <summary>
/// Stan grainu ProductCatalog przechowywany w bazie danych
/// ProductCatalog grain state persisted in database
/// </summary>
[Serializable]
public class ProductCatalogState
{
    /// <summary>
    /// Lista wszystkich ID produktów w katalogu
    /// List of all product IDs in catalog
    /// </summary>
    public HashSet<Guid> ProductIds { get; set; } = new();
}

/// <summary>
/// IMPLEMENTACJA PRODUCT CATALOG GRAIN
/// 
/// Ten grain demonstruje KLUCZOWE OPTYMALIZACJE Orleans dla operacji na listach:
/// This grain demonstrates KEY Orleans OPTIMIZATIONS for list operations:
/// 
/// 1. FAN-OUT PATTERN (Wzorzec rozgałęzienia)
///    ========================================
///    Zamiast:                      Optymalizacja:
///    for each id                   var tasks = ids.Select(id => 
///      await GetInfo(id)              grain.GetInfo(id));
///                                   await Task.WhenAll(tasks)
///    
///    Czas: N * latency             Czas: 1 * latency (!)
///    
/// 2. GRAIN REFERENCE CACHING
///    ========================
///    Zamiast:                      Optymalizacja:
///    for each call                 var grain = GetGrain(id) // raz
///      var grain = GetGrain(id)    for each call
///      await grain.Method()           await grain.Method()
///    
/// 3. BATCH PROCESSING W GRAINIE
///    ===========================
///    Zamiast wielu wywołań HTTP do API, jedno wywołanie do grainu katalogowego,
///    który wewnętrznie robi fan-out do grainów produktów
///    
/// W klasycznym podejściu:
/// - SELECT * FROM Products WHERE Id IN (@ids) - jedno zapytanie SQL
/// - Jednak wymaga połączenia z bazą przy każdym żądaniu
/// 
/// W Orleans:
/// - Catalog grain trzyma indeks w pamięci
/// - Fan-out do product grainów (które mają stan w pamięci)
/// - Brak zapytań do bazy (stan już załadowany w grainach)
/// - Skaluje się horyzontalnie (każdy produkt może być na innym serwerze)
/// 
/// PRODUCT CATALOG GRAIN IMPLEMENTATION
/// 
/// This grain demonstrates KEY Orleans OPTIMIZATIONS for list operations:
/// 
/// 1. FAN-OUT PATTERN
///    ===============
///    Instead of:                   Optimization:
///    for each id                   var tasks = ids.Select(id => 
///      await GetInfo(id)              grain.GetInfo(id));
///                                   await Task.WhenAll(tasks)
///    
///    Time: N * latency             Time: 1 * latency (!)
///    
/// 2. GRAIN REFERENCE CACHING
///    =======================
///    Instead of:                   Optimization:
///    for each call                 var grain = GetGrain(id) // once
///      var grain = GetGrain(id)    for each call
///      await grain.Method()           await grain.Method()
///    
/// 3. BATCH PROCESSING IN GRAIN
///    ==========================
///    Instead of many HTTP calls to API, one call to catalog grain,
///    which internally does fan-out to product grains
///    
/// In classic approach:
/// - SELECT * FROM Products WHERE Id IN (@ids) - one SQL query
/// - However requires database connection on every request
/// 
/// In Orleans:
/// - Catalog grain keeps index in memory
/// - Fan-out to product grains (which have state in memory)
/// - No database queries (state already loaded in grains)
/// - Scales horizontally (each product can be on different server)
/// </summary>
public class ProductCatalogGrain : Grain<ProductCatalogState>, IProductCatalogGrain
{
    public async Task AddProductAsync(Guid productId)
    {
        State.ProductIds.Add(productId);
        await WriteStateAsync();
    }

    public Task<List<Guid>> GetAllProductIdsAsync()
    {
        return Task.FromResult(State.ProductIds.ToList());
    }

    public async Task<List<ProductInfo>> GetProductsAsync(List<Guid> productIds)
    {
        // ========================================
        // ANTI-PATTERN (WOLNE - NIE RÓB TEGO!)
        // ANTI-PATTERN (SLOW - DON'T DO THIS!)
        // ========================================
        // var result = new List<ProductInfo>();
        // foreach (var id in productIds)
        // {
        //     var grain = GrainFactory.GetGrain<IProductGrain>(id);
        //     var info = await grain.GetInfoAsync();  // Czekamy na każde wywołanie!
        //     if (info != null) result.Add(info);
        // }
        // return result;
        // 
        // Problem: Jeśli mamy 20 produktów i każde wywołanie zajmuje 10ms,
        //          to łącznie: 20 * 10ms = 200ms (!)
        // Problem: If we have 20 products and each call takes 10ms,
        //          total: 20 * 10ms = 200ms (!)

        // ========================================
        // OPTYMALIZACJA 1: GRAIN REFERENCE CACHING
        // Pobierz wszystkie referencje do grainów na raz
        // Get all grain references at once
        // ========================================
        var productGrains = productIds
            .Select(id => GrainFactory.GetGrain<IProductGrain>(id))
            .ToList();

        // ========================================
        // OPTYMALIZACJA 2: FAN-OUT PATTERN
        // Wywołaj wszystkie grainy RÓWNOLEGLE
        // Call all grains in PARALLEL
        // ========================================
        var tasks = productGrains
            .Select(grain => grain.GetInfoAsync())
            .ToList();

        // Task.WhenAll czeka na WSZYSTKIE zadania równocześnie
        // Task.WhenAll waits for ALL tasks concurrently
        var results = await Task.WhenAll(tasks);

        // Czas wykonania: max(wszystkie wywołania), nie suma!
        // Execution time: max(all calls), not sum!
        // Jeśli każde wywołanie = 10ms, to łącznie: ~10ms (nie 200ms!)
        // If each call = 10ms, total: ~10ms (not 200ms!)

        // Filtruj null'e (produkty które nie istnieją)
        // Filter nulls (products that don't exist)
        return results
            .Where(info => info != null)
            .Cast<ProductInfo>()
            .ToList();
    }

    public async Task<List<ProductInfo>> SearchProductsAsync(string searchTerm, int maxResults = 20)
    {
        // Krok 1: Pobierz wszystkie produkty (fan-out)
        // Step 1: Fetch all products (fan-out)
        var allProducts = await GetProductsAsync(State.ProductIds.ToList());

        // Krok 2: Filtruj w pamięci
        // Step 2: Filter in memory
        var filtered = allProducts
            .Where(p => 
                p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .Take(maxResults)
            .ToList();

        // UWAGA: To działa dla małych katalogów (setki produktów)
        // Dla dużych katalogów (tysiące+) potrzebny jest grain indeksujący
        // 
        // NOTE: This works for small catalogs (hundreds of products)
        // For large catalogs (thousands+) you need an indexing grain

        return filtered;
    }
}
