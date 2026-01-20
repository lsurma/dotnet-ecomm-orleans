namespace ECommerce.Grains.States;

/// <summary>
/// Stan grainu ProductIndex (materialized view)
/// ProductIndex grain state (materialized view)
/// </summary>
[Serializable]
public class ProductIndexState
{
    /// <summary>
    /// Produkty w tym indeksie (np. kategoria "Electronics")
    /// Products in this index (e.g. category "Electronics")
    /// </summary>
    public Dictionary<Guid, ProductIndexEntry> Products { get; set; } = new();

    /// <summary>
    /// Ostatnia aktualizacja
    /// Last update timestamp
    /// </summary>
    public DateTime LastUpdated { get; set; }
}

[Serializable]
public class ProductIndexEntry
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal RetailPrice { get; set; }
    public decimal WholesalePrice { get; set; }
    public int StockQuantity { get; set; }
}
