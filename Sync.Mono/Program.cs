using Microsoft.AspNetCore.Components;
using Sync.Mono.Components;
using Sync.Mono.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddHealthChecks();

builder.Services.AddSingleton<IEditorService, EditorService>();
builder.Services.AddSingleton<WebSocketService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapHealthChecks("/health");

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
});

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

app.MapFallbackToPage("/_Host");

app.Run();
