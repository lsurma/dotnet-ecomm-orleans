namespace ECommerce.Contracts.Models;

/// <summary>
/// Pozycja zamówienia
/// Order item
/// </summary>
public record OrderItem
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    
    /// <summary>
    /// Cena jednostkowa (zależna od typu klienta)
    /// Unit price (depends on customer type)
    /// </summary>
    public decimal UnitPrice { get; init; }
    
    /// <summary>
    /// Wartość netto = UnitPrice * Quantity
    /// Net value = UnitPrice * Quantity
    /// </summary>
    public decimal NetAmount => UnitPrice * Quantity;
}
