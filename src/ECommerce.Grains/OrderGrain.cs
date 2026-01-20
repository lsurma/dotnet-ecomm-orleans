using ECommerce.Contracts.Grains;
using ECommerce.Contracts.Models;
using ECommerce.Grains.States;
using Orleans.Runtime;

namespace ECommerce.Grains;

/// <summary>
/// IMPLEMENTACJA ORDER GRAIN
/// 
/// OrderGrain pokazuje najpotężniejszą cechę Orleans - KOMUNIKACJĘ MIĘDZY GRAINAMI:
/// 
/// 1. GRAIN-TO-GRAIN CALLS
///    - OrderGrain wywołuje CustomerGrain i ProductGrain
///    - W klasycznym podejściu: trzeba wstrzykiwać serwisy, zarządzać transakcjami
///    - W Orleans: po prostu GrainFactory.GetGrain<T>(id) i wywołujemy metody
/// 
/// 2. WORKFLOW / SAGA PATTERN
///    - Zamówienie przechodzi przez stany: Pending -> Confirmed -> Shipped
///    - Każda zmiana stanu może wywoływać inne grainy
///    - Przy błędzie możemy łatwo cofnąć zmiany (ReturnStockAsync)
/// 
/// 3. TRANSAKCYJNOŚĆ (Transaction-like Behavior)
///    - Choć Orleans nie ma distributed transactions
///    - Możemy zaimplementować compensating actions (cofnięcie zmian)
///    - Przykład: jeśli potwierdzenie się nie udało, zwracamy produkty
/// 
/// 4. BUSINESS LOGIC COORDINATION
///    - Order koordynuje Customer i Product
///    - Waliduje reguły biznesowe
///    - Oblicza ceny z uwzględnieniem typu klienta
/// 
/// ORDER GRAIN IMPLEMENTATION
/// 
/// OrderGrain shows the most powerful Orleans feature - GRAIN-TO-GRAIN COMMUNICATION:
/// 
/// 1. GRAIN-TO-GRAIN CALLS
///    - OrderGrain calls CustomerGrain and ProductGrain
///    - In classic approach: need to inject services, manage transactions
///    - In Orleans: just GrainFactory.GetGrain<T>(id) and call methods
/// 
/// 2. WORKFLOW / SAGA PATTERN
///    - Order goes through states: Pending -> Confirmed -> Shipped
///    - Each state change can call other grains
///    - On error we can easily rollback changes (ReturnStockAsync)
/// 
/// 3. TRANSACTION-LIKE BEHAVIOR
///    - Although Orleans doesn't have distributed transactions
///    - We can implement compensating actions (rollback changes)
///    - Example: if confirmation failed, return products
/// 
/// 4. BUSINESS LOGIC COORDINATION
///    - Order coordinates Customer and Product
///    - Validates business rules
///    - Calculates prices considering customer type
/// </summary>
public class OrderGrain : Grain<OrderState>, IOrderGrain
{
    public async Task CreateAsync(Guid customerId)
    {
        if (State.IsCreated)
        {
            throw new InvalidOperationException("Order already exists");
        }

        // Sprawdź czy klient istnieje - przykład komunikacji grain-to-grain
        // Check if customer exists - example of grain-to-grain communication
        var customerGrain = GrainFactory.GetGrain<ICustomerGrain>(customerId);
        var customerInfo = await customerGrain.GetInfoAsync();
        
        if (customerInfo == null)
        {
            throw new InvalidOperationException("Customer does not exist");
        }

        State.CustomerId = customerId;
        State.Status = OrderStatus.Pending;
        State.CreatedAt = DateTime.UtcNow;
        State.VatRate = customerInfo.VatRate; // Pobierz stawkę VAT klienta / Get customer's VAT rate
        State.Items = new List<OrderItem>();
        State.IsCreated = true;

        await WriteStateAsync();
    }

    public async Task<bool> AddItemAsync(Guid productId, int quantity)
    {
        if (!State.IsCreated)
        {
            return false;
        }

        if (State.Status != OrderStatus.Pending)
        {
            return false; // Można dodawać produkty tylko do zamówień Pending / Can only add to Pending orders
        }

        // Pobierz informacje o produkcie - grain-to-grain call
        // Get product information - grain-to-grain call
        var productGrain = GrainFactory.GetGrain<IProductGrain>(productId);
        var productInfo = await productGrain.GetInfoAsync();
        
        if (productInfo == null)
        {
            return false;
        }

        // Pobierz typ klienta i odpowiednią cenę
        // Get customer type and appropriate price
        var customerGrain = GrainFactory.GetGrain<ICustomerGrain>(State.CustomerId);
        var customerInfo = await customerGrain.GetInfoAsync();
        
        if (customerInfo == null)
        {
            return false;
        }

        var price = await productGrain.GetPriceForCustomerTypeAsync(customerInfo.CustomerType);

        // Dodaj pozycję do zamówienia
        // Add item to order
        var orderItem = new OrderItem
        {
            ProductId = productId,
            ProductName = productInfo.Name,
            Quantity = quantity,
            UnitPrice = price
        };

        State.Items.Add(orderItem);

        await WriteStateAsync();
        return true;
    }

    public async Task<bool> ConfirmAsync()
    {
        if (!State.IsCreated || State.Status != OrderStatus.Pending)
        {
            return false;
        }

        if (!State.Items.Any())
        {
            return false; // Nie można potwierdzić pustego zamówienia / Cannot confirm empty order
        }

        // Rezerwuj produkty w magazynie - dla każdej pozycji
        // Reserve products in warehouse - for each item
        var reservations = new List<(Guid ProductId, int Quantity)>();
        
        foreach (var item in State.Items)
        {
            var productGrain = GrainFactory.GetGrain<IProductGrain>(item.ProductId);
            var reserved = await productGrain.ReserveStockAsync(item.Quantity);
            
            if (!reserved)
            {
                // Cofnij już dokonane rezerwacje - compensating action
                // Rollback already made reservations - compensating action
                foreach (var (productId, quantity) in reservations)
                {
                    var rollbackGrain = GrainFactory.GetGrain<IProductGrain>(productId);
                    await rollbackGrain.ReturnStockAsync(quantity);
                }
                
                return false; // Brak wystarczającej ilości produktu / Insufficient stock
            }
            
            reservations.Add((item.ProductId, item.Quantity));
        }

        State.Status = OrderStatus.Confirmed;
        State.ConfirmedAt = DateTime.UtcNow;

        await WriteStateAsync();

        // AKTUALIZUJ MATERIALIZED VIEW - Statystyki zamówień
        // UPDATE MATERIALIZED VIEW - Order statistics
        // W klasycznym podejściu: Cache invalidation lub REFRESH MATERIALIZED VIEW
        // In classic approach: Cache invalidation or REFRESH MATERIALIZED VIEW
        var statsGrain = GrainFactory.GetGrain<IOrderStatisticsGrain>(Guid.Empty); // Singleton stats grain
        var orderInfo = await GetInfoAsync();
        if (orderInfo != null)
        {
            await statsGrain.OnOrderConfirmedAsync(this.GetPrimaryKey(), State.CustomerId, orderInfo.GrossTotal);
        }

        return true;
    }

    public async Task CancelAsync()
    {
        if (!State.IsCreated)
        {
            return;
        }

        if (State.Status == OrderStatus.Confirmed || State.Status == OrderStatus.Pending)
        {
            // Zwróć produkty do magazynu jeśli były zarezerwowane
            // Return products to warehouse if they were reserved
            if (State.Status == OrderStatus.Confirmed)
            {
                // Aktualizuj materialized view - anulowanie zamówienia
                // Update materialized view - order cancellation
                var orderInfo = await GetInfoAsync();
                if (orderInfo != null)
                {
                    var statsGrain = GrainFactory.GetGrain<IOrderStatisticsGrain>(Guid.Empty);
                    await statsGrain.OnOrderCancelledAsync(this.GetPrimaryKey(), State.CustomerId, orderInfo.GrossTotal);
                }

                foreach (var item in State.Items)
                {
                    var productGrain = GrainFactory.GetGrain<IProductGrain>(item.ProductId);
                    await productGrain.ReturnStockAsync(item.Quantity);
                }
            }

            State.Status = OrderStatus.Cancelled;
            await WriteStateAsync();
        }
    }

    public Task<OrderInfo?> GetInfoAsync()
    {
        if (!State.IsCreated)
        {
            return Task.FromResult<OrderInfo?>(null);
        }

        var info = new OrderInfo
        {
            OrderId = this.GetPrimaryKey(),
            CustomerId = State.CustomerId,
            Status = State.Status,
            CreatedAt = State.CreatedAt,
            ConfirmedAt = State.ConfirmedAt,
            Items = State.Items.ToList(),
            VatRate = State.VatRate
        };

        return Task.FromResult<OrderInfo?>(info);
    }

    public async Task UpdateStatusAsync(OrderStatus newStatus)
    {
        if (!State.IsCreated)
        {
            return;
        }

        State.Status = newStatus;
        await WriteStateAsync();
    }
}
