namespace ECommerce.Contracts.Models;

/// <summary>
/// Informacje o produkcie
/// Product information
/// </summary>
public record ProductInfo
{
    public Guid ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Cena bazowa dla klientów indywidualnych
    /// Base price for individual customers
    /// </summary>
    public decimal RetailPrice { get; init; }
    
    /// <summary>
    /// Cena dla klientów biznesowych (zwykle niższa)
    /// Price for business customers (usually lower)
    /// </summary>
    public decimal WholesalePrice { get; init; }
    
    /// <summary>
    /// Aktualny stan magazynowy
    /// Current inventory stock
    /// </summary>
    public int StockQuantity { get; init; }
}
