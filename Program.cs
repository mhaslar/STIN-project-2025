using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using STIN_Burza.Services;

var builder = WebApplication.CreateBuilder(args);

// ✅ Přidání MVC kontrolérů
builder.Services.AddControllers();

// ✅ Přidání Swaggeru
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<StockService>();
builder.Services.AddHostedService<BackgroundStockFetcher>();

var app = builder.Build();

// ✅ Povolení Swaggeru i v produkčním režimu
app.UseSwagger();
app.UseSwaggerUI();

// ✅ Správné mapování kontrolérů
app.MapControllers();

app.Run();
