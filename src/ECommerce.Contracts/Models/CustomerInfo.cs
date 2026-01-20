namespace ECommerce.Contracts.Models;

/// <summary>
/// Informacje o kliencie
/// Customer information
/// 
/// ORLEANS SERIALIZATION:
/// [GenerateSerializer] - Orleans wygeneruje automatyczny serializer
/// [Id(x)] - kolejny numer pola dla serializacji
/// 
/// Orleans wymaga serializacji dla wszystkich typów przekazywanych między grainami
/// Orleans requires serialization for all types passed between grains
/// </summary>
[GenerateSerializer]
public record CustomerInfo
{
    [Id(0)]
    public Guid CustomerId { get; init; }
    
    [Id(1)]
    public string Name { get; init; } = string.Empty;
    
    [Id(2)]
    public string Email { get; init; } = string.Empty;
    
    [Id(3)]
    public CustomerType CustomerType { get; init; }
    
    /// <summary>
    /// NIP dla klientów biznesowych / Tax ID for business customers
    /// </summary>
    [Id(4)]
    public string? TaxId { get; init; }
    
    /// <summary>
    /// Stawka VAT dla tego klienta (w procentach)
    /// VAT rate for this customer (in percentage)
    /// </summary>
    [Id(5)]
    public decimal VatRate { get; init; }
}
