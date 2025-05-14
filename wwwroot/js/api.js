// wwwroot/js/api.js

export async function fetchStocks(days) {
    const resp = await fetch(`/api/liststock?days=${days}`);
    if (!resp.ok) throw new Error("Chyba při načítání seznamu akcií");
    return resp.json();
}

export async function getRatings(dateFrom, dateTo, stocks) {
    const body = {
        timestamp: new Date().toISOString(),
        date_from: dateFrom,
        date_to: dateTo,
        stocks: stocks.map(name => ({ name, rating: null, sell: null }))
    };
    const resp = await fetch('/api/getrating', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    });
    if (!resp.ok) throw new Error("Chyba při získání ratingů");
    return resp.json();
}

export async function sendRecommendations(dateFrom, dateTo, recs) {
    const body = {
        timestamp: new Date().toISOString(),
        date_from: dateFrom,
        date_to: dateTo,
        stocks: recs.map(r => ({ name: r.name, rating: null, sell: r.sell }))
    };
    const resp = await fetch('/api/sendrecommendations', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    });
    if (!resp.ok) throw new Error("Chyba při odesílání doporučení");
}