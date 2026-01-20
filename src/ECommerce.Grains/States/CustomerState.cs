using ECommerce.Contracts.Models;

namespace ECommerce.Grains.States;

/// <summary>
/// Stan grainu Customer przechowywany w bazie danych
/// Customer grain state persisted in database
/// 
/// W Orleans stan grainu jest automatycznie:
/// - Ładowany z bazy przy aktywacji grainu
/// - Zapisywany do bazy po zmianach (gdy wywołamy WriteStateAsync)
/// - Usuwany z pamięci gdy grain jest nieaktywny
/// 
/// In Orleans grain state is automatically:
/// - Loaded from database on grain activation
/// - Saved to database after changes (when we call WriteStateAsync)
/// - Removed from memory when grain is inactive
/// </summary>
[Serializable]
public class CustomerState
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public CustomerType CustomerType { get; set; }
    public string? TaxId { get; set; }
    public decimal VatRate { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsCreated { get; set; }
}
