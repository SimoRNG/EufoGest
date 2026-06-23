//using EufoGestWeb.Client.Pages.Services;
using EufoGestWeb.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

//builder.Services.AddScoped<AuthHandler>();
builder.Services.AddScoped<AuthService>();

builder.Services.AddScoped(sp =>
{
    var auth = sp.GetRequiredService<AuthService>();
    var nav = sp.GetRequiredService<NavigationManager>();
    //var handler = new AuthHandler(auth, nav)
    //{
    //    InnerHandler = new HttpClientHandler()
    //    //{
    //    //    UseCookies = true,
    //    //    CookieContainer = new System.Net.CookieContainer()
    //    //}
    //};
    return new HttpClient()//handler)
    {
        BaseAddress = new Uri("https://localhost:7106/")
    };
});

await builder.Build().RunAsync();
