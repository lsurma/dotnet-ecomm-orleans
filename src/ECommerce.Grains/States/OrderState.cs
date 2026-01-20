using ECommerce.Contracts.Models;

namespace ECommerce.Grains.States;

/// <summary>
/// Stan grainu Order przechowywany w bazie danych
/// Order grain state persisted in database
/// </summary>
[Serializable]
public class OrderState
{
    public Guid CustomerId { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public decimal VatRate { get; set; }
    public bool IsCreated { get; set; }
}
