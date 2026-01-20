#!/bin/bash

# Demo script dla Orleans E-Commerce Tutorial
# Demo script for Orleans E-Commerce Tutorial

echo "======================================"
echo "Orleans E-Commerce Demo"
echo "======================================"
echo ""
echo "Ten skrypt demonstruje podstawowe operacje w aplikacji e-commerce:"
echo "This script demonstrates basic e-commerce operations:"
echo "1. Tworzenie klientów (biznesowych i indywidualnych)"
echo "   Creating customers (business and individual)"
echo "2. Dodawanie produktów z różnymi cenami"
echo "   Adding products with different prices"
echo "3. Składanie zamówień z różnymi cenami dla różnych klientów"
echo "   Placing orders with different prices for different customers"
echo ""
echo "Upewnij się, że uruchomiłeś Silo i API przed uruchomieniem tego skryptu!"
echo "Make sure you started the Silo and API before running this script!"
echo ""
read -p "Naciśnij Enter aby kontynuować / Press Enter to continue..."

# API endpoint
API="http://localhost:5000"

echo ""
echo "======================================"
echo "KROK 1: Tworzenie klientów / Creating customers"
echo "======================================"

# Klient indywidualny
echo ""
echo "Tworzenie klienta indywidualnego (B2C)..."
echo "Creating individual customer (B2C)..."
CUSTOMER_INDIVIDUAL=$(curl -s -X POST "$API/customers" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Jan Kowalski",
    "email": "jan.kowalski@example.com",
    "customerType": 0,
    "taxId": null
  }')

CUSTOMER_INDIVIDUAL_ID=$(echo $CUSTOMER_INDIVIDUAL | grep -o '"customerId":"[^"]*' | cut -d'"' -f4)
echo "✓ Utworzono klienta indywidualnego ID: $CUSTOMER_INDIVIDUAL_ID"
echo "$CUSTOMER_INDIVIDUAL" | python3 -m json.tool || echo "$CUSTOMER_INDIVIDUAL"

# Klient biznesowy
echo ""
echo "Tworzenie klienta biznesowego (B2B)..."
echo "Creating business customer (B2B)..."
CUSTOMER_BUSINESS=$(curl -s -X POST "$API/customers" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Acme Corporation",
    "email": "office@acme.com",
    "customerType": 1,
    "taxId": "1234567890"
  }')

CUSTOMER_BUSINESS_ID=$(echo $CUSTOMER_BUSINESS | grep -o '"customerId":"[^"]*' | cut -d'"' -f4)
echo "✓ Utworzono klienta biznesowego ID: $CUSTOMER_BUSINESS_ID"
echo "$CUSTOMER_BUSINESS" | python3 -m json.tool || echo "$CUSTOMER_BUSINESS"

echo ""
echo "======================================"
echo "KROK 2: Dodawanie produktów / Adding products"
echo "======================================"

# Produkt 1: Laptop
echo ""
echo "Dodawanie Laptop (retail: 5000 PLN, wholesale: 4000 PLN)..."
PRODUCT_LAPTOP=$(curl -s -X POST "$API/products" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Laptop Dell XPS 15",
    "description": "Professional laptop for business and personal use",
    "retailPrice": 5000,
    "wholesalePrice": 4000,
    "initialStock": 50
  }')

PRODUCT_LAPTOP_ID=$(echo $PRODUCT_LAPTOP | grep -o '"productId":"[^"]*' | cut -d'"' -f4)
echo "✓ Dodano produkt Laptop ID: $PRODUCT_LAPTOP_ID"
echo "$PRODUCT_LAPTOP" | python3 -m json.tool || echo "$PRODUCT_LAPTOP"

# Produkt 2: Mysz
echo ""
echo "Dodawanie Mysz (retail: 150 PLN, wholesale: 100 PLN)..."
PRODUCT_MOUSE=$(curl -s -X POST "$API/products" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Logitech MX Master 3",
    "description": "Wireless mouse for professionals",
    "retailPrice": 150,
    "wholesalePrice": 100,
    "initialStock": 200
  }')

PRODUCT_MOUSE_ID=$(echo $PRODUCT_MOUSE | grep -o '"productId":"[^"]*' | cut -d'"' -f4)
echo "✓ Dodano produkt Mysz ID: $PRODUCT_MOUSE_ID"
echo "$PRODUCT_MOUSE" | python3 -m json.tool || echo "$PRODUCT_MOUSE"

echo ""
echo "======================================"
echo "KROK 3: Zamówienie klienta indywidualnego"
echo "         Individual customer order"
echo "======================================"

# Zamówienie klienta indywidualnego
echo ""
echo "Tworzenie zamówienia dla klienta indywidualnego..."
ORDER_INDIVIDUAL=$(curl -s -X POST "$API/orders" \
  -H "Content-Type: application/json" \
  -d "{\"customerId\": \"$CUSTOMER_INDIVIDUAL_ID\"}")

ORDER_INDIVIDUAL_ID=$(echo $ORDER_INDIVIDUAL | grep -o '"orderId":"[^"]*' | cut -d'"' -f4)
echo "✓ Utworzono zamówienie ID: $ORDER_INDIVIDUAL_ID"

echo ""
echo "Dodawanie Laptop do zamówienia (cena: 5000 PLN - retail)..."
curl -s -X POST "$API/orders/$ORDER_INDIVIDUAL_ID/items" \
  -H "Content-Type: application/json" \
  -d "{\"productId\": \"$PRODUCT_LAPTOP_ID\", \"quantity\": 1}" | python3 -m json.tool

echo ""
echo "Dodawanie Mysz do zamówienia (cena: 150 PLN - retail)..."
curl -s -X POST "$API/orders/$ORDER_INDIVIDUAL_ID/items" \
  -H "Content-Type: application/json" \
  -d "{\"productId\": \"$PRODUCT_MOUSE_ID\", \"quantity\": 2}" | python3 -m json.tool

echo ""
echo "Potwierdzanie zamówienia..."
ORDER_INDIVIDUAL_CONFIRMED=$(curl -s -X POST "$API/orders/$ORDER_INDIVIDUAL_ID/confirm")
echo "$ORDER_INDIVIDUAL_CONFIRMED" | python3 -m json.tool || echo "$ORDER_INDIVIDUAL_CONFIRMED"

echo ""
echo "PODSUMOWANIE ZAMÓWIENIA KLIENTA INDYWIDUALNEGO:"
echo "- Laptop: 1 x 5000 PLN = 5000 PLN"
echo "- Mysz: 2 x 150 PLN = 300 PLN"
echo "- NETTO: 5300 PLN"
echo "- VAT (23%): ~1219 PLN"
echo "- BRUTTO: ~6519 PLN"

echo ""
echo "======================================"
echo "KROK 4: Zamówienie klienta biznesowego"
echo "         Business customer order"
echo "======================================"

# Zamówienie klienta biznesowego
echo ""
echo "Tworzenie zamówienia dla klienta biznesowego..."
ORDER_BUSINESS=$(curl -s -X POST "$API/orders" \
  -H "Content-Type: application/json" \
  -d "{\"customerId\": \"$CUSTOMER_BUSINESS_ID\"}")

ORDER_BUSINESS_ID=$(echo $ORDER_BUSINESS | grep -o '"orderId":"[^"]*' | cut -d'"' -f4)
echo "✓ Utworzono zamówienie ID: $ORDER_BUSINESS_ID"

echo ""
echo "Dodawanie Laptop do zamówienia (cena: 4000 PLN - wholesale)..."
curl -s -X POST "$API/orders/$ORDER_BUSINESS_ID/items" \
  -H "Content-Type: application/json" \
  -d "{\"productId\": \"$PRODUCT_LAPTOP_ID\", \"quantity\": 1}" | python3 -m json.tool

echo ""
echo "Dodawanie Mysz do zamówienia (cena: 100 PLN - wholesale)..."
curl -s -X POST "$API/orders/$ORDER_BUSINESS_ID/items" \
  -H "Content-Type: application/json" \
  -d "{\"productId\": \"$PRODUCT_MOUSE_ID\", \"quantity\": 2}" | python3 -m json.tool

echo ""
echo "Potwierdzanie zamówienia..."
ORDER_BUSINESS_CONFIRMED=$(curl -s -X POST "$API/orders/$ORDER_BUSINESS_ID/confirm")
echo "$ORDER_BUSINESS_CONFIRMED" | python3 -m json.tool || echo "$ORDER_BUSINESS_CONFIRMED"

echo ""
echo "PODSUMOWANIE ZAMÓWIENIA KLIENTA BIZNESOWEGO:"
echo "- Laptop: 1 x 4000 PLN = 4000 PLN (OSZCZĘDNOŚĆ: 1000 PLN!)"
echo "- Mysz: 2 x 100 PLN = 200 PLN (OSZCZĘDNOŚĆ: 100 PLN!)"
echo "- NETTO: 4200 PLN (vs 5300 PLN dla klienta indywidualnego)"
echo "- VAT (23%): ~966 PLN"
echo "- BRUTTO: ~5166 PLN (vs ~6519 PLN)"

echo ""
echo "======================================"
echo "PODSUMOWANIE DEMO"
echo "======================================"
echo ""
echo "✓ Klient indywidualny płaci więcej (retail prices)"
echo "✓ Klient biznesowy płaci mniej (wholesale prices)"
echo "✓ Różnica w tym zamówieniu: ~1353 PLN (20.7%)"
echo ""
echo "Sprawdź stan magazynu:"
echo "curl http://localhost:5000/products/$PRODUCT_LAPTOP_ID"
echo "curl http://localhost:5000/products/$PRODUCT_MOUSE_ID"
echo ""
echo "Sprawdź szczegóły zamówień:"
echo "curl http://localhost:5000/orders/$ORDER_INDIVIDUAL_ID"
echo "curl http://localhost:5000/orders/$ORDER_BUSINESS_ID"
echo ""
echo "Orleans Dashboard: http://localhost:8080"
echo ""
