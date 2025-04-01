using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sync.Frontend;
using Sync.Frontend.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Get the backend URL from environment or configuration
var backendUrl = Environment.GetEnvironmentVariable("BACKEND_URL") ?? 
                 builder.Configuration["BackendUrl"] ?? 
                 builder.HostEnvironment.BaseAddress.Replace("5025", "5001");

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Настраиваем базовый адрес для HTTP клиента
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(backendUrl) });

// Регистрируем сервисы
builder.Services.AddScoped<IEditorService, EditorService>();
builder.Services.AddScoped<WebSocketService>();

Console.WriteLine($"Application starting with base address: {backendUrl}");

await builder.Build().RunAsync();
