using ECommerce.Contracts.Grains;
using ECommerce.Contracts.Models;
using ECommerce.Grains.States;
using Orleans.Runtime;

namespace ECommerce.Grains;

/// <summary>
/// IMPLEMENTACJA MATERIALIZED VIEW - Indeks Produktów
/// 
/// WZORZEC INDEKSOWANIA (INDEX PATTERN):
/// ====================================
/// 
/// W klasycznym podejściu:
/// - Full-text search w bazie: CREATE INDEX idx_product_name ON products(name)
/// - Elasticsearch jako osobny system
/// - Wymaga synchronizacji z bazą główną
/// - Opóźnienie w aktualizacji indeksu
/// 
/// W Orleans:
/// - Grain jako indeks (shard per category)
/// - Klucz grainu = nazwa kategorii (np. "Electronics", "Books")
/// - Aktualizacja w czasie rzeczywistym
/// - Nie wymaga osobnego systemu
/// 
/// SHARDING INDEKSU:
/// - IProductIndexGrain.GetGrain("Electronics") → indeks elektroniki
/// - IProductIndexGrain.GetGrain("Books") → indeks książek
/// - Każdy indeks to osobny grain = naturalne partycjonowanie
/// 
/// MATERIALIZED VIEW IMPLEMENTATION - Product Index
/// 
/// INDEX PATTERN:
/// =============
/// 
/// In classic approach:
/// - Full-text search in database: CREATE INDEX idx_product_name ON products(name)
/// - Elasticsearch as separate system
/// - Requires synchronization with main database
/// - Delay in index updates
/// 
/// In Orleans:
/// - Grain as index (shard per category)
/// - Grain key = category name (e.g. "Electronics", "Books")
/// - Real-time updates
/// - No separate system required
/// 
/// INDEX SHARDING:
/// - IProductIndexGrain.GetGrain("Electronics") → electronics index
/// - IProductIndexGrain.GetGrain("Books") → books index
/// - Each index is a separate grain = natural partitioning
/// </summary>
public class ProductIndexGrain : Grain<ProductIndexState>, IProductIndexGrain
{
    public async Task AddProductAsync(ProductInfo product)
    {
        // AKTUALIZACJA INDEKSU W CZASIE RZECZYWISTYM
        // REAL-TIME INDEX UPDATE
        
        // UWAGA: Ten grain jest shardowany po kategorii (klucz = nazwa kategorii)
        // Aplikacja powinna wywoływać odpowiedni grain dla kategorii produktu
        // 
        // NOTE: This grain is sharded by category (key = category name)
        // Application should call the appropriate grain for product's category
        // 
        // Przykład / Example:
        // var categoryGrain = GrainFactory.GetGrain<IProductIndexGrain>("Electronics");
        // await categoryGrain.AddProductAsync(product);
        
        var entry = new ProductIndexEntry
        {
            ProductId = product.ProductId,
            Name = product.Name,
            Description = product.Description,
            RetailPrice = product.RetailPrice,
            WholesalePrice = product.WholesalePrice,
            StockQuantity = product.StockQuantity
        };

        State.Products[product.ProductId] = entry;
        State.LastUpdated = DateTime.UtcNow;

        await WriteStateAsync();
    }

    public async Task RemoveProductAsync(Guid productId)
    {
        State.Products.Remove(productId);
        State.LastUpdated = DateTime.UtcNow;

        await WriteStateAsync();
    }

    public Task<List<ProductInfo>> SearchAsync(string query, int maxResults)
    {
        // WYSZUKIWANIE W INDEKSIE (IN-MEMORY)
        // INDEX SEARCH (IN-MEMORY)
        
        // W klasycznym podejściu: SELECT * FROM products WHERE name LIKE '%query%'
        // In classic approach: SELECT * FROM products WHERE name LIKE '%query%'
        
        var results = State.Products.Values
            .Where(p =>
                p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(maxResults)
            .Select(ToProductInfo)
            .ToList();

        return Task.FromResult(results);
    }

    public Task<List<ProductInfo>> GetAllAsync()
    {
        var results = State.Products.Values
            .Select(ToProductInfo)
            .ToList();

        return Task.FromResult(results);
    }

    public Task<List<ProductInfo>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice)
    {
        // FILTROWANIE PO CENIE (bez zapytania do bazy!)
        // PRICE FILTERING (without database query!)
        
        var results = State.Products.Values
            .Where(p => p.RetailPrice >= minPrice && p.RetailPrice <= maxPrice)
            .Select(ToProductInfo)
            .ToList();

        return Task.FromResult(results);
    }

    private static ProductInfo ToProductInfo(ProductIndexEntry entry)
    {
        return new ProductInfo
        {
            ProductId = entry.ProductId,
            Name = entry.Name,
            Description = entry.Description,
            RetailPrice = entry.RetailPrice,
            WholesalePrice = entry.WholesalePrice,
            StockQuantity = entry.StockQuantity
        };
    }
}
