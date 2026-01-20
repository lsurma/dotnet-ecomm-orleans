using ECommerce.Contracts.Grains;
using ECommerce.Contracts.Models;
using ECommerce.Grains.States;
using Orleans.Runtime;

namespace ECommerce.Grains;

/// <summary>
/// IMPLEMENTACJA CUSTOMER GRAIN
/// 
/// Grain dziedziczy po Grain<TState> gdzie TState to typ stanu do persystencji
/// 
/// Ważne koncepcje Orleans:
/// 
/// 1. AKTYWACJA GRAINU (Grain Activation)
///    - Grain jest tworzony gdy po raz pierwszy ktoś się do niego odwoła
///    - OnActivateAsync jest wywoływany i ładuje stan z bazy
///    - Grain pozostaje w pamięci do deaktywacji
/// 
/// 2. PERSYSTENCJA (Persistence)
///    - State jest automatycznie ładowany przy aktywacji
///    - WriteStateAsync() zapisuje stan do bazy
///    - ReadStateAsync() odświeża stan z bazy (rzadko używane)
/// 
/// 3. SINGLE-THREADED EXECUTION
///    - Orleans gwarantuje, że tylko jedno żądanie obsługuje grain w danej chwili
///    - Nie potrzeba lock'ów ani Semaphore
///    - Możemy bezpiecznie modyfikować State
/// 
/// 4. GRAIN IDENTITY
///    - Każdy grain ma unikalny klucz (tu: Guid klienta)
///    - Orleans automatycznie routuje wywołania do odpowiedniej instancji
///    - GrainFactory.GetGrain<ICustomerGrain>(customerId) zawsze zwraca tego samego grainu
/// 
/// CUSTOMER GRAIN IMPLEMENTATION
/// 
/// Grain inherits from Grain<TState> where TState is the state type for persistence
/// 
/// Important Orleans concepts:
/// 
/// 1. GRAIN ACTIVATION
///    - Grain is created when first referenced
///    - OnActivateAsync is called and loads state from database
///    - Grain stays in memory until deactivation
/// 
/// 2. PERSISTENCE
///    - State is automatically loaded on activation
///    - WriteStateAsync() saves state to database
///    - ReadStateAsync() refreshes state from database (rarely used)
/// 
/// 3. SINGLE-THREADED EXECUTION
///    - Orleans guarantees only one request processes grain at a time
///    - No need for locks or Semaphore
///    - We can safely modify State
/// 
/// 4. GRAIN IDENTITY
///    - Each grain has a unique key (here: customer Guid)
///    - Orleans automatically routes calls to the right instance
///    - GrainFactory.GetGrain<ICustomerGrain>(customerId) always returns the same grain
/// </summary>
public class CustomerGrain : Grain<CustomerState>, ICustomerGrain
{
    public async Task CreateAsync(string name, string email, CustomerType customerType, string? taxId = null)
    {
        if (State.IsCreated)
        {
            throw new InvalidOperationException("Customer already exists");
        }

        State.Name = name;
        State.Email = email;
        State.CustomerType = customerType;
        State.TaxId = taxId;
        
        // Różne stawki VAT dla różnych typów klientów
        // Different VAT rates for different customer types
        State.VatRate = customerType == CustomerType.Business ? 23m : 23m; // W Polsce standard to 23%, ale można różnicować
        
        State.CreatedAt = DateTime.UtcNow;
        State.IsCreated = true;

        // Zapisz stan do bazy danych - to jest kluczowa metoda Orleans
        // Save state to database - this is a key Orleans method
        await WriteStateAsync();
    }

    public Task<CustomerInfo?> GetInfoAsync()
    {
        if (!State.IsCreated)
        {
            return Task.FromResult<CustomerInfo?>(null);
        }

        var info = new CustomerInfo
        {
            CustomerId = this.GetPrimaryKey(), // Pobieramy ID grainu / Get grain ID
            Name = State.Name,
            Email = State.Email,
            CustomerType = State.CustomerType,
            TaxId = State.TaxId,
            VatRate = State.VatRate
        };

        return Task.FromResult<CustomerInfo?>(info);
    }

    public async Task UpdateAsync(string name, string email)
    {
        if (!State.IsCreated)
        {
            throw new InvalidOperationException("Customer does not exist");
        }

        State.Name = name;
        State.Email = email;

        await WriteStateAsync();
    }
}
