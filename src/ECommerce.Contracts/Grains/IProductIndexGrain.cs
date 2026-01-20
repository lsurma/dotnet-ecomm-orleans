using ECommerce.Contracts.Models;
using Orleans;

namespace ECommerce.Contracts.Grains;

/// <summary>
/// MATERIALIZED VIEW GRAIN - Indeks produktów dla wyszukiwania
/// Product Index Grain for Search (Materialized View)
/// 
/// WZORZEC INDEKSOWANIA W ORLEANS:
/// - Zamiast skanować wszystkie produkty przy każdym wyszukiwaniu
/// - Trzymamy indeksy (mapy: kategoria → produkty, cena → produkty)
/// - Aktualizujemy indeksy gdy produkty się zmieniają
/// 
/// W klasycznym podejściu .NET:
/// - Pełnotekstowe indeksy w bazie (Full-Text Search)
/// - Elasticsearch/Solr jako osobny system
/// - Wymaga synchronizacji z bazą główną
/// 
/// W Orleans:
/// - Grain jako indeks wyszukiwania
/// - Aktualizacja w czasie rzeczywistym
/// - Rozproszone indeksy (shard per category)
/// - Nie wymaga osobnego systemu
/// 
/// INDEX PATTERN IN ORLEANS:
/// - Instead of scanning all products on every search
/// - We keep indexes (maps: category → products, price → products)
/// - We update indexes when products change
/// 
/// In classic .NET approach:
/// - Full-text indexes in database (Full-Text Search)
/// - Elasticsearch/Solr as separate system
/// - Requires synchronization with main database
/// 
/// In Orleans:
/// - Grain as search index
/// - Real-time updates
/// - Distributed indexes (shard per category)
/// - No separate system required
/// </summary>
public interface IProductIndexGrain : IGrainWithStringKey
{
    /// <summary>
    /// Dodaj produkt do indeksu
    /// Add product to index
    /// </summary>
    Task AddProductAsync(ProductInfo product);

    /// <summary>
    /// Usuń produkt z indeksu
    /// Remove product from index
    /// </summary>
    Task RemoveProductAsync(Guid productId);

    /// <summary>
    /// Wyszukaj produkty w tej kategorii/indeksie
    /// Search products in this category/index
    /// </summary>
    Task<List<ProductInfo>> SearchAsync(string query, int maxResults);

    /// <summary>
    /// Pobierz wszystkie produkty w tej kategorii
    /// Get all products in this category
    /// </summary>
    Task<List<ProductInfo>> GetAllAsync();

    /// <summary>
    /// Pobierz produkty w zakresie cenowym
    /// Get products in price range
    /// </summary>
    Task<List<ProductInfo>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice);
}
