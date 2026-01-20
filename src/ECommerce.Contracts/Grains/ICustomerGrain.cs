using ECommerce.Contracts.Models;

namespace ECommerce.Contracts.Grains;

/// <summary>
/// ORLEANS GRAIN INTERFACE - Interfejs dla Customer Grain
/// 
/// W klasycznym podejściu .NET:
/// - Byłby to zwykły serwis (np. CustomerService) wstrzykiwany przez DI
/// - Każde wywołanie metody działałoby na tym samym obiekcie współdzielonym przez wiele żądań
/// - Wymagałoby synchronizacji dostępu do zasobów (lock, SemaphoreSlim)
/// 
/// W Orleans:
/// - Grain to aktor - każdy klient ma swoją WŁASNĄ instancję tego grainu
/// - Orleans gwarantuje, że w danym momencie tylko jedno żądanie obsługuje dany grain
/// - Nie trzeba się martwić o synchronizację - Orleans to załatwia za nas
/// - Grain może być aktywny na różnych serwerach w klastrze
/// 
/// IN CLASSIC .NET APPROACH:
/// - This would be a regular service (e.g. CustomerService) injected via DI
/// - Each method call would operate on the same object shared across many requests
/// - Would require synchronization of access to resources (lock, SemaphoreSlim)
/// 
/// IN ORLEANS:
/// - Grain is an actor - each customer has their OWN instance of this grain
/// - Orleans guarantees that only one request processes a given grain at a time
/// - No need to worry about synchronization - Orleans handles it for us
/// - Grain can be active on different servers in the cluster
/// </summary>
public interface ICustomerGrain : IGrainWithGuidKey
{
    /// <summary>
    /// Tworzy nowego klienta / Creates a new customer
    /// </summary>
    Task CreateAsync(string name, string email, CustomerType customerType, string? taxId = null);

    /// <summary>
    /// Pobiera informacje o kliencie / Gets customer information
    /// </summary>
    Task<CustomerInfo?> GetInfoAsync();

    /// <summary>
    /// Aktualizuje dane klienta / Updates customer data
    /// </summary>
    Task UpdateAsync(string name, string email);
}
