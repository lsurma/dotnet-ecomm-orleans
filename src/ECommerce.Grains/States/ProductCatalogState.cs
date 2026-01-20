namespace ECommerce.Grains.States;

/// <summary>
/// Stan grainu ProductCatalog przechowywany w bazie danych
/// ProductCatalog grain state persisted in database
/// </summary>
[Serializable]
public class ProductCatalogState
{
    /// <summary>
    /// Lista wszystkich ID produktów w katalogu
    /// List of all product IDs in catalog
    /// </summary>
    public HashSet<Guid> ProductIds { get; set; } = new();
}
