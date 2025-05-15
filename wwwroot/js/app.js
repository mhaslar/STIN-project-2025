console.log("app.js načten!");

// 1) searchCache[query] => pole "bestMatches" z SYMBOL_SEARCH
// 2) stockDataCache[symbol] => TIME_SERIES_DAILY data
const searchCache = {};
const stockDataCache = {};
// Pro uložení poslední hodnoty a % změny, abychom mohli třídit
const modulesData = {};

var typFiltru = 3; // defaultně 3 dny

var threshold = nactiThreshold(); // Hranice pro určení hodnoty sell
document.getElementById("threshold").innerHTML = threshold;

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

const modeSwitcher = document.getElementById("modeSwitcher");

// === DARK MODE toggle ===
const darkModeCheckbox = document.getElementById("darkModeCheckbox");
if (darkModeCheckbox) {
  darkModeCheckbox.addEventListener("change", () => {
    document.body.classList.toggle("dark-mode", darkModeCheckbox.checked);
  });
}

modeSwitcher.addEventListener("click", () => {
  if (document.getElementById("modeSwitcherSpan").innerHTML === "3 dny") {
    document.getElementById("modeSwitcherSpan").innerHTML = "5 dní";
    typFiltru = 5;
  } else {
    document.getElementById("modeSwitcherSpan").innerHTML = "3 dny";
    typFiltru = 3;
  }
});

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
function toISOStringNoMs(dt) {
  return dt.toISOString().split('.')[0] + '+00:00';
}

// 2) Sestaví JSON podle modulů
// 2) Sestaví JSON podle modulů a aplikuje filtr podle typFiltru
function filterCompanies() {
  // 1) sebereme všechny symboly z existujících modulů
  const modules = document.getElementsByClassName('module');
  const symbols = Array.from(modules)
    .map(mod => mod.id.replace('mod-', ''));

  // 2) projdeme každý symbol a zkontrolujeme data v cache
  const filtered = symbols.filter(symbol => {
    const data = stockDataCache[symbol];
    if (!data || !data["Time Series (Daily)"]) return false;

    // Time Series
    const series = data["Time Series (Daily)"];
    // seřazené datumy (trading days)
    const dates = Object.keys(series).sort();

    // helper: vyextrahuje pole close hodnot pro posledních N+1 dní
    const getCloses = (n) => {
      const slice = dates.slice(- (n + 1)); // N+1 dat pro N porovnání
      return slice.map(d => parseFloat(series[d]["4. close"]));
    };

    if (typFiltru === 3) {
      // potřebujeme 4 data pro 3 po sobě jdoucí poklesy
      if (dates.length < 4) return false;
      const closes = getCloses(3);
      // tři poklesy: c0>c1, c1>c2, c2>c3
      return closes[0] > closes[1]
        && closes[1] > closes[2]
        && closes[2] > closes[3];
    }
    else if (typFiltru === 5) {
      // potřebujeme 6 dat pro 5 intervalů, ze kterých spočítáme poklesy
      if (dates.length < 6) return false;
      const closes = getCloses(5);
      // spočítáme, kolikrát c[i] > c[i+1]
      let declines = 0;
      for (let i = 0; i < closes.length - 1; i++) {
        if (closes[i] > closes[i + 1]) declines++;
      }
      // chceme více než 2 poklesy
      return declines > 2;
    }

    // ostatní typy filtru (pokud by se přidaly) vrátí false
    return false;
  });

  // 3) vrátíme JSON se seznamem filtrovaných firem a aktuálním typFiltru
  return JSON.stringify({
    stocks: filtered,
    type: typFiltru
  });
}


// 3) Tohle zavolá přímo inline onclick – **žádné další addEventListenery zde**
function CallListStockAPI() {
  // a) rozparsovat
  const { stocks, type } = JSON.parse(filterCompanies());
  console.log('Vybrané akcie:', stocks, 'typ:', type);

  // b) timestampy
  const now = new Date();
  const timestamp = toISOStringNoMs(now);
  const date_from = toISOStringNoMs(new Date(now.getTime() - type * 24 * 60 * 60 * 1000));
  const date_to = timestamp;

  // c) payload
  const payload = {
    timestamp,
    date_from,
    date_to,
    stocks: stocks.map(name => ({ name, rating: null, sell: null }))
  };

  console.log('Odesílám payload:', payload);
  console.log('JSON.stringify:', JSON.stringify(payload, null, 2));

  // d) fetch na proxy endpoint
  fetch('/api/burza/liststock', {
    method: 'POST',
    headers: {
      burza: 'velmitajneheslo',
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(payload)
  })
    .then(resp => {
      console.log('Adresa API:', resp.url);
      if (!resp.ok) throw new Error(`HTTP ${resp.status}`);
      return resp.json();
    })
    .then(data => {
      console.log('API odpověď:', data);
      alert('Testovací data úspěšně odeslána!');
    })
    .catch(err => {
      console.error('Chyba při odesílání:', err);
      alert('Nepodařilo se odeslat testovací data.');
    });
}

function zvysHranici() {
  if (threshold != 10) {
    threshold += 1;
    document.getElementById("threshold").innerHTML = threshold;
    console.log("Zvýšení hranice na", threshold);
    console.log(JSON.stringify({ threshold }));

    fetch('/api/burza/setThreshold', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ threshold })
    })
      .then(resp => {
        console.log('Adresa API:', resp.url);
        if (!resp.ok) throw new Error(`HTTP ${resp.status}`);
        return resp;
      })
      .then(data => {
        console.log('API odpověď:', data);
        //alert('Hranice úspěšně zvýšena!');
      })
      .catch(err => {
        console.error('Chyba při zvyšování hranice:', err);
        alert('Nepodařilo se zvýšit hranici.');
      });
  }
}

function snizHranici() {
  if (threshold != -10) {
    threshold -= 1;
    document.getElementById("threshold").innerHTML = threshold;
    console.log("Snížení hranice na", threshold);
    console.log(JSON.stringify({ threshold }));

    fetch('/api/burza/setThreshold', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ threshold })
    })
      .then(resp => {
        console.log('Adresa API:', resp.url);
        if (!resp.ok) throw new Error(`HTTP ${resp.status}`);
        return resp;
      })
      .then(data => {
        console.log('API odpověď:', data);
        //alert('Hranice úspěšně snížena!');
      })
      .catch(err => {
        console.error('Chyba při snižování hranice:', err);
        alert('Nepodařilo se snížit hranici.');
      });
  }
}

function nactiThreshold() {
  fetch('/api/burza/getThreshold')
    .then(resp => {
      console.log('Adresa API:', resp.url);
      if (!resp.ok) throw new Error(`HTTP ${resp.status}`);
      return resp.json();
    })
    .then(data => {
      console.log('API odpověď:', data);
      threshold = data.threshold;
      document.getElementById("threshold").innerHTML = threshold;
    })
    .catch(err => {
      console.error('Chyba při načítání hranice:', err);
      alert('Nepodařilo se načíst hranici.');
    });
}

function toggleVolbuHranice() {
  const elementy = document.querySelectorAll(".zmenaHodnoty");
  elementy.forEach((element) => {
    if (element.style.width === "0%") {
      element.style.width = "33%";
    } else {
      element.style.width = "0%";
    }
  });
}

function zavriVolbuHranice() {
  const elementy = document.querySelectorAll(".zmenaHodnoty");
  elementy.forEach((element) => {
    element.style.width = "0%";
  });
}

function otevriVolbuHranice() {
  console.log("otevriVolbuHranice");
  const elementy = document.querySelectorAll(".zmenaHodnoty");
  elementy.forEach((element) => {
    element.style.width = "33%";
  });
}

if (!sendToApiBtn) {
  console.error("sendToApiBtn Není!");
} else {
  console.log("sendToApiBtn Načteno:");
}