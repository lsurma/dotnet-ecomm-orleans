namespace ECommerce.Contracts.Models;

/// <summary>
/// Typ klienta - indywidualny lub biznesowy
/// Customer type - individual or business
/// </summary>
public enum CustomerType
{
    /// <summary>
    /// Klient indywidualny (B2C) - wyższe ceny, standardowy VAT
    /// Individual customer (B2C) - higher prices, standard VAT
    /// </summary>
    Individual,

    /// <summary>
    /// Klient biznesowy (B2B) - niższe ceny hurtowe, możliwa inna stawka VAT
    /// Business customer (B2B) - lower wholesale prices, potentially different VAT rate
    /// </summary>
    Business
}
