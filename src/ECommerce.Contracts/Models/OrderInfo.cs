namespace ECommerce.Contracts.Models;

/// <summary>
/// Informacje o zamówieniu
/// Order information
/// </summary>
public record OrderInfo
{
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public OrderStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ConfirmedAt { get; init; }
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
