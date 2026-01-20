using ECommerce.Contracts.Models;

namespace ECommerce.Contracts.Grains;

/// <summary>
/// ORLEANS GRAIN INTERFACE - Interfejs dla Product Grain
/// 
/// W klasycznym podejściu .NET:
/// - ProductService z dostępem do bazy danych
/// - Każde zapytanie o produkt = zapytanie do bazy
/// - Cache wymagałby Redis lub MemoryCache + zarządzania invalidacją
/// 
/// W Orleans:
/// - Stan produktu jest trzymany w pamięci grainu
/// - Grain ładuje dane z bazy tylko raz (przy aktywacji)
/// - Orleans automatycznie usuwa nieaktywne grainy z pamięci
/// - Persystencja stanu jest automatyczna (zapis do bazy przy zmianach)
/// 
/// IN CLASSIC .NET APPROACH:
/// - ProductService with database access
/// - Each product query = database query
/// - Caching would require Redis or MemoryCache + invalidation management
/// 
/// IN ORLEANS:
/// - Product state is kept in grain memory
/// - Grain loads data from database only once (on activation)
/// - Orleans automatically removes inactive grains from memory
/// - State persistence is automatic (saves to database on changes)
/// </summary>
public interface IProductGrain : IGrainWithGuidKey
{
    /// <summary>
    /// Tworzy nowy produkt / Creates a new product
    /// </summary>
    Task CreateAsync(string name, string description, decimal retailPrice, decimal wholesalePrice, int initialStock);

    /// <summary>
    /// Pobiera informacje o produkcie / Gets product information
    /// </summary>
    Task<ProductInfo?> GetInfoAsync();

    /// <summary>
    /// Rezerwuje produkty dla zamówienia (zmniejsza stan magazynowy)
    /// Reserves products for an order (decreases stock)
    /// </summary>
    Task<bool> ReserveStockAsync(int quantity);

    /// <summary>
    /// Zwraca produkty do magazynu (np. przy anulowaniu zamówienia)
    /// Returns products to warehouse (e.g. when cancelling order)
    /// </summary>
    Task ReturnStockAsync(int quantity);

    /// <summary>
    /// Pobiera cenę dla danego typu klienta
    /// Gets price for given customer type
    /// </summary>
    Task<decimal> GetPriceForCustomerTypeAsync(CustomerType customerType);
}
