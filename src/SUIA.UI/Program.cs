using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SUIA.UI;
using SUIA.UI.Authentication;
using SUIA.UI.Services;
using Sysinfocus.AspNetCore.Components;
using System.Net.Http.Json;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var client = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
var settings = await client.GetFromJsonAsync<Settings>("appsettings.json");


builder.Services.AddScoped(sp => client);
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, SUIAAuthenticationStateProvider>();
builder.Services.AddTransient<IAPIService, APIService>();
builder.Services.AddScoped(_ => new Settings(settings!.ApiEndpoint));

builder.Services.AddSysinfocus();

await builder.Build().RunAsync();
