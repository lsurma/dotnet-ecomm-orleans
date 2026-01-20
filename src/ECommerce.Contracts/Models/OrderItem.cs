namespace ECommerce.Contracts.Models;

/// <summary>
/// Pozycja zamówienia
/// Order item
/// 
/// ORLEANS SERIALIZATION:
/// Typy zagnieżdżone także muszą być serializowalne
/// Nested types must also be serializable
/// </summary>
[GenerateSerializer]
public record OrderItem
{
    [Id(0)]
    public Guid ProductId { get; init; }
    
    [Id(1)]
    public string ProductName { get; init; } = string.Empty;
    
    [Id(2)]
    public int Quantity { get; init; }
    
    /// <summary>
    /// Cena jednostkowa (zależna od typu klienta)
    /// Unit price (depends on customer type)
    /// </summary>
    [Id(3)]
    public decimal UnitPrice { get; init; }
    
    /// <summary>
    /// Wartość netto = UnitPrice * Quantity
    /// Net value = UnitPrice * Quantity
    /// </summary>
    public decimal NetAmount => UnitPrice * Quantity;
}
