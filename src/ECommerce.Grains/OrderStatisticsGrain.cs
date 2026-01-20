using ECommerce.Contracts.Grains;
using ECommerce.Contracts.Models;
using ECommerce.Grains.States;
using Orleans.Runtime;

namespace ECommerce.Grains;

/// <summary>
/// IMPLEMENTACJA MATERIALIZED VIEW - Statystyki Zamówień
/// 
/// WZORZEC MATERIALIZED VIEW W ORLEANS:
/// ====================================
/// 
/// 1. PROBLEM: Obliczanie statystyk za każdym razem jest kosztowne
///    - SELECT COUNT(*), SUM(total) FROM orders WHERE customer_id = ?
///    - Skanowanie całej tabeli zamówień
///    - Wolne dla dużych zbiorów danych
/// 
/// 2. ROZWIĄZANIE: Pre-obliczone wartości w osobnym grainie
///    - Stan grainu zawiera już zagregowane dane
///    - Aktualizacja przyrostowa (incremental update)
///    - Natychmiastowy dostęp bez zapytań agregujących
/// 
/// 3. AKTUALIZACJA WIDOKU:
///    - OrderGrain wywołuje OnOrderConfirmedAsync() po potwierdzeniu
///    - Przyrostowa aktualizacja: totalOrders++, totalSpent += amount
///    - Brak konieczności przebudowy całego widoku
/// 
/// PORÓWNANIE:
/// ===========
/// 
/// Klasyczne SQL:
/// CREATE MATERIALIZED VIEW order_stats AS
///   SELECT customer_id, COUNT(*) as total_orders, SUM(total) as total_spent
///   FROM orders GROUP BY customer_id;
/// REFRESH MATERIALIZED VIEW order_stats; // Kosztowne!
/// 
/// Orleans Materialized View:
/// - Przyrostowa aktualizacja przy każdej zmianie
/// - Brak konieczności REFRESH
/// - Skaluje się horyzontalnie (grain per customer)
/// 
/// MATERIALIZED VIEW IMPLEMENTATION - Order Statistics
/// 
/// MATERIALIZED VIEW PATTERN IN ORLEANS:
/// ====================================
/// 
/// 1. PROBLEM: Computing statistics every time is expensive
///    - SELECT COUNT(*), SUM(total) FROM orders WHERE customer_id = ?
///    - Scanning entire orders table
///    - Slow for large datasets
/// 
/// 2. SOLUTION: Pre-computed values in separate grain
///    - Grain state contains already aggregated data
///    - Incremental update
///    - Immediate access without aggregating queries
/// 
/// 3. VIEW UPDATE:
///    - OrderGrain calls OnOrderConfirmedAsync() after confirmation
///    - Incremental update: totalOrders++, totalSpent += amount
///    - No need to rebuild entire view
/// 
/// COMPARISON:
/// ===========
/// 
/// Classic SQL:
/// CREATE MATERIALIZED VIEW order_stats AS
///   SELECT customer_id, COUNT(*) as total_orders, SUM(total) as total_spent
///   FROM orders GROUP BY customer_id;
/// REFRESH MATERIALIZED VIEW order_stats; // Expensive!
/// 
/// Orleans Materialized View:
/// - Incremental update on each change
/// - No need for REFRESH
/// - Scales horizontally (grain per customer)
/// </summary>
public class OrderStatisticsGrain : Grain<OrderStatisticsState>, IOrderStatisticsGrain
{
    public async Task OnOrderConfirmedAsync(Guid orderId, Guid customerId, decimal orderTotal)
    {
        // AKTUALIZACJA PRZYROSTOWA - nie przebudowujemy całego widoku!
        // INCREMENTAL UPDATE - we don't rebuild the entire view!
        
        // Aktualizuj statystyki klienta
        // Update customer statistics
        if (!State.CustomerStatistics.ContainsKey(customerId))
        {
            State.CustomerStatistics[customerId] = new CustomerStats();
        }

        var customerStats = State.CustomerStatistics[customerId];
        customerStats.TotalOrders++;
        customerStats.TotalSpent += orderTotal;
        customerStats.LastOrderDate = DateTime.UtcNow;

        // Aktualizuj globalne statystyki
        // Update global statistics
        State.GlobalStatistics.TotalOrders++;
        State.GlobalStatistics.TotalRevenue += orderTotal;
        State.GlobalStatistics.UniqueCustomers.Add(customerId);

        // Zapisz zaktualizowany widok
        // Persist updated view
        await WriteStateAsync();
    }

    public async Task OnOrderCancelledAsync(Guid orderId, Guid customerId, decimal orderTotal)
    {
        // KOMPENSACJA - cofamy zmiany w widoku
        // COMPENSATION - rollback changes in view
        
        // Tylko aktualizuj jeśli klient istnieje w statystykach
        // Only update if customer exists in statistics
        if (State.CustomerStatistics.ContainsKey(customerId))
        {
            var customerStats = State.CustomerStatistics[customerId];
            
            // Zapobiegnij ujemnym wartościom
            // Prevent negative values
            if (customerStats.TotalOrders > 0)
            {
                customerStats.TotalOrders--;
                customerStats.TotalSpent -= orderTotal;
                
                // Aktualizuj globalne statystyki tylko jeśli zaktualizowano klienta
                // Update global statistics only if customer was updated
                if (State.GlobalStatistics.TotalOrders > 0)
                {
                    State.GlobalStatistics.TotalOrders--;
                    State.GlobalStatistics.TotalRevenue -= orderTotal;
                }
            }
        }

        await WriteStateAsync();
    }

    public Task<CustomerStatistics> GetCustomerStatisticsAsync(Guid customerId)
    {
        // ODCZYT Z MATERIALIZED VIEW - natychmiastowy, bez agregacji!
        // READ FROM MATERIALIZED VIEW - immediate, no aggregation!
        
        if (!State.CustomerStatistics.TryGetValue(customerId, out var stats))
        {
            return Task.FromResult(new CustomerStatistics
            {
                CustomerId = customerId,
                TotalOrders = 0,
                TotalSpent = 0,
                AverageOrderValue = 0
            });
        }

        var avgOrderValue = stats.TotalOrders > 0 
            ? stats.TotalSpent / stats.TotalOrders 
            : 0;

        return Task.FromResult(new CustomerStatistics
        {
            CustomerId = customerId,
            TotalOrders = stats.TotalOrders,
            TotalSpent = stats.TotalSpent,
            AverageOrderValue = avgOrderValue,
            LastOrderDate = stats.LastOrderDate
        });
    }

    public Task<GlobalStatistics> GetGlobalStatisticsAsync()
    {
        // ODCZYT GLOBALNYCH STATYSTYK - bez COUNT/SUM w bazie!
        // READ GLOBAL STATISTICS - no COUNT/SUM in database!
        
        var avgOrderValue = State.GlobalStatistics.TotalOrders > 0
            ? State.GlobalStatistics.TotalRevenue / State.GlobalStatistics.TotalOrders
            : 0;

        return Task.FromResult(new GlobalStatistics
        {
            TotalOrders = State.GlobalStatistics.TotalOrders,
            TotalRevenue = State.GlobalStatistics.TotalRevenue,
            TotalCustomers = State.GlobalStatistics.UniqueCustomers.Count,
            AverageOrderValue = avgOrderValue
        });
    }
}
