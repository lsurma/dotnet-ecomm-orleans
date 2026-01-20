# Materialized Views w Orleans / Materialized Views in Orleans

## Czym jest Materialized View? / What is a Materialized View?

**Materialized View** to pre-obliczony zestaw danych (agregacja, indeks, raport), który jest przechowywany i aktualizowany przyrostowo zamiast obliczany za każdym razem.

**Materialized View** is a pre-computed set of data (aggregation, index, report) that is stored and updated incrementally instead of being computed every time.

---

## Problem / Problem

### Klasyczne podejście / Classic Approach

```sql
-- Za każdym razem obliczamy statystyki od nowa
-- Every time we compute statistics from scratch
SELECT 
    customer_id,
    COUNT(*) as total_orders,
    SUM(total) as total_spent,
    AVG(total) as avg_order_value
FROM orders
WHERE customer_id = @customerId
GROUP BY customer_id;

-- Wolne dla dużych tabel! / Slow for large tables!
-- 1 milion zamówień = kilka sekund / 1 million orders = several seconds
```

**Problemy:**
- ❌ Kosztowne zapytania agregujące (COUNT, SUM, AVG)
- ❌ Skanowanie całej tabeli
- ❌ Wolne dla dużych zbiorów danych
- ❌ Obciąża bazę danych

**Problems:**
- ❌ Expensive aggregating queries (COUNT, SUM, AVG)
- ❌ Full table scan
- ❌ Slow for large datasets
- ❌ Loads database

---

## Rozwiązanie: Materialized View w Orleans

### Wzorzec / Pattern

```
┌─────────────┐
│ OrderGrain  │ ──┐
└─────────────┘   │
                  │ OnOrderConfirmed()
┌─────────────┐   │
│ OrderGrain  │ ──┤
└─────────────┘   │
                  ▼
┌─────────────┐   ┌──────────────────────┐
│ OrderGrain  │──▶│ OrderStatisticsGrain │ ◀── Materialized View
└─────────────┘   │ (Pre-computed stats) │
                  └──────────────────────┘
                           │
                           │ Instant read!
                           ▼
                    ┌────────────┐
                    │ API Client │
                    └────────────┘
```

### Kluczowe Cechy / Key Features

1. **Przyrostowa aktualizacja / Incremental Update**
   - Zamiast przebudowy: `totalOrders++`, `totalSpent += amount`
   - Szybka aktualizacja: ~1ms

2. **Natychmiastowy odczyt / Instant Read**
   - Brak zapytań agregujących
   - Dane już przygotowane w pamięci
   - Czas odczytu: ~1ms

3. **Skalowanie horyzontalne / Horizontal Scaling**
   - Każdy klient = osobny grain
   - Naturalne partycjonowanie
   - Rozproszone obliczenia

---

## Przykład 1: Statystyki Zamówień / Order Statistics

### Implementacja / Implementation

```csharp
public interface IOrderStatisticsGrain : IGrainWithGuidKey
{
    // Aktualizacja widoku przy zmianie danych źródłowych
    // Update view when source data changes
    Task OnOrderConfirmedAsync(Guid orderId, Guid customerId, decimal orderTotal);
    
    // Odczyt z materialized view - natychmiastowy!
    // Read from materialized view - instant!
    Task<CustomerStatistics> GetCustomerStatisticsAsync(Guid customerId);
}

public class OrderStatisticsGrain : Grain<OrderStatisticsState>, IOrderStatisticsGrain
{
    public async Task OnOrderConfirmedAsync(Guid orderId, Guid customerId, decimal orderTotal)
    {
        // PRZYROSTOWA AKTUALIZACJA (nie rebuild całego widoku!)
        // INCREMENTAL UPDATE (no full view rebuild!)
        
        if (!State.CustomerStatistics.ContainsKey(customerId))
        {
            State.CustomerStatistics[customerId] = new CustomerStats();
        }

        var stats = State.CustomerStatistics[customerId];
        stats.TotalOrders++;           // Inkrementuj / Increment
        stats.TotalSpent += orderTotal; // Dodaj do sumy / Add to sum
        stats.LastOrderDate = DateTime.UtcNow;

        await WriteStateAsync(); // Zapisz widok / Persist view
    }

    public Task<CustomerStatistics> GetCustomerStatisticsAsync(Guid customerId)
    {
        // ODCZYT - natychmiastowy, bez agregacji!
        // READ - instant, no aggregation!
        
        var stats = State.CustomerStatistics[customerId];
        return Task.FromResult(new CustomerStatistics
        {
            CustomerId = customerId,
            TotalOrders = stats.TotalOrders,
            TotalSpent = stats.TotalSpent,
            AverageOrderValue = stats.TotalSpent / stats.TotalOrders
        });
    }
}
```

### Aktualizacja w OrderGrain / Update in OrderGrain

```csharp
public class OrderGrain : Grain<OrderState>, IOrderGrain
{
    public async Task<bool> ConfirmAsync()
    {
        // ... logika potwierdzania zamówienia ...
        
        // AKTUALIZUJ MATERIALIZED VIEW
        // UPDATE MATERIALIZED VIEW
        var statsGrain = GrainFactory.GetGrain<IOrderStatisticsGrain>(Guid.Empty);
        await statsGrain.OnOrderConfirmedAsync(
            this.GetPrimaryKey(), 
            State.CustomerId, 
            CalculateTotal());
        
        return true;
    }
}
```

### Użycie w API / API Usage

```bash
# Pobierz statystyki klienta (instant!)
# Get customer statistics (instant!)
curl http://localhost:5269/statistics/customer/{customerId}

# Response (~1ms):
{
  "customerId": "...",
  "totalOrders": 15,
  "totalSpent": 45600.00,
  "averageOrderValue": 3040.00,
  "lastOrderDate": "2026-01-20T19:00:00Z"
}
```

---

## Przykład 2: Indeks Produktów / Product Index

### Wzorzec Sharded Index / Sharded Index Pattern

```
┌─────────────────────────┐
│ IProductIndexGrain      │
│ Key = "Electronics"     │ ◀── Indeks dla elektroniki
│ Contains: [P1, P2, P3]  │     Index for electronics
└─────────────────────────┘

┌─────────────────────────┐
│ IProductIndexGrain      │
│ Key = "Books"           │ ◀── Indeks dla książek
│ Contains: [P4, P5, P6]  │     Index for books
└─────────────────────────┘

┌─────────────────────────┐
│ IProductIndexGrain      │
│ Key = "Clothing"        │ ◀── Indeks dla ubrań
│ Contains: [P7, P8, P9]  │     Index for clothing
└─────────────────────────┘

Każdy indeks = osobny grain = naturalne partycjonowanie!
Each index = separate grain = natural partitioning!
```

### Implementacja / Implementation

```csharp
public interface IProductIndexGrain : IGrainWithStringKey
{
    Task AddProductAsync(ProductInfo product);
    Task<List<ProductInfo>> SearchAsync(string query, int maxResults);
    Task<List<ProductInfo>> GetByPriceRangeAsync(decimal min, decimal max);
}

public class ProductIndexGrain : Grain<ProductIndexState>, IProductIndexGrain
{
    public async Task AddProductAsync(ProductInfo product)
    {
        // AKTUALIZACJA INDEKSU W CZASIE RZECZYWISTYM
        // REAL-TIME INDEX UPDATE
        State.Products[product.ProductId] = ToIndexEntry(product);
        await WriteStateAsync();
    }

    public Task<List<ProductInfo>> SearchAsync(string query, int maxResults)
    {
        // WYSZUKIWANIE W PAMIĘCI (bez bazy danych!)
        // IN-MEMORY SEARCH (without database!)
        
        var results = State.Products.Values
            .Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(maxResults)
            .ToList();
        
        return Task.FromResult(results);
    }
}
```

### Użycie / Usage

```bash
# Wyszukaj produkty w kategorii "Electronics"
# Search products in "Electronics" category
curl "http://localhost:5269/index/Electronics/search?q=laptop&limit=20"

# Filtruj po cenie (bez zapytania do bazy!)
# Filter by price (without database query!)
curl "http://localhost:5269/index/Electronics/price-range?min=1000&max=5000"
```

---

## Porównanie Wydajności / Performance Comparison

### Statystyki Zamówień / Order Statistics

| Metoda | Zapytanie / Query | Czas / Time |
|--------|-------------------|-------------|
| **SQL Aggregation** | `SELECT COUNT(*), SUM(total) FROM orders WHERE customer_id = ?` | ~500ms dla 100k zamówień |
| **Materialized View (Orleans)** | `await statsGrain.GetCustomerStatisticsAsync(id)` | ~1ms (zawsze!) |
| **Przyspieszenie / Speedup** | - | **500x szybciej!** |

### Wyszukiwanie Produktów / Product Search

| Metoda | Zapytanie / Query | Czas / Time |
|--------|-------------------|-------------|
| **SQL LIKE** | `SELECT * FROM products WHERE name LIKE '%laptop%'` | ~200ms dla 100k produktów |
| **Full-Text Index** | `SELECT * FROM products WHERE MATCH(name) AGAINST('laptop')` | ~50ms |
| **Elasticsearch** | External system call | ~20-50ms + latency |
| **Orleans Index Grain** | `await indexGrain.SearchAsync("laptop", 20)` | ~1-5ms |
| **Przyspieszenie / Speedup** | - | **40-200x szybciej!** |

---

## Wzorce Aktualizacji / Update Patterns

### 1. Eager Update (Natychmiastowa)

```csharp
// Aktualizuj widok natychmiast po zmianie danych
// Update view immediately after data change
public async Task ConfirmAsync()
{
    State.Status = OrderStatus.Confirmed;
    await WriteStateAsync();
    
    // EAGER UPDATE - od razu
    var statsGrain = GrainFactory.GetGrain<IOrderStatisticsGrain>(Guid.Empty);
    await statsGrain.OnOrderConfirmedAsync(...);
}
```

**Zalety:** Widok zawsze aktualny  
**Wady:** Dodatkowy koszt przy zapisie

### 2. Lazy Update (Opóźniona)

```csharp
// Aktualizuj widok przy odczycie jeśli jest przestarzały
// Update view on read if stale
public async Task<CustomerStatistics> GetCustomerStatisticsAsync(Guid customerId)
{
    if (IsStale())
    {
        await RefreshViewAsync();
    }
    return State.CustomerStatistics[customerId];
}
```

**Zalety:** Brak kosztów przy zapisie  
**Wady:** Pierwszy odczyt może być wolny

### 3. Periodic Refresh (Okresowa)

```csharp
// Odświeżaj widok co X minut używając Timer
// Refresh view every X minutes using Timer
public override async Task OnActivateAsync(CancellationToken cancellationToken)
{
    RegisterTimer(
        async _ => await RefreshViewAsync(),
        null,
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(5));
}
```

**Zalety:** Balans między aktualnością a wydajnością  
**Wady:** Widok może być lekko nieaktualny

---

## Najlepsze Praktyki / Best Practices

### ✅ DO (Rób to):

1. **Używaj dla częstych odczytów** - Jeśli dane są odczytywane częściej niż zapisywane
2. **Przyrostowa aktualizacja** - Aktualizuj tylko zmienione wartości (++, +=)
3. **Partycjonuj duże widoki** - Jeden grain per kategoria/klient/region
4. **Cache grain references** - Nie twórz wielokrotnie tego samego grainu
5. **Monitoruj rozmiar** - Limituj ilość danych w widoku

### ❌ DON'T (Nie rób tego):

1. **Nie używaj dla rzadkich odczytów** - Materialized view ma sens dla częstych zapytań
2. **Nie trzymaj zbyt dużo danych** - Grain state powinien być relatywnie mały (<1MB)
3. **Nie zapomnij o aktualizacji** - Widok musi być aktualizowany przy zmianach źródła
4. **Nie duplikuj logiki** - Jedna metoda aktualizacji, wiele miejsc wywołania
5. **Nie ignoruj błędów aktualizacji** - Loguj i monitoruj niepowodzenia

---

## Porównanie z innymi technologiami

### SQL Materialized View

```sql
CREATE MATERIALIZED VIEW order_stats AS
  SELECT customer_id, COUNT(*) as total_orders, SUM(total) as total_spent
  FROM orders
  GROUP BY customer_id;

-- Wymagana ręczna odświeżanie (kosztowne!)
-- Manual refresh required (expensive!)
REFRESH MATERIALIZED VIEW order_stats;
```

**vs Orleans:**
- ✅ Orleans: Przyrostowa aktualizacja (inkrementalna)
- ✅ Orleans: Automatyczna aktualizacja przy zmianach
- ✅ Orleans: Skalowanie horyzontalne
- ❌ SQL: REFRESH przebudowuje cały widok
- ❌ SQL: Wymaga triggerów lub scheduled jobs

### Redis Cache

```csharp
// Cache z ręczną invalidacją
await cache.SetAsync($"stats:{customerId}", stats, TimeSpan.FromMinutes(5));

// Problem: Jak invalidować cache gdy dane się zmieniają?
// Problem: How to invalidate cache when data changes?
```

**vs Orleans:**
- ✅ Orleans: Automatyczna invalidacja (aktualizacja w źródle)
- ✅ Orleans: Rozproszona logika (grain per entity)
- ✅ Orleans: Nie wymaga osobnego systemu
- ❌ Redis: Ręczna invalidacja
- ❌ Redis: Wymaga zewnętrznego systemu
- ❌ Redis: TTL może prowadzić do nieaktualnych danych

### Elasticsearch

```bash
# Osobny system z opóźnieniem synchronizacji
# Separate system with synchronization delay
curl -X POST "localhost:9200/products/_search" -d '{
  "query": { "match": { "name": "laptop" } }
}'
```

**vs Orleans:**
- ✅ Orleans: Wbudowane w aplikację
- ✅ Orleans: Aktualizacja w czasie rzeczywistym
- ✅ Orleans: Brak dodatkowej infrastruktury
- ✅ Elasticsearch: Zaawansowane full-text search
- ✅ Elasticsearch: Skalowanie dla bardzo dużych zbiorów
- ❌ Elasticsearch: Opóźnienie synchronizacji
- ❌ Elasticsearch: Dodatkowa infrastruktura

---

## Przykłady Użycia w Świecie Rzeczywistym

### 1. E-commerce

- **Product Search Index** - Wyszukiwanie produktów po kategorii
- **Customer Lifetime Value** - Statystyki zakupów klienta
- **Inventory Summary** - Podsumowanie stanów magazynowych

### 2. Social Media

- **User Feed** - Pre-calculated feed dla użytkownika
- **Notifications Count** - Liczba nieprzeczytanych powiadomień
- **Trending Topics** - Top hashtagi/tematy

### 3. Gaming

- **Leaderboards** - Rankingi graczy
- **Player Statistics** - Statystyki gracza
- **Guild Summary** - Podsumowanie gildii

### 4. Analytics

- **Real-time Dashboards** - Dashboardy w czasie rzeczywistym
- **Report Cache** - Cache dla raportów
- **Metrics Aggregation** - Agregacja metryk

---

## Kiedy używać Materialized View?

### ✅ Używaj gdy:

- Dane są częściej odczytywane niż zapisywane (read-heavy)
- Agregacje są kosztowne (COUNT, SUM, AVG na dużych zbiorach)
- Potrzebujesz niskich latencji odczytu (<10ms)
- Dane mogą być lekko nieaktualne (eventual consistency OK)
- Masz naturalne partycjonowanie (per customer, per category)

### ❌ Nie używaj gdy:

- Dane są częściej zapisywane niż odczytywane (write-heavy)
- Agregacje są proste i szybkie
- Potrzebujesz 100% aktualnych danych (strong consistency)
- Widok zawierałby gigabajty danych
- Brak naturalnego partycjonowania

---

## Testowanie

```bash
# 1. Utwórz zamówienie i potwierdź
curl -X POST http://localhost:5269/orders -d '{"customerId": "..."}'
curl -X POST http://localhost:5269/orders/{orderId}/confirm

# 2. Sprawdź statystyki (powinny się zaktualizować natychmiast!)
curl http://localhost:5269/statistics/customer/{customerId}

# 3. Dodaj produkt do indeksu
curl -X POST http://localhost:5269/catalog/products/register \
  -d '{"productId": "..."}'

# 4. Wyszukaj w indeksie
curl "http://localhost:5269/index/Electronics/search?q=laptop"
```

---

## Podsumowanie

**Materialized View w Orleans** to potężny wzorzec dla:
- ✅ Pre-obliczonych agregacji
- ✅ Indeksów wyszukiwania
- ✅ Raportów i dashboardów
- ✅ Statystyk w czasie rzeczywistym

**Kluczowe zalety:**
- 🚀 Natychmiastowe odczyty (~1ms)
- 📈 Skalowanie horyzontalne
- 🔄 Automatyczna aktualizacja
- 💾 Wbudowane w Orleans (brak dodatkowej infrastruktury)

**vs klasyczne podejścia:**
- **SQL Materialized Views**: 500x szybsze odczyty
- **Redis Cache**: Automatyczna invalidacja
- **Elasticsearch**: Brak dodatkowej infrastruktury
