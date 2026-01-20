using ECommerce.Contracts.Grains;
using ECommerce.Contracts.Models;
using ECommerce.Grains.States;
using Orleans.Runtime;

namespace ECommerce.Grains;

/// <summary>
/// IMPLEMENTACJA PRODUCT GRAIN
/// 
/// ProductGrain demonstruje:
/// 
/// 1. ZARZĄDZANIE STANEM MAGAZYNU (Stock Management)
///    - Stan produktu (stock) jest w pamięci
///    - Modyfikacje są atomowe (Orleans zapewnia single-threaded execution)
///    - W klasycznym podejściu wymagałoby SELECT FOR UPDATE lub optimistic locking
/// 
/// 2. RÓŻNE CENY DLA RÓŻNYCH KLIENTÓW (Different Prices for Different Customers)
///    - Logika biznesowa w grainie
///    - Cena zależy od typu klienta
///    - Prostsza implementacja niż JOIN'y w bazie
/// 
/// 3. WALIDACJA I LOGIKA BIZNESOWA (Validation and Business Logic)
///    - Grain może odrzucić operację (zwrócić false)
///    - Łatwiej testować logikę niż stored procedures
/// 
/// PRODUCT GRAIN IMPLEMENTATION
/// 
/// ProductGrain demonstrates:
/// 
/// 1. STOCK MANAGEMENT
///    - Product state (stock) is in memory
///    - Modifications are atomic (Orleans ensures single-threaded execution)
///    - In classic approach would require SELECT FOR UPDATE or optimistic locking
/// 
/// 2. DIFFERENT PRICES FOR DIFFERENT CUSTOMERS
///    - Business logic in grain
///    - Price depends on customer type
///    - Simpler implementation than database JOINs
/// 
/// 3. VALIDATION AND BUSINESS LOGIC
///    - Grain can reject operation (return false)
///    - Easier to test logic than stored procedures
/// </summary>
public class ProductGrain : Grain<ProductState>, IProductGrain
{
    public async Task CreateAsync(string name, string description, decimal retailPrice, decimal wholesalePrice, int initialStock)
    {
        if (State.IsCreated)
        {
            throw new InvalidOperationException("Product already exists");
        }

        State.Name = name;
        State.Description = description;
        State.RetailPrice = retailPrice;
        State.WholesalePrice = wholesalePrice;
        State.StockQuantity = initialStock;
        State.CreatedAt = DateTime.UtcNow;
        State.IsCreated = true;

        await WriteStateAsync();
    }

    public Task<ProductInfo?> GetInfoAsync()
    {
        if (!State.IsCreated)
        {
            return Task.FromResult<ProductInfo?>(null);
        }

        var info = new ProductInfo
        {
            ProductId = this.GetPrimaryKey(),
            Name = State.Name,
            Description = State.Description,
            RetailPrice = State.RetailPrice,
            WholesalePrice = State.WholesalePrice,
            StockQuantity = State.StockQuantity
        };

        return Task.FromResult<ProductInfo?>(info);
    }

    public async Task<bool> ReserveStockAsync(int quantity)
    {
        if (!State.IsCreated)
        {
            return false;
        }

        // Walidacja stanu magazynowego
        // Validate stock availability
        if (State.StockQuantity < quantity)
        {
            return false; // Brak wystarczającej ilości / Insufficient stock
        }

        // Zmniejsz stan magazynowy
        // Decrease stock
        State.StockQuantity -= quantity;

        await WriteStateAsync();
        return true;
    }

    public async Task ReturnStockAsync(int quantity)
    {
        if (!State.IsCreated)
        {
            return;
        }

        // Zwiększ stan magazynowy (np. przy anulowaniu zamówienia)
        // Increase stock (e.g. when cancelling order)
        State.StockQuantity += quantity;

        await WriteStateAsync();
    }

    public Task<decimal> GetPriceForCustomerTypeAsync(CustomerType customerType)
    {
        if (!State.IsCreated)
        {
            return Task.FromResult(0m);
        }

        // Różne ceny dla różnych typów klientów
        // Different prices for different customer types
        var price = customerType == CustomerType.Business 
            ? State.WholesalePrice  // Niższa cena dla biznesu / Lower price for business
            : State.RetailPrice;     // Wyższa cena dla klientów indywidualnych / Higher price for individuals

        return Task.FromResult(price);
    }
}
