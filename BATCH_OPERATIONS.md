# Operacje na Listach w Orleans / Batch Operations in Orleans

## Problem: Jak efektywnie pobrać wiele obiektów? / How to efficiently fetch multiple objects?

### Scenariusz / Scenario
Chcesz pobrać informacje o 20 produktach jednocześnie.
You want to fetch information about 20 products at once.

---

## ❌ Podejście 1: Sekwencyjne wywołania (WOLNE)

### Classic .NET Approach
```csharp
// Jedno zapytanie SQL
var products = await _dbContext.Products
    .Where(p => productIds.Contains(p.Id))
    .ToListAsync();

// Czas: ~50ms (zależy od bazy danych)
// Time: ~50ms (depends on database)
```

### Orleans - Naiwna implementacja (NIE RÓB TEGO!)
```csharp
var result = new List<ProductInfo>();
foreach (var id in productIds)  // 20 produktów
{
    var grain = client.GetGrain<IProductGrain>(id);
    var info = await grain.GetInfoAsync();  // ~10ms per call
    if (info != null) result.Add(info);
}
// Łączny czas: 20 * 10ms = 200ms (!)
// Total time: 20 * 10ms = 200ms (!)
```

**Problem:**
- Każde `await` czeka na zakończenie wywołania
- Suma opóźnień wszystkich wywołań
- Nie wykorzystuje równoległości

---

## ✅ Podejście 2: Fan-Out Pattern (OPTYMALNE)

### Implementacja / Implementation

```csharp
// KROK 1: Przygotuj wszystkie zadania (nie czekaj jeszcze!)
// STEP 1: Prepare all tasks (don't wait yet!)
var tasks = productIds
    .Select(id => client.GetGrain<IProductGrain>(id).GetInfoAsync())
    .ToList();

// KROK 2: Czekaj na wszystkie równocześnie
// STEP 2: Wait for all concurrently
var results = await Task.WhenAll(tasks);

// KROK 3: Filtruj wyniki
// STEP 3: Filter results
var products = results.Where(p => p != null).ToList();

// Łączny czas: max(wszystkie wywołania) ≈ 10-20ms
// Total time: max(all calls) ≈ 10-20ms
```

### Jak to działa? / How it works?

```
Sekwencyjne:          [A]---[B]---[C]---[D]---[E]  = 50ms
Sequential:

Fan-out:              [A]
                      [B]
                      [C]  = 10ms (max pojedynczego)
                      [D]
                      [E]
```

---

## 🎯 Podejście 3: Grain Katalogowy (NAJLEPSZE)

### Dlaczego dodatkowy grain? / Why an additional grain?

**Bez grainu katalogowego:**
```
Client → API → 20x Wywołań do Product Grains
       ↑___________________↓
         20 wywołań przez sieć
         (API ↔ Silo)
```

**Z grainem katalogowym:**
```
Client → API → Catalog Grain → 20x Wywołań do Product Grains
       ↑_________↓              ↑_________________________↓
      1 wywołanie              20 wywołań wewnętrznych
      przez sieć               (w ramach Silo)
```

### Implementacja / Implementation

```csharp
public interface IProductCatalogGrain : IGrainWithGuidKey
{
    Task<List<ProductInfo>> GetProductsAsync(List<Guid> productIds);
}

public class ProductCatalogGrain : Grain<ProductCatalogState>, IProductCatalogGrain
{
    public async Task<List<ProductInfo>> GetProductsAsync(List<Guid> productIds)
    {
        // Fan-out wewnątrz grainu
        // Fan-out inside grain
        var tasks = productIds
            .Select(id => GrainFactory.GetGrain<IProductGrain>(id).GetInfoAsync())
            .ToList();
        
        var results = await Task.WhenAll(tasks);
        return results.Where(p => p != null).ToList();
    }
}
```

### Użycie / Usage

```csharp
// W API
var catalogGrain = client.GetGrain<IProductCatalogGrain>(catalogId);
var products = await catalogGrain.GetProductsAsync(productIds);
// Jedno wywołanie API → Silo, wiele wywołań wewnętrznych
```

---

## 📊 Porównanie Wydajności / Performance Comparison

### Test: Pobieranie 20 produktów / Fetching 20 products

| Metoda / Method | Czas / Time | Wywołania sieciowe / Network calls |
|-----------------|-------------|-----------------------------------|
| SQL (Classic .NET) | ~50ms | 1 (API → DB) |
| Orleans - Sekwencyjne | ~200ms | 20 (API → Silo) |
| Orleans - Fan-out | ~10-20ms | 20 (API → Silo) |
| Orleans - Catalog Grain | ~10-15ms | 1 (API → Silo) + 20 wewnętrznych |

### Test: Pobieranie 100 produktów / Fetching 100 products

| Metoda / Method | Czas / Time | Skalowalność / Scalability |
|-----------------|-------------|---------------------------|
| SQL | ~100-200ms | Limit połączeń DB |
| Orleans - Sekwencyjne | ~1000ms | ❌ Nie skaluje się |
| Orleans - Fan-out | ~20-30ms | ✅ Skaluje horyzontalnie |
| Orleans - Catalog Grain | ~20-25ms | ✅ Skaluje horyzontalnie |

---

## 💡 Optymalizacje / Optimizations

### 1. Grain Reference Caching

**Bez cache:**
```csharp
foreach (var id in ids)
{
    var grain = GrainFactory.GetGrain<IProductGrain>(id);  // Za każdym razem!
    await grain.DoSomething();
}
```

**Z cache:**
```csharp
// Przygotuj referencje raz
var grains = ids.Select(id => GrainFactory.GetGrain<IProductGrain>(id)).ToList();

// Użyj wielokrotnie
foreach (var grain in grains)
{
    await grain.DoSomething();
}
```

### 2. Batch Size Limiting

```csharp
public async Task<List<ProductInfo>> GetProductsAsync(List<Guid> productIds)
{
    const int BATCH_SIZE = 50;  // Limit równoczesnych wywołań
    
    var result = new List<ProductInfo>();
    
    // Przetwarzaj w partiach
    for (int i = 0; i < productIds.Count; i += BATCH_SIZE)
    {
        var batch = productIds.Skip(i).Take(BATCH_SIZE);
        var tasks = batch.Select(id => 
            GrainFactory.GetGrain<IProductGrain>(id).GetInfoAsync());
        
        var batchResults = await Task.WhenAll(tasks);
        result.AddRange(batchResults.Where(p => p != null));
    }
    
    return result;
}
```

### 3. Index Grain dla dużych katalogów

Dla tysięcy produktów, użyj grainu indeksującego:

```csharp
public interface IProductIndexGrain : IGrainWithStringKey
{
    // Indeks po kategorii
    Task<List<Guid>> GetProductIdsByCategoryAsync(string category);
    
    // Indeks po zakresie cen
    Task<List<Guid>> GetProductIdsByPriceRangeAsync(decimal min, decimal max);
}
```

---

## 🔥 Przykłady API / API Examples

### Pobierz wiele produktów (fan-out)
```bash
curl -X POST http://localhost:5269/catalog/products/batch \
  -H "Content-Type: application/json" \
  -d '{
    "productIds": [
      "a5fe77fb-6570-444f-9f60-84c49f48bae0",
      "b6ef88fc-7681-555g-0g71-95d50g59cbf1",
      "c7fg99gd-8792-666h-1h82-06e61h60dcg2"
    ]
  }'
```

### Pobierz przez grain katalogowy
```bash
curl -X POST http://localhost:5269/catalog/products/batch-via-catalog \
  -H "Content-Type: application/json" \
  -d '{
    "productIds": [
      "a5fe77fb-6570-444f-9f60-84c49f48bae0",
      "b6ef88fc-7681-555g-0g71-95d50g59cbf1"
    ]
  }'
```

### Wyszukaj produkty
```bash
curl "http://localhost:5269/catalog/products/search?q=laptop&limit=20"
```

### Pobierz wszystkie produkty
```bash
curl http://localhost:5269/catalog/products/all
```

---

## 🎓 Najlepsze Praktyki / Best Practices

### ✅ DO (Rób to):

1. **Używaj Task.WhenAll** dla równoległych wywołań
2. **Cachuj grain references** jeśli używasz ich wielokrotnie
3. **Użyj grainu katalogowego** dla często używanych list
4. **Limituj rozmiar batch** (np. 50-100 na raz)
5. **Rozważ partycjonowanie** dla bardzo dużych zbiorów

### ❌ DON'T (Nie rób tego):

1. **Nie używaj pętli z await** - użyj Task.WhenAll
2. **Nie pobieraj wszystkiego naraz** - użyj paginacji
3. **Nie duplikuj logiki** - wynieś ją do grainu katalogowego
4. **Nie zapomnij o timeout** - ustaw limity czasowe
5. **Nie ignoruj null results** - filtruj wyniki

---

## 📈 Kiedy używać której metody? / When to use which method?

| Scenariusz | Metoda | Powód |
|-----------|--------|-------|
| < 10 produktów | Fan-out w API | Prostota |
| 10-50 produktów | Fan-out w API | Wystarczająco szybkie |
| 50-200 produktów | Catalog Grain | Mniej wywołań sieciowych |
| > 200 produktów | Catalog Grain + batching | Limituj równoczesne wywołania |
| Częste zapytania | Catalog Grain | Możliwość cache'owania |
| Wyszukiwanie | Index Grain | Dedykowane indeksy |

---

## 🧪 Testowanie / Testing

### Test wydajnościowy
```bash
# Utwórz 20 produktów
for i in {1..20}; do
  curl -X POST http://localhost:5269/products \
    -H "Content-Type: application/json" \
    -d "{
      \"name\": \"Product $i\",
      \"description\": \"Test product\",
      \"retailPrice\": 100,
      \"wholesalePrice\": 80,
      \"initialStock\": 50
    }"
done

# Zmierz czas pobierania (używając /usr/bin/time)
time curl -X POST http://localhost:5269/catalog/products/batch \
  -H "Content-Type: application/json" \
  -d '{"productIds": [...]}'  # Wstaw wszystkie 20 IDs
```

---

## 🔗 Zobacz też / See also

- [Orleans Documentation - Grains](https://docs.microsoft.com/orleans/grains)
- [Task.WhenAll Documentation](https://docs.microsoft.com/dotnet/api/system.threading.tasks.task.whenall)
- [Async/Await Best Practices](https://docs.microsoft.com/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming)
