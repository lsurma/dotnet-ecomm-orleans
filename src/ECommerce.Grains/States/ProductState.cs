using ECommerce.Contracts.Models;

namespace ECommerce.Grains.States;

/// <summary>
/// Stan grainu Product przechowywany w bazie danych
/// Product grain state persisted in database
/// </summary>
[Serializable]
public class ProductState
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal RetailPrice { get; set; }
    public decimal WholesalePrice { get; set; }
    public int StockQuantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsCreated { get; set; }
}
