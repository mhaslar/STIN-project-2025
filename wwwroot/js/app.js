// app.js
console.log("app.js načten!");

// 1) searchCache[query] => pole "bestMatches" z SYMBOL_SEARCH
// 2) stockDataCache[symbol] => TIME_SERIES_DAILY data
const searchCache = {};
const stockDataCache = {};
// Pro uložení poslední hodnoty a % změny, abychom mohli třídit
const modulesData = {};

// Globální pole firem
let searchResults = [];
let selectedSet = new Set();

const overlay = document.getElementById("overlay");
const companySearch = document.getElementById("companySearch");
const companyList = document.getElementById("companyList");
const selectedCompaniesDiv = document.getElementById("selectedCompanies");
const addToGridBtn = document.getElementById("addToGridBtn");
const modulesGrid = document.getElementById("modulesGrid");
const sendToApiBtn = document.getElementById("sendToApiBtn");

const dayRange = document.getElementById("dayRange");
const sortUpBtn = document.getElementById("sortUpBtn");
const sortDownBtn = document.getElementById("sortDownBtn");

// Modal
const detailModal = document.getElementById("detailModal");
const modalCloseBtn = document.getElementById("modalCloseBtn");
const modalTitle = document.getElementById("modalTitle");
const modalBody = document.getElementById("modalBody");

// === DARK MODE toggle ===
const darkModeCheckbox = document.getElementById("darkModeCheckbox");
if (darkModeCheckbox) {
  darkModeCheckbox.addEventListener("change", () => {
    document.body.classList.toggle("dark-mode", darkModeCheckbox.checked);
  });
}

// 2) DEBOUNCE vyhledávání + min. počet znaků
let debounceTimer;
companySearch.addEventListener("input", () => {
  clearTimeout(debounceTimer);

  const query = companySearch.value.trim();
  if (query.length < 3) {
    companyList.innerHTML = "";
    return;
  }

  debounceTimer = setTimeout(() => {
    searchCompanies(query).then((results) => {
      searchResults = results;
      renderCompanyList();
    });
  }, 400);
});

// 3) Tlačítko Přidat do Gridu
/*addToGridBtn.addEventListener("click", () => {
  // Odstranění modulů, které už nejsou ve selectedSet
  const existingModules = Array.from(modulesGrid.querySelectorAll(".module"));
  existingModules.forEach((m) => {
    const symbol = m.id.replace("mod-", "");
    if (!selectedSet.has(symbol)) m.remove();
  });

  // Přidání modulů, které chybí
  selectedSet.forEach((symbol) => {
    if (!document.getElementById("mod-" + symbol)) {
      addModule(symbol);
    }
  });
});*/

// 4) Vykreslení seznamu firem (výsledky vyhledávání)
function renderCompanyList() {
  companyList.innerHTML = "";
  searchResults.forEach((item) => {
    const symbol = item["1. symbol"];
    const name = item["2. name"];
    const li = document.createElement("li");
    li.textContent = `${name} (${symbol})`;
    li.dataset.symbol = symbol;

    if (selectedSet.has(symbol)) {
      li.classList.add("selected");
    }
    li.addEventListener("click", () => {
      selectedSet.add(symbol);
      addModule(symbol);
      showSelectedCompanies();
    });
    companyList.appendChild(li);
  });
}

// 5) Zobrazení vybraných Akcií
function showSelectedCompanies() {
  selectedCompaniesDiv.innerHTML = "";
  selectedSet.forEach((symbol) => {
    const found = searchResults.find((i) => i["1. symbol"] === symbol);
    const name = found ? found["2. name"] : symbol;

    const chip = document.createElement("div");
    chip.classList.add("chip");
    chip.textContent = name;

    const closeSpan = document.createElement("span");
    closeSpan.classList.add("chip-close");
    closeSpan.textContent = "✕";
    closeSpan.addEventListener("click", (e) => {
      e.stopPropagation();
      selectedSet.delete(symbol);
      showSelectedCompanies();
      const li = companyList.querySelector(`li[data-symbol="${symbol}"]`);
      if (li) li.classList.remove("selected");
      const mod = document.getElementById(`mod-${symbol}`);
      if (mod) mod.remove();
    });

    chip.appendChild(closeSpan);
    selectedCompaniesDiv.appendChild(chip);
  });
}

// 6) Vytvoření modulu (Akcie) s Plotly grafem
function addModule(symbol) {
  if (document.getElementById("mod-" + symbol)) return;

  const found = searchResults.find((i) => i["1. symbol"] === symbol);
  const name = found ? found["2. name"] : symbol;

  const moduleElem = document.createElement("article");
  moduleElem.classList.add("module");
  moduleElem.id = "mod-" + symbol;
  moduleElem.dataset.name = name;

  moduleElem.innerHTML = `
    <div class="module-remove" title="Odstranit modul">×</div>
    <div class="module-top">
      ${name} - ${symbol}
    </div>
    <div class="module-middle">
      <!-- Div pro Plotly graf -->
      <div id="plot-${symbol}" style="width:100%; height:100%;"></div>
    </div>
    <div class="module-bottom" id="info-${symbol}">
      Hodnota: Loading... <br/>
      Změna: Loading...
    </div>
  `;

  // Kliknutí na modul (kromě "x") => detail
  moduleElem.addEventListener("click", (e) => {
    if (e.target.classList.contains("module-remove")) return;
    showDetailModal({ symbol, name });
  });

  // Kliknutí na "x" => odstranění modulu
  const removeBtn = moduleElem.querySelector(".module-remove");
  removeBtn.addEventListener("click", (e) => {
    e.stopPropagation();
    selectedSet.delete(symbol);
    moduleElem.remove();
    showSelectedCompanies();
  });

  console.log("Přidávám modul", symbol);
  modulesGrid.appendChild(moduleElem);

  // Načtení dat z Alphavantage
  fetchStockData(symbol)
    .then((data) => {
      renderPlotlyCandlestick(symbol, data, name);
      updateStockInfo(symbol, data);
    })
    .catch((err) => {
      console.error("Chyba data pro", symbol, err);
    });
}

// 7) Kešované vyhledání firem (SYMBOL_SEARCH)
function searchCompanies(query) {
  const apiKey = "JME1OBSYLJIEQAW6";
  if (searchCache[query]) {
    console.log("Používám keš pro search:", query);
    return Promise.resolve(searchCache[query]);
  }
  const url = `https://www.alphavantage.co/query?function=SYMBOL_SEARCH&keywords=${query}&apikey=${apiKey}`;
  return fetch(url)
    .then((resp) => resp.json())
    .then((json) => {
      if (!json.bestMatches) return [];
      const best = json.bestMatches;
      searchCache[query] = best;
      return best;
    })
    .catch((err) => {
      console.error("Chyba searchCompanies", err);
      return [];
    });
}

// 8) Kešované fetchStockData (TIME_SERIES_DAILY)
function fetchStockData(symbol) {
  const apiKey = "JME1OBSYLJIEQAW6";
  if (stockDataCache[symbol]) {
    console.log("Používám keš pro symbol:", symbol);
    return Promise.resolve(stockDataCache[symbol]);
  }
  const url = `https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol=${symbol}&apikey=${apiKey}`;
  return fetch(url)
    .then((r) => r.json())
    .then((data) => {
      stockDataCache[symbol] = data;
      return data;
    });
}

// 9) Plotly Candlestick
function renderPlotlyCandlestick(symbol, data, name) {
  const series = data["Time Series (Daily)"];
  if (!series) {
    console.error("Chybná data pro", symbol, data);
    return;
  }

  const dates = [];
  const openArr = [];
  const highArr = [];
  const lowArr = [];
  const closeArr = [];

  // Seřaď klíče (datumy) a naplň pole
  const sortedKeys = Object.keys(series).sort();
  sortedKeys.forEach((dateStr) => {
    dates.push(dateStr);
    openArr.push(parseFloat(series[dateStr]["1. open"]));
    highArr.push(parseFloat(series[dateStr]["2. high"]));
    lowArr.push(parseFloat(series[dateStr]["3. low"]));
    closeArr.push(parseFloat(series[dateStr]["4. close"]));
  });

  // Plotly
  const trace = {
    x: dates,
    open: openArr,
    high: highArr,
    low: lowArr,
    close: closeArr,
    type: "candlestick",
  };

  const layout = {
    title: `${name} - ${symbol}`,
    dragmode: "pan",
    margin: { l: 40, r: 10, t: 40, b: 40 },
    paper_bgcolor: "rgba(0,0,0,0)",
    plot_bgcolor: "rgba(0,0,0,0)",
    font: { color: "#999" },
  };

  Plotly.newPlot(`plot-${symbol}`, [trace], layout);
}

// 10) updateStockInfo
function updateStockInfo(symbol, data) {
  const series = data["Time Series (Daily)"];
  if (!series) return;

  const dates = Object.keys(series).sort();
  if (dates.length < 2) return;

  const latestDate = dates[dates.length - 1];
  const prevDate = dates[dates.length - 2];
  const latestClose = parseFloat(series[latestDate]["4. close"]);
  const prevClose = parseFloat(series[prevDate]["4. close"]);
  const changePerc = ((latestClose - prevClose) / prevClose) * 100;

  modulesData[symbol] = {
    lastClose: latestClose,
    changePerc: changePerc,
  };

  const infoElem = document.getElementById(`info-${symbol}`);
  if (infoElem) {
    infoElem.innerHTML = `
      Hodnota: $${latestClose.toFixed(2)} <br/>
      Změna: ${changePerc.toFixed(2)}%
    `;
  }
}



// 14) Filtr pro API
function filterCompanies(companies, type) {
  return '{"stocks": ["APPL", "GOOGL"], "type": 3}'; // TODO: implementovat
}

function CallListStockAPI() {
  function toISOStringNoMs(dt) {
    return dt.toISOString().split(".")[0] + "+00:00";
  }
    sendToApiBtn.addEventListener("click", () => {
      // 1) Zavoláme filterCompanies
      const { stocks, type } = JSON.parse(filterCompanies());

      // 2) Spočítáme časy
      const now = new Date();
      const timestamp = toISOStringNoMs(now);
      const dateFromDt = new Date(now.getTime() - type * 24 * 60 * 60 * 1000);
      const date_from = toISOStringNoMs(dateFromDt);
      const date_to = timestamp;

      // 3) Sestavíme payload
      const payload = {
        timestamp,
        date_from,
        date_to,
        stocks: stocks.map((name) => ({
          name,
          rating: null,
          sell: null,
        })),
      };

      console.log("Odesílám payload:", payload);
      console.log("JSON.stringify:", JSON.stringify(payload, null, 2));
      // 4) Odešleme na API
      fetch("https://novinky.zumepro.cz:8000/api/", {
        method: "POST",
        headers: { "burza": "velmitajneheslo" },
        body: JSON.stringify(payload),
      })
        .then((resp) => {
          if (!resp.ok) throw new Error(`HTTP ${resp.status}`);
          return resp.json();
        })
        .then((data) => {
          console.log("API odpověď:", data);
          alert("Testovací data úspěšně odeslána!");
        })
        .catch((err) => {
          console.error("Chyba při odesílání:", err);
          alert("Nepodařilo se odeslat testovací data.");
        });
    });
  }

if (!sendToApiBtn) {
    console.error("sendToApiBtn Není!");
  } else {
    console.log("sendToApiBtn Načteno:", sendToApiBtn);
  }