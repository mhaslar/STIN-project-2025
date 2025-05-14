using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using STIN_Burza.Services;
using System.Reflection;
using System;
using System.Net.Http.Headers;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Služby
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddHttpClient();
// Konfigurace klienta pro API Zpráv
var zpravySection = builder.Configuration.GetSection("ZpravyApi");
builder.Services.AddHttpClient<ZpravyApiClient>(client =>
{
    client.BaseAddress = new Uri(zpravySection["Url"]);
    var creds = Convert.ToBase64String(
        Encoding.ASCII.GetBytes($"{zpravySection["Username"]}:{zpravySection["Password"]}")
    );
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Basic", creds);
});
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<StockService>();
builder.Services.AddHostedService<BackgroundStockFetcher>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Volitelně: zahrnout XML komentáře
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
});

var app = builder.Build();

// Middleware
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Swagger (i mimo development!)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "STIN Burza API V1");
    c.RoutePrefix = "swagger"; // běží na /swagger
});

// Error handling mimo development
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Error/{0}");
app.UseAuthorization();

// Endpointy
app.MapControllers();
app.MapRazorPages();

app.Run();
