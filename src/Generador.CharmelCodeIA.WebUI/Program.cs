using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Generador.CharmelCodeIA.WebUI;
using Generador.CharmelCodeIA.WebUI.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;

builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

await builder.Build().RunAsync();
