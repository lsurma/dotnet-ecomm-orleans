# E-Commerce Orleans Tutorial / Tutorial E-Commerce z Orleans

> **Tutorial Project**: Aplikacja e-commerce używająca Microsoft Orleans i EF Core z PostgreSQL  
> **Tutorial Project**: E-commerce application using Microsoft Orleans and EF Core with PostgreSQL

## 📖 O Projekcie / About

Ten projekt to **tutorial wprowadzający do Microsoft Orleans** dla programistów znających .NET, ale nowych w Orleans. Pokazuje praktyczne zastosowanie Orleans w aplikacji e-commerce z:

- **Klientami biznesowymi i indywidualnymi** (różne ceny i stawki VAT)
- **Zarządzaniem zamówieniami**
- **Magazynem produktów**

This project is an **introductory tutorial to Microsoft Orleans** for developers familiar with .NET but new to Orleans. It shows practical Orleans usage in an e-commerce application with:

- **Business and individual customers** (different prices and VAT rates)
- **Order management**
- **Product warehouse**

---

## 🎯 Co to jest Orleans? / What is Orleans?

### Klasyczne podejście .NET / Classic .NET Approach

W typowej aplikacji ASP.NET Core:
```csharp
// Serwis wstrzykiwany przez DI
public class OrderService
{
    private readonly DbContext _db;
    private readonly SemaphoreSlim _lock = new(1);
    
    public async Task ProcessOrder(Guid orderId)
    {
        await _lock.WaitAsync(); // Synchronizacja!
        try
        {
            // Obsługa zamówienia
            var order = await _db.Orders.FindAsync(orderId);
            // ...
        }
        finally
        {
            _lock.Release();
        }
    }
}
```

**Problemy:**
- Trzeba zarządzać synchronizacją (lock, SemaphoreSlim)
- Współdzielony stan między wieloma żądaniami
- Trudne skalowanie poziome
- Cache wymaga Redis i zarządzania invalidacją

In a typical ASP.NET Core application:
```csharp
// Service injected via DI
public class OrderService
{
    private readonly DbContext _db;
    private readonly SemaphoreSlim _lock = new(1);
    
    public async Task ProcessOrder(Guid orderId)
    {
        await _lock.WaitAsync(); // Synchronization!
        try
        {
            // Process order
            var order = await _db.Orders.FindAsync(orderId);
            // ...
        }
        finally
        {
            _lock.Release();
        }
    }
}
```

**Problems:**
- Need to manage synchronization (lock, SemaphoreSlim)
- Shared state between many requests
- Difficult horizontal scaling
- Cache requires Redis and invalidation management

### Podejście Orleans / Orleans Approach

```csharp
// Grain - aktor reprezentujący jedno zamówienie
public class OrderGrain : Grain<OrderState>, IOrderGrain
{
    // Brak lock'ów! Orleans gwarantuje single-threaded execution
    public async Task ProcessOrder()
    {
        // Bezpieczna modyfikacja State
        State.Status = OrderStatus.Processing;
        await WriteStateAsync(); // Automatyczny zapis do bazy
    }
}
```

**Zalety Orleans:**
- ✅ **Automatyczna synchronizacja** - jeden grain = jedno wywołanie na raz
- ✅ **Wbudowany cache** - stan w pamięci, automatyczne zarządzanie
- ✅ **Naturalne skalowanie** - grainy rozproszone po serwerach
- ✅ **Persystencja** - automatyczny zapis/odczyt z bazy

**Orleans Benefits:**
- ✅ **Automatic synchronization** - one grain = one call at a time
- ✅ **Built-in cache** - state in memory, automatic management
- ✅ **Natural scaling** - grains distributed across servers
- ✅ **Persistence** - automatic save/load from database

---

## 🏗️ Architektura / Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     ECommerce.Api (Client)                  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │   Customer   │  │   Product    │  │    Order     │      │
│  │  Controller  │  │  Controller  │  │  Controller  │      │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘      │
│         │                  │                  │              │
│         └──────────────────┼──────────────────┘              │
│                            │ IClusterClient                  │
└────────────────────────────┼─────────────────────────────────┘
                             │
                             │ Orleans Communication
                             │
┌────────────────────────────┼─────────────────────────────────┐
│                  ECommerce.Silo (Server)                     │
│         │                  │                  │              │
│  ┌──────▼───────┐  ┌──────▼───────┐  ┌──────▼───────┐      │
│  │   Customer   │  │   Product    │  │    Order     │      │
│  │    Grain     │  │    Grain     │  │    Grain     │      │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘      │
│         │                  │                  │              │
│         └──────────────────┼──────────────────┘              │
│                            │ State Persistence               │
└────────────────────────────┼─────────────────────────────────┘
                             │
                             ▼
                    ┌─────────────────┐
                    │   PostgreSQL    │
                    │  OrleansStorage │
                    └─────────────────┘
```

### Projekty / Projects

1. **ECommerce.Contracts** - Interfejsy grainów i modele danych / Grain interfaces and data models
2. **ECommerce.Grains** - Implementacje grainów (logika biznesowa) / Grain implementations (business logic)
3. **ECommerce.Data** - EF Core DbContext i konfiguracja bazy / EF Core DbContext and database configuration
4. **ECommerce.Silo** - Serwer Orleans hostujący grainy / Orleans server hosting grains
5. **ECommerce.Api** - Klient Orleans (REST API) / Orleans client (REST API)

---

## 🔑 Kluczowe Koncepcje Orleans / Key Orleans Concepts

### 1. Grain (Aktor)

**Grain** to podstawowa jednostka w Orleans - obiekt z unikalnym ID i własnym stanem.

**Grain** is the fundamental unit in Orleans - an object with unique ID and its own state.

```csharp
// Każdy klient ma swój własny grain
var customer1 = client.GetGrain<ICustomerGrain>(customerId1);
var customer2 = client.GetGrain<ICustomerGrain>(customerId2);
// To są dwa RÓŻNE obiekty w pamięci
```

**Kluczowe cechy / Key features:**
- Unikalny identyfikator (Guid, long, string)
- Własny stan przechowywany w pamięci
- Single-threaded execution (Orleans gwarantuje)
- Automatyczna aktywacja/deaktywacja

### 2. Grain Interface

Interfejsy grainów definiują operacje:

```csharp
public interface ICustomerGrain : IGrainWithGuidKey
{
    Task CreateAsync(string name, string email, CustomerType type);
    Task<CustomerInfo?> GetInfoAsync();
}
```

### 3. Persystencja Stanu / State Persistence

Orleans automatycznie zapisuje stan do bazy:

```csharp
public class CustomerGrain : Grain<CustomerState>, ICustomerGrain
{
    public async Task CreateAsync(...)
    {
        State.Name = name; // Modyfikacja w pamięci
        await WriteStateAsync(); // Automatyczny zapis do bazy
    }
}
```

**W klasycznym podejściu:**
```csharp
await _dbContext.Customers.AddAsync(customer);
await _dbContext.SaveChangesAsync();
```

### 4. Grain-to-Grain Communication

Grainy mogą się nawzajem wywoływać:

```csharp
public async Task<bool> AddItemAsync(Guid productId, int quantity)
{
    // Wywołaj inny grain
    var productGrain = GrainFactory.GetGrain<IProductGrain>(productId);
    var price = await productGrain.GetPriceForCustomerTypeAsync(...);
    
    // Użyj wyniku
    State.Items.Add(new OrderItem { Price = price, ... });
}
```

### 5. Operacje na Listach - Fan-Out Pattern / Batch Operations - Fan-Out Pattern

**Problem:** Jak pobrać 20 produktów efektywnie?

**Naiwne podejście (WOLNE ❌):**
```csharp
// N wywołań sekwencyjnych = N * latency
var products = new List<ProductInfo>();
foreach (var id in productIds)  // 20 iteracji
{
    var grain = client.GetGrain<IProductGrain>(id);
    var info = await grain.GetInfoAsync();  // Czekamy ~10ms
    if (info != null) products.Add(info);
}
// Łączny czas: 20 * 10ms = 200ms
```

**Optymalne podejście (SZYBKIE ✅):**
```csharp
// FAN-OUT PATTERN: Wszystkie wywołania równolegle
var tasks = productIds
    .Select(id => client.GetGrain<IProductGrain>(id).GetInfoAsync())
    .ToList();

var results = await Task.WhenAll(tasks);  // Czeka na wszystkie jednocześnie
var products = results.Where(p => p != null).ToList();
// Łączny czas: max(10ms) = ~10ms (20x szybciej!)
```

**Przykład: Pobieranie 20 produktów**
```bash
# Metoda 1: Fan-out bezpośrednio w API
curl -X POST http://localhost:5269/catalog/products/batch \
  -H "Content-Type: application/json" \
  -d '{"productIds": ["id1", "id2", ..., "id20"]}'

# Metoda 2: Przez grain katalogowy (zalecane dla większych list)
curl -X POST http://localhost:5269/catalog/products/batch-via-catalog \
  -H "Content-Type: application/json" \
  -d '{"productIds": ["id1", "id2", ..., "id20"]}'

# Wyszukiwanie produktów
curl "http://localhost:5269/catalog/products/search?q=laptop&limit=20"
```

**Porównanie wydajności:**

| Metoda | Liczba produktów | Czas |
|--------|-----------------|------|
| Sekwencyjna (❌) | 20 | ~200ms |
| Fan-out (✅) | 20 | ~10-20ms |
| Sekwencyjna (❌) | 100 | ~1000ms |
| Fan-out (✅) | 100 | ~10-30ms |

**Zalety Fan-Out Pattern:**
- ✅ Czas proporcjonalny do najwolniejszego grainu, nie suma wszystkich
- ✅ Wykorzystuje równoległość sieci i CPU
- ✅ Skaluje się liniowo z liczbą produktów
- ✅ Orleans automatycznie rozdziela grainy po serwerach w klastrze

**W klasycznym podejściu:**
```csharp
// Jedno zapytanie SQL, ale...
var products = await _dbContext.Products
    .Where(p => productIds.Contains(p.Id))
    .ToListAsync();
// - Wymaga połączenia z bazą przy każdym żądaniu
// - Nie skaluje się horyzontalnie bez sharding
// - Trudne cachowanie (invalidacja)
```

---

## 🚀 Uruchomienie / Getting Started

### Wymagania / Prerequisites

- .NET 10 SDK
- PostgreSQL (opcjonalnie - dla persystencji produkcyjnej)
- Docker (opcjonalnie - dla PostgreSQL)

### Krok 1: Klonowanie / Clone

```bash
git clone <repository-url>
cd dotnet-ecomm-orleans
```

### Krok 2: Uruchomienie w trybie developerskim (bez bazy danych) / Run in development mode (without database)

W trybie developerskim aplikacja używa **in-memory storage** - nie potrzebujesz PostgreSQL!

In development mode the app uses **in-memory storage** - you don't need PostgreSQL!

```bash
# Terminal 1 - Orleans Silo
cd src/ECommerce.Silo
dotnet run

# Terminal 2 - API
cd src/ECommerce.Api
dotnet run
```

### Krok 3: Testowanie API / Test API

```bash
# Utwórz klienta biznesowego
curl -X POST http://localhost:5000/customers \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Acme Corp",
    "email": "contact@acme.com",
    "customerType": 1,
    "taxId": "1234567890"
  }'

# Utwórz produkt
curl -X POST http://localhost:5000/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Laptop",
    "description": "Business laptop",
    "retailPrice": 5000,
    "wholesalePrice": 4000,
    "initialStock": 100
  }'

# Utwórz zamówienie
curl -X POST http://localhost:5000/orders \
  -H "Content-Type: application/json" \
  -d '{"customerId": "<customer-id>"}'
```

### Opcjonalnie: PostgreSQL dla persystencji / Optional: PostgreSQL for persistence

```bash
# Docker
docker run --name postgres-orleans \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=ecommerce_orleans \
  -p 5432:5432 \
  -d postgres:15

# Zmień ASPNETCORE_ENVIRONMENT na Production
# Change ASPNETCORE_ENVIRONMENT to Production
export ASPNETCORE_ENVIRONMENT=Production
```

---

## 💡 Przykłady Użycia / Usage Examples

### Przykład 1: Różne ceny dla różnych klientów / Different prices for different customers

```csharp
// W ProductGrain
public Task<decimal> GetPriceForCustomerTypeAsync(CustomerType customerType)
{
    var price = customerType == CustomerType.Business 
        ? State.WholesalePrice  // 4000 PLN dla biznesu
        : State.RetailPrice;     // 5000 PLN dla klienta indywidualnego
    return Task.FromResult(price);
}
```

### Przykład 2: Workflow zamówienia / Order workflow

```csharp
// 1. Utwórz zamówienie
var orderGrain = client.GetGrain<IOrderGrain>(orderId);
await orderGrain.CreateAsync(customerId);

// 2. Dodaj produkty
await orderGrain.AddItemAsync(productId1, quantity: 2);
await orderGrain.AddItemAsync(productId2, quantity: 1);

// 3. Potwierdź (rezerwuje produkty w magazynie)
var success = await orderGrain.ConfirmAsync();

// 4. W razie problemów - anuluj (zwraca produkty)
await orderGrain.CancelAsync();
```

### Przykład 3: Compensating Actions (Saga Pattern)

```csharp
public async Task<bool> ConfirmAsync()
{
    var reservations = new List<(Guid ProductId, int Quantity)>();
    
    foreach (var item in State.Items)
    {
        var reserved = await productGrain.ReserveStockAsync(item.Quantity);
        
        if (!reserved)
        {
            // ROLLBACK - zwróć już zarezerwowane produkty
            foreach (var (productId, quantity) in reservations)
            {
                var rollbackGrain = GrainFactory.GetGrain<IProductGrain>(productId);
                await rollbackGrain.ReturnStockAsync(quantity);
            }
            return false;
        }
        
        reservations.Add((item.ProductId, item.Quantity));
    }
    
    return true;
}
```

---

## 📚 Kluczowe Różnice: Orleans vs Klasyczny .NET / Key Differences: Orleans vs Classic .NET

| Aspekt | Klasyczny .NET | Orleans |
|--------|----------------|---------|
| **Współbieżność** | Ręczne lock'i, SemaphoreSlim | Automatyczna (single-threaded grain) |
| **Stan w pamięci** | Redis + invalidacja | Wbudowany + auto-zarządzanie |
| **Skalowanie** | Load balancer + sticky sessions | Automatyczne rozproszone grainy |
| **Persystencja** | Ręczne SaveChanges() | WriteStateAsync() |
| **Distributed transactions** | Saga pattern (ręcznie) | Compensating actions (łatwiej) |
| **Testy jednostkowe** | Mock DbContext | Mock GrainFactory |

---

## 🛠️ Orleans Dashboard

Orleans Dashboard jest dostępny podczas działania Silo:

```
http://localhost:8080
```

Dashboard pokazuje:
- Aktywne grainy
- Statystyki wywołań
- Stan klastra
- Metryki wydajności

---

## 📖 Dalsze Kroki / Next Steps

1. **Dodaj Reminders** - Scheduled tasks (np. automatyczne anulowanie zamówień po 24h)
2. **Dodaj Streams** - Event-driven architecture (np. powiadomienia o zmianach stanu)
3. **Dodaj testy jednostkowe** - TestCluster dla testowania grainów
4. **Deployment** - Kubernetes + AdoNet clustering dla produkcji
5. **Monitoring** - Application Insights integration

---

## 🤝 Contributing

To projekt edukacyjny - pull requesty mile widziane!

This is an educational project - pull requests welcome!

---

## 📄 License

MIT

---

## 📞 Pytania? / Questions?

Jeśli masz pytania o Orleans lub ten projekt, otwórz Issue na GitHubie.

If you have questions about Orleans or this project, open an Issue on GitHub.
