curl -X POST -u "burza:velmitajneheslo" \
  'https://novinky.zumepro.cz:8000/api/liststock' \
  -H "Content-Type: application/json" \
  -d '{
  "timestamp": "2012-04-23T18:25:43.511Z",
  "date_from": "2012-04-23T18:25:43.511Z",
  "date_to": "2012-04-23T18:25:43.511Z",
  "stocks": [
    {
      "name": "MSFT",
      "rating": null,
      "sell": null
    },
    {
      "name": "HPQ",
      "rating": null,
      "sell": null
    },
    {
      "name": "TXN",
      "rating": null,
      "sell": null
    },
    {
      "name": "IDK",
      "rating": null,
      "sell": null
    }
  ]
}'