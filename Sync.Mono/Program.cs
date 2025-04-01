using Microsoft.AspNetCore.Components;
using Sync.Mono.Components;
using Sync.Mono.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add health checks
builder.Services.AddHealthChecks();

// Register our services
builder.Services.AddSingleton<IEditorService, EditorService>();
builder.Services.AddSingleton<WebSocketService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Map health check endpoint
app.MapHealthChecks("/health");

// Enable WebSockets
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
});

// Configure WebSocket endpoint
app.MapGet("/ws/{editorId}/{userId}", async (HttpContext context, string editorId, string userId, WebSocketService webSocketService) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        if (editorId != null && userId != null)
        {
            await webSocketService.HandleConnectionAsync(webSocket, editorId, userId);
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.MapBlazorHub();

// Important: This should be last in the pipeline
app.MapFallbackToPage("/_Host");

app.Run();
