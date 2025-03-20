# **📄 Dokumentace k API – Modul Burza (ASP.NET Core Web API)**  
Tato dokumentace popisuje všechny **dostupné endpointy** modulu **Burza**, jejich **použití**, **příklady požadavků a odpovědí**, a **jak API testovat**.

---

## **📌 Základní informace**
| **Vlastnost**     | **Hodnota**                     |
|------------------|--------------------------------|
| **Base URL**     | `http://localhost:5000/api/burza` |
| **Formát dat**   | JSON                            |
| **Autentizace**  | Není potřeba (public API)      |

📌 API **komunikuje s modulem Zprávy** na `http://localhost:5001/api/zpravy/analyze`.  

---

## **📌 1️⃣ Vyhledání akcií (`GET /api/burza/search`)**
### **Popis**
Vyhledá akcie podle zadaného dotazu (např. jméno firmy nebo symbol).

### **URL**
```
GET /api/burza/search?query=MSFT
```

### **Parametry**
| **Parametr** | **Typ**  | **Popis**                      | **Příklad** |
|-------------|---------|--------------------------------|------------|
| `query`     | string  | Název nebo symbol akcie       | `MSFT`     |

### **Příklad požadavku**
```sh
curl -X GET "http://localhost:5000/api/burza/search?query=MSFT"
```

### **Odpověď (`200 OK`)**
```json
[
  { "symbol": "MSFT", "name": "Microsoft Corporation" }
]
```

### **Chybové odpovědi**
| **Kód** | **Popis**                  |
|--------|----------------------------|
| `400`  | Chybí parametr `query`      |
| `500`  | Chyba při komunikaci s API |

---

## **📌 2️⃣ Odeslání žádosti o doporučení (`POST /api/burza/recommendation`)**
### **Popis**
Odešle seznam akcií do **modulu Zprávy**, který analyzuje sentiment a vrátí **rating (-10 až 10)**.

### **URL**
```
POST /api/burza/recommendation
```

### **Tělo požadavku**
```json
{
  "timestamp": "2024-03-20T18:25:43.511Z",
  "date_from": "2024-03-15T18:25:43.511Z",
  "date_to": "2024-03-20T18:25:43.511Z",
  "stocks": [
    { "name": "MSFT", "rating": null, "sell": null },
    { "name": "AAPL", "rating": null, "sell": null }
  ]
}
```

### **Příklad požadavku (`curl`)**
```sh
curl -X POST "http://localhost:5000/api/burza/recommendation" \
     -H "Content-Type: application/json" \
     -d '{
  "timestamp": "2024-03-20T18:25:43.511Z",
  "date_from": "2024-03-15T18:25:43.511Z",
  "date_to": "2024-03-20T18:25:43.511Z",
  "stocks": [
    { "name": "MSFT", "rating": null, "sell": null },
    { "name": "AAPL", "rating": null, "sell": null }
  ]
}'
```

### **Odpověď (`200 OK`)**
```json
{
  "timestamp": "2024-03-20T18:25:43.511Z",
  "date_from": "2024-03-15T18:25:43.511Z",
  "date_to": "2024-03-20T18:25:43.511Z",
  "stocks": [
    { "name": "MSFT", "rating": 8, "sell": false },
    { "name": "AAPL", "rating": -5, "sell": true }
  ]
}
```
✅ **Vysvětlení odpovědi:**
- `rating: 8` → Pozitivní zprávy, není potřeba prodávat.
- `rating: -5` → Negativní sentiment, doporučeno **prodat**.

### **Chybové odpovědi**
| **Kód** | **Popis**                      |
|--------|--------------------------------|
| `400`  | Chybí data v `stocks`          |
| `500`  | Chyba komunikace s modulem Zprávy |

---

## **📌 3️⃣ Filtrování akcií (`POST /api/burza/filter`)**
### **Popis**
Aplikuje **filtry** na akcie:
1. Odfiltruje ty, které **poslední 3 dny klesaly**.
2. Odfiltruje akcie, které **měly více než 2 poklesy za posledních 5 dní**.

### **URL**
```
POST /api/burza/filter
```

### **Tělo požadavku**
```json
{
  "timestamp": "2024-03-20T18:25:43.511Z",
  "date_from": "2024-03-15T18:25:43.511Z",
  "date_to": "2024-03-20T18:25:43.511Z",
  "stocks": [
    { "name": "MSFT", "rating": null, "sell": null },
    { "name": "AAPL", "rating": null, "sell": null }
  ]
}
```

### **Příklad požadavku (`curl`)**
```sh
curl -X POST "http://localhost:5000/api/burza/filter" \
     -H "Content-Type: application/json" \
     -d '{
  "timestamp": "2024-03-20T18:25:43.511Z",
  "date_from": "2024-03-15T18:25:43.511Z",
  "date_to": "2024-03-20T18:25:43.511Z",
  "stocks": [
    { "name": "MSFT", "rating": null, "sell": null },
    { "name": "AAPL", "rating": null, "sell": null }
  ]
}'
```

### **Odpověď (`200 OK`)**
```json
{
  "timestamp": "2024-03-20T18:25:43.511Z",
  "date_from": "2024-03-15T18:25:43.511Z",
  "date_to": "2024-03-20T18:25:43.511Z",
  "stocks": [
    { "name": "MSFT", "sell": false },
    { "name": "AAPL", "sell": true }
  ]
}
```
✅ **Vysvětlení odpovědi:**
- `sell: false` → MSFT **nesplňuje podmínky pro prodej**.
- `sell: true` → AAPL **má negativní trend → doporučeno prodat**.

---

## **📌 Testování API**
### **1️⃣ Ověř, že backend běží**
```sh
dotnet run
```
📌 **Pokud vidíš `Now listening on: http://localhost:5000`, API běží.**

### **2️⃣ Otestuj API v prohlížeči**
Otevři **Swagger**:
```
http://localhost:5000/swagger
```

### **3️⃣ Testuj v Postmanu**
- `GET /api/burza/search?query=MSFT`
- `POST /api/burza/recommendation`
- `POST /api/burza/filter`

---

## **🔥 Shrnutí**
✅ **API běží na `http://localhost:5000/api/burza`**  
✅ **Swagger UI: `http://localhost:5000/swagger`**  
✅ **Testuj pomocí `curl` nebo Postmanu**  
✅ **Komunikuje s modulem Zprávy (`http://localhost:5001/api/zpravy/analyze`)**  

🚀 **Teď můžeš API plně využívat!** Pokud něco nefunguje, dej mi vědět. 💪
