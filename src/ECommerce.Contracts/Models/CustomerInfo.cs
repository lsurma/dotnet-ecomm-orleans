namespace ECommerce.Contracts.Models;

/// <summary>
/// Informacje o kliencie
/// Customer information
/// </summary>
public record CustomerInfo
{
    public Guid CustomerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public CustomerType CustomerType { get; init; }
    
    /// <summary>
    /// NIP dla klientów biznesowych / Tax ID for business customers
    /// </summary>
    public string? TaxId { get; init; }
    
    /// <summary>
    /// Stawka VAT dla tego klienta (w procentach)
    /// VAT rate for this customer (in percentage)
    /// </summary>
    public decimal VatRate { get; init; }
}
