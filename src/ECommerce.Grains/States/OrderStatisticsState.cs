namespace ECommerce.Grains.States;

/// <summary>
/// Stan grainu OrderStatistics (materialized view)
/// OrderStatistics grain state (materialized view)
/// </summary>
[Serializable]
public class OrderStatisticsState
{
    /// <summary>
    /// Statystyki per klient
    /// Statistics per customer
    /// </summary>
    public Dictionary<Guid, CustomerStats> CustomerStatistics { get; set; } = new();

    /// <summary>
    /// Globalne statystyki
    /// Global statistics
    /// </summary>
    public GlobalStats GlobalStatistics { get; set; } = new();
}

[Serializable]
public class CustomerStats
{
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime? LastOrderDate { get; set; }
}

[Serializable]
public class GlobalStats
{
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public HashSet<Guid> UniqueCustomers { get; set; } = new();
}
