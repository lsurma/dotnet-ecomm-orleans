using ECommerce.Contracts.Models;

namespace ECommerce.Contracts.Grains;

/// <summary>
/// ORLEANS GRAIN INTERFACE - Katalog produktów (dla operacji na listach)
/// Product Catalog Grain Interface (for batch operations)
/// 
/// W klasycznym podejściu .NET:
/// - Jeden serwis ProductService z metodą GetProducts(List<Guid> ids)
/// - Jedno zapytanie do bazy: SELECT * FROM Products WHERE Id IN (...)
/// - Cache całej listy w Redis
/// 
/// W Orleans - PODEJŚCIE 1 (NAIWNE - WOLNE):
/// - Pętla po ID produktów, dla każdego GetGrain() i GetInfoAsync()
/// - N wywołań zdalnych (N-network calls)
/// - Wolne dla dużych list!
/// 
/// W Orleans - PODEJŚCIE 2 (OPTYMALNE - TO CO TUTAJ):
/// - Grain katalogowy przechowuje listę/indeks produktów
/// - FAN-OUT PATTERN: równoległe wywołania do wielu grainów (Task.WhenAll)
/// - Grain reference caching - unikamy wielokrotnego GetGrain()
/// 
/// IN CLASSIC .NET APPROACH:
/// - One ProductService with GetProducts(List<Guid> ids) method
/// - One database query: SELECT * FROM Products WHERE Id IN (...)
/// - Cache entire list in Redis
/// 
/// IN ORLEANS - APPROACH 1 (NAIVE - SLOW):
/// - Loop over product IDs, for each GetGrain() and GetInfoAsync()
/// - N remote calls (N-network calls)
/// - Slow for large lists!
/// 
/// IN ORLEANS - APPROACH 2 (OPTIMAL - THIS ONE):
/// - Catalog grain stores product list/index
/// - FAN-OUT PATTERN: parallel calls to many grains (Task.WhenAll)
/// - Grain reference caching - avoid repeated GetGrain()
/// </summary>
public interface IProductCatalogGrain : IGrainWithGuidKey
{
    /// <summary>
    /// Dodaje produkt do katalogu
    /// Adds product to catalog
    /// </summary>
    Task AddProductAsync(Guid productId);

    /// <summary>
    /// Pobiera listę wszystkich produktów (IDs)
    /// Gets list of all product IDs
    /// </summary>
    Task<List<Guid>> GetAllProductIdsAsync();

    /// <summary>
    /// OPTYMALIZACJA: Pobiera wiele produktów równolegle (FAN-OUT PATTERN)
    /// Zamiast N wywołań sekwencyjnych, wykonuje je wszystkie równocześnie
    /// 
    /// OPTIMIZATION: Fetches multiple products in parallel (FAN-OUT PATTERN)
    /// Instead of N sequential calls, executes them all concurrently
    /// </summary>
    Task<List<ProductInfo>> GetProductsAsync(List<Guid> productIds);

    /// <summary>
    /// Wyszukuje produkty spełniające warunki
    /// Searches for products matching criteria
    /// </summary>
    Task<List<ProductInfo>> SearchProductsAsync(string searchTerm, int maxResults = 20);
}
