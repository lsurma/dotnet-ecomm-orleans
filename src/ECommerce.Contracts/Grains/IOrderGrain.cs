using ECommerce.Contracts.Models;

namespace ECommerce.Contracts.Grains;

/// <summary>
/// ORLEANS GRAIN INTERFACE - Interfejs dla Order Grain
/// 
/// W klasycznym podejściu .NET:
/// - OrderService koordynujący transakcje między tabelami
/// - Wymagana transakcja bazodanowa lub Saga pattern dla spójności
/// - Trudna synchronizacja między serwisami (Customer, Product, Order)
/// - Problemy z współbieżnością przy wielu jednoczesnych zamówieniach
/// 
/// W Orleans:
/// - Grain reprezentuje jedno zamówienie
/// - Może wywoływać inne grainy (Customer, Product) bezpośrednio
/// - Orleans zapewnia spójność - jedno zamówienie = jeden grain = jedno wywołanie na raz
/// - Stan zamówienia jest automatycznie persystowany
/// - Naturalna implementacja workflow (pending -> confirmed -> shipped)
/// 
/// IN CLASSIC .NET APPROACH:
/// - OrderService coordinating transactions between tables
/// - Database transaction or Saga pattern required for consistency
/// - Difficult synchronization between services (Customer, Product, Order)
/// - Concurrency issues with multiple simultaneous orders
/// 
/// IN ORLEANS:
/// - Grain represents one order
/// - Can call other grains (Customer, Product) directly
/// - Orleans ensures consistency - one order = one grain = one call at a time
/// - Order state is automatically persisted
/// - Natural workflow implementation (pending -> confirmed -> shipped)
/// </summary>
public interface IOrderGrain : IGrainWithGuidKey
{
    /// <summary>
    /// Tworzy nowe zamówienie dla klienta
    /// Creates a new order for a customer
    /// </summary>
    Task CreateAsync(Guid customerId);

    /// <summary>
    /// Dodaje produkt do zamówienia
    /// Adds a product to the order
    /// </summary>
    Task<bool> AddItemAsync(Guid productId, int quantity);

    /// <summary>
    /// Potwierdza zamówienie (rezerwuje produkty w magazynie)
    /// Confirms the order (reserves products in warehouse)
    /// </summary>
    Task<bool> ConfirmAsync();

    /// <summary>
    /// Anuluje zamówienie (zwalnia zarezerwowane produkty)
    /// Cancels the order (releases reserved products)
    /// </summary>
    Task CancelAsync();

    /// <summary>
    /// Pobiera informacje o zamówieniu
    /// Gets order information
    /// </summary>
    Task<OrderInfo?> GetInfoAsync();

    /// <summary>
    /// Aktualizuje status zamówienia
    /// Updates order status
    /// </summary>
    Task UpdateStatusAsync(OrderStatus newStatus);
}
