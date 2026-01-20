namespace ECommerce.Contracts.Models;

/// <summary>
/// Informacje o zamówieniu
/// Order information
/// 
/// ORLEANS SERIALIZATION:
/// [GenerateSerializer] mówi Orleans aby wygenerował kod serializacji
/// List<OrderItem> także będzie serializowana automatycznie
/// 
/// [GenerateSerializer] tells Orleans to generate serialization code
/// List<OrderItem> will also be automatically serialized
/// </summary>
[GenerateSerializer]
public record OrderInfo
{
    [Id(0)]
    public Guid OrderId { get; init; }
    
    [Id(1)]
    public Guid CustomerId { get; init; }
    
    [Id(2)]
    public OrderStatus Status { get; init; }
    
    [Id(3)]
    public DateTime CreatedAt { get; init; }
    
    [Id(4)]
    public DateTime? ConfirmedAt { get; init; }
    
    [Id(5)]
    public List<OrderItem> Items { get; init; } = new();
    
    /// <summary>
    /// Suma wartości netto wszystkich pozycji
    /// Total net value of all items
    /// </summary>
    public decimal NetTotal => Items.Sum(i => i.NetAmount);
    
    /// <summary>
    /// Stawka VAT klienta (w procentach)
    /// Customer's VAT rate (in percentage)
    /// </summary>
    [Id(6)]
    public decimal VatRate { get; init; }
    
    /// <summary>
    /// Kwota VAT = NetTotal * (VatRate / 100)
    /// VAT amount = NetTotal * (VatRate / 100)
    /// </summary>
    public decimal VatAmount => NetTotal * (VatRate / 100);
    
    /// <summary>
    /// Wartość brutto = NetTotal + VatAmount
    /// Gross total = NetTotal + VatAmount
    /// </summary>
    public decimal GrossTotal => NetTotal + VatAmount;
}
