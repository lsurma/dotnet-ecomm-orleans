namespace ECommerce.Contracts.Models;

/// <summary>
/// Status zamówienia
/// Order status
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Zamówienie stworzone, ale jeszcze nie zatwierdzone
    /// Order created but not yet confirmed
    /// </summary>
    Pending,
    
    /// <summary>
    /// Zamówienie zatwierdzone, gotowe do realizacji
    /// Order confirmed, ready for fulfillment
    /// </summary>
    Confirmed,
    
    /// <summary>
    /// Zamówienie w trakcie realizacji
    /// Order being fulfilled
    /// </summary>
    Processing,
    
    /// <summary>
    /// Zamówienie wysłane
    /// Order shipped
    /// </summary>
    Shipped,
    
    /// <summary>
    /// Zamówienie dostarczone
    /// Order delivered
    /// </summary>
    Delivered,
    
    /// <summary>
    /// Zamówienie anulowane
    /// Order cancelled
    /// </summary>
    Cancelled
}
