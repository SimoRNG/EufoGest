using EufoGestWebApp;
using EufoGestWebApp.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");


// API del servizio REST a cui connettersi
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? throw new InvalidOperationException("ApiBaseUrl non configurato in appsettings.");
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
});

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AnagraficheService>();
builder.Services.AddScoped<LingueService>();
builder.Services.AddScoped<UtentiService>();
builder.Services.AddScoped<LezioniService>();
builder.Services.AddScoped<AuleService>();
builder.Services.AddScoped<PacchettiOreService>();

await builder.Build().RunAsync();