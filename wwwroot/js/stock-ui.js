// wwwroot/js/stock-ui.js
import { fetchStocks, getRatings, sendRecommendations } from './api.js';

document.addEventListener('DOMContentLoaded', () => {
    const btnLoad = document.getElementById('btnLoad');
    const btnRate = document.getElementById('btnRate');
    const btnRec = document.getElementById('btnRec');
    const tblBody = document.querySelector('#stockTable tbody');
    const inputDays = document.getElementById('inputDays');
    const inputFrom = document.getElementById('inputFrom');
    const inputTo = document.getElementById('inputTo');
    const inputThr = document.getElementById('inputThreshold');

    let currentStocks = [];

    btnLoad.addEventListener('click', async () => {
        const days = parseInt(inputDays.value, 10) || 3;
        try {
            const list = await fetchStocks(days);
            currentStocks = list.map(s => s.name);
            renderTable(list.map(n => ({ name: n, rating: null, sell: null })));
        } catch (e) { alert(e.message); }
    });

    btnRate.addEventListener('click', async () => {
        try {
            const rated = await getRatings(inputFrom.value, inputTo.value, currentStocks);
            renderTable(rated.stocks);
        } catch (e) { alert(e.message); }
    });

    btnRec.addEventListener('click', async () => {
        const thr = parseInt(inputThr.value, 10);
        const toSell = [];
        tblBody.querySelectorAll('tr').forEach(row => {
            const name = row.dataset.name;
            const rating = parseInt(row.querySelector('.cell-rating').innerText, 10);
            if (!isNaN(rating) && rating > thr) {
                toSell.push({ name, sell: true });
            }
        });
        try {
            await sendRecommendations(inputFrom.value, inputTo.value, toSell);
            alert('Doporučení odesláno');
        } catch (e) { alert(e.message); }
    });

    function renderTable(stocks) {
        tblBody.innerHTML = '';
        stocks.forEach(s => {
            const r = document.createElement('tr');
            r.dataset.name = s.name;
            r.innerHTML = `
        <td>${s.name}</td>
        <td class="cell-rating">${s.rating ?? '–'}</td>
        <td>${s.sell == null ? '–' : (s.sell ? '👎' : '👍')}</td>
      `;
            tblBody.append(r);
        });
    }
});