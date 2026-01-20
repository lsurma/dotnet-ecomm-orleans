using ECommerce.Contracts.Models;

namespace ECommerce.Contracts.Grains;

/// <summary>
/// MATERIALIZED VIEW GRAIN - Statystyki zamówień
/// Order Statistics Materialized View Grain
/// 
/// MATERIALIZED VIEW to wzorzec, gdzie:
/// - Zamiast agregować dane za każdym razem (SELECT SUM, COUNT itp.)
/// - Trzymamy pre-obliczone wartości w osobnym grainie
/// - Aktualizujemy je gdy dane źródłowe się zmieniają
/// 
/// W klasycznym podejściu .NET:
/// - SQL Materialized View: CREATE MATERIALIZED VIEW order_stats AS SELECT...
/// - Cache z czasem wygaśnięcia (Redis TTL)
/// - Wymaga ręcznej invalidacji przy zmianach
/// 
/// W Orleans:
/// - Grain jako materialized view
/// - Automatyczna aktualizacja przy zmianach źródła
/// - Rozproszone pre-obliczenia
/// - Nie trzeba zapytań agregujących do bazy
/// 
/// MATERIALIZED VIEW is a pattern where:
/// - Instead of aggregating data every time (SELECT SUM, COUNT, etc.)
/// - We keep pre-computed values in a separate grain
/// - We update them when source data changes
/// 
/// In classic .NET approach:
/// - SQL Materialized View: CREATE MATERIALIZED VIEW order_stats AS SELECT...
/// - Cache with expiration (Redis TTL)
/// - Requires manual invalidation on changes
/// 
/// In Orleans:
/// - Grain as materialized view
/// - Automatic update when source changes
/// - Distributed pre-computations
/// - No need for aggregating queries to database
/// </summary>
public interface IOrderStatisticsGrain : IGrainWithGuidKey
{
    /// <summary>
    /// Aktualizuj statystyki po potwierdzeniu zamówienia
    /// Update statistics after order confirmation
    /// </summary>
    Task OnOrderConfirmedAsync(Guid orderId, Guid customerId, decimal orderTotal);

    /// <summary>
    /// Aktualizuj statystyki po anulowaniu zamówienia
    /// Update statistics after order cancellation
    /// </summary>
    Task OnOrderCancelledAsync(Guid orderId, Guid customerId, decimal orderTotal);

    /// <summary>
    /// Pobierz statystyki dla klienta (materialized view)
    /// Get customer statistics (materialized view)
    /// </summary>
    Task<CustomerStatistics> GetCustomerStatisticsAsync(Guid customerId);

    /// <summary>
    /// Pobierz globalne statystyki (materialized view)
    /// Get global statistics (materialized view)
    /// </summary>
    Task<GlobalStatistics> GetGlobalStatisticsAsync();
}

/// <summary>
/// Statystyki klienta (materialized view)
/// Customer statistics (materialized view)
/// </summary>
[GenerateSerializer]
public record CustomerStatistics
{
    [Id(0)]
    public Guid CustomerId { get; init; }
    
    [Id(1)]
    public int TotalOrders { get; init; }
    
    [Id(2)]
    public decimal TotalSpent { get; init; }
    
    [Id(3)]
    public decimal AverageOrderValue { get; init; }
    
    [Id(4)]
    public DateTime? LastOrderDate { get; init; }
}

/// <summary>
/// Globalne statystyki (materialized view)
/// Global statistics (materialized view)
/// </summary>
[GenerateSerializer]
public record GlobalStatistics
{
    [Id(0)]
    public int TotalOrders { get; init; }
    
    [Id(1)]
    public decimal TotalRevenue { get; init; }
    
    [Id(2)]
    public int TotalCustomers { get; init; }
    
    [Id(3)]
    public decimal AverageOrderValue { get; init; }
}
