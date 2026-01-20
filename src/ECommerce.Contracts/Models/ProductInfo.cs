namespace ECommerce.Contracts.Models;

/// <summary>
/// Informacje o produkcie
/// Product information
/// 
/// ORLEANS SERIALIZATION:
/// Wszystkie typy używane w interfejsach grainów muszą być serializowalne
/// All types used in grain interfaces must be serializable
/// </summary>
[GenerateSerializer]
public record ProductInfo
{
    [Id(0)]
    public Guid ProductId { get; init; }
    
    [Id(1)]
    public string Name { get; init; } = string.Empty;
    
    [Id(2)]
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Cena bazowa dla klientów indywidualnych
    /// Base price for individual customers
    /// </summary>
    [Id(3)]
    public decimal RetailPrice { get; init; }
    
    /// <summary>
    /// Cena dla klientów biznesowych (zwykle niższa)
    /// Price for business customers (usually lower)
    /// </summary>
    [Id(4)]
    public decimal WholesalePrice { get; init; }
    
    /// <summary>
    /// Aktualny stan magazynowy
    /// Current inventory stock
    /// </summary>
    [Id(5)]
    public int StockQuantity { get; init; }
}
