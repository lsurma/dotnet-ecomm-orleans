# Orleans E-Commerce Tutorial - Quick Start Guide

## Co zostało zaimplementowane / What Was Implemented

### ✅ Core Features

1. **Klienci (Customers)**
   - Klienci indywidualni (B2C) - wyższe ceny detaliczne
   - Klienci biznesowi (B2B) - niższe ceny hurtowe
   - Różne stawki VAT dla różnych typów

2. **Produkty (Products)**
   - Dwie ceny: retail (indywidualni) i wholesale (biznesowi)
   - Zarządzanie stanem magazynowym
   - Automatyczna rezerwacja przy potwierdzaniu zamówień

3. **Zamówienia (Orders)**
   - Workflow: Pending → Confirmed
   - Automatyczne naliczanie cen zależnie od typu klienta
   - Kompensacja przy błędach (zwrot produktów)

### ✅ Orleans Features Demonstrated

1. **Grains** - Actor model implementation
   - CustomerGrain - zarządzanie klientami
   - ProductGrain - katalog i magazyn
   - OrderGrain - proces zamówienia

2. **State Persistence** - Automatyczny zapis do bazy
   - In-memory dla deweloperki
   - PostgreSQL dla produkcji

3. **Grain-to-Grain Communication**
   - Order → Customer (pobieranie typu klienta)
   - Order → Product (ceny, rezerwacja)

4. **Serialization** - [GenerateSerializer] + [Id(x)]

## Szybki Start / Quick Start

```bash
# 1. Klonuj repozytorium
git clone <repo-url>
cd dotnet-ecomm-orleans

# 2. Uruchom Silo (w jednym terminalu)
cd src/ECommerce.Silo
dotnet run

# 3. Uruchom API (w drugim terminalu)
cd src/ECommerce.Api
dotnet run

# 4. Uruchom demo (w trzecim terminalu)
./demo.sh
```

## Testowanie Ręczne / Manual Testing

### Utwórz klienta indywidualnego
```bash
curl -X POST http://localhost:5269/customers \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Jan Kowalski",
    "email": "jan@example.com",
    "customerType": 0,
    "taxId": null
  }'
```

### Utwórz klienta biznesowego
```bash
curl -X POST http://localhost:5269/customers \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Acme Corp",
    "email": "office@acme.com",
    "customerType": 1,
    "taxId": "1234567890"
  }'
```

### Dodaj produkt
```bash
curl -X POST http://localhost:5269/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Laptop Dell XPS 15",
    "description": "Professional laptop",
    "retailPrice": 5000,
    "wholesalePrice": 4000,
    "initialStock": 50
  }'
```

## Kluczowe Różnice w Cenach / Key Price Differences

| Typ Klienta | Cena Netto | VAT (23%) | Cena Brutto |
|-------------|------------|-----------|-------------|
| **Indywidualny** | 5000 PLN | 1150 PLN | **6150 PLN** |
| **Biznesowy** | 4000 PLN | 920 PLN | **4920 PLN** |
| **Oszczędność** | 1000 PLN | 230 PLN | **1230 PLN (20%)** |

## Najważniejsze Pliki / Most Important Files

```
src/
├── ECommerce.Contracts/
│   ├── Grains/           # Interfejsy grainów
│   │   ├── ICustomerGrain.cs
│   │   ├── IProductGrain.cs
│   │   └── IOrderGrain.cs
│   └── Models/           # DTOs z serializacją
│       ├── CustomerInfo.cs
│       ├── ProductInfo.cs
│       └── OrderInfo.cs
│
├── ECommerce.Grains/     # Implementacje grainów
│   ├── CustomerGrain.cs  # Logika klientów
│   ├── ProductGrain.cs   # Logika produktów + magazyn
│   └── OrderGrain.cs     # Workflow zamówień
│
├── ECommerce.Silo/       # Serwer Orleans
│   └── Program.cs        # Konfiguracja clustering + persistence
│
└── ECommerce.Api/        # Klient Orleans
    └── Program.cs        # REST API endpoints
```

## Kluczowe Koncepty w Kodzie / Key Concepts in Code

### 1. Single-Threaded Grain Execution
```csharp
// Nie trzeba lock'ów! Orleans gwarantuje atomowość
State.StockQuantity -= quantity;
await WriteStateAsync();
```

### 2. Grain-to-Grain Communication
```csharp
var customerGrain = GrainFactory.GetGrain<ICustomerGrain>(customerId);
var customerInfo = await customerGrain.GetInfoAsync();
var price = await productGrain.GetPriceForCustomerTypeAsync(customerInfo.CustomerType);
```

### 3. Compensating Actions (Saga Pattern)
```csharp
if (!reserved) {
    // Cofnij już dokonane rezerwacje
    foreach (var (productId, quantity) in reservations) {
        await rollbackGrain.ReturnStockAsync(quantity);
    }
    return false;
}
```

## Dashboard Orleans

Po uruchomieniu Silo otwórz:
```
http://localhost:8080
```

Dashboard pokazuje:
- Aktywne grainy
- Statystyki wywołań
- Stan klastra

## Co Dalej? / What's Next?

1. **Dodaj więcej produktów i klientów**
2. **Przetestuj scenariusze błędów** (brak produktów w magazynie)
3. **Obejrzyj Orleans Dashboard** podczas działania aplikacji
4. **Przeczytaj komentarze w kodzie** - szczegółowe wyjaśnienia Orleans
5. **Porównaj z klasycznym .NET** - zobacz różnice w podejściu

## Troubleshooting

### Błąd: "Found unserializable types"
- Dodaj `[GenerateSerializer]` do klasy
- Dodaj `[Id(x)]` do każdej właściwości

### Błąd: "Connection refused"
- Upewnij się, że Silo jest uruchomiony przed API

### Błąd: "Could not confirm order"
- Sprawdź stan magazynu (może brak produktów)
- Zobacz logi w Silo

## Zasoby / Resources

- [Oficjalna dokumentacja Orleans](https://docs.microsoft.com/en-us/dotnet/orleans/)
- [Orleans GitHub](https://github.com/dotnet/orleans)
- [Orleans Samples](https://github.com/dotnet/orleans/tree/main/samples)

---

**Powodzenia w nauce Orleans!** 🎉
**Good luck learning Orleans!** 🎉
