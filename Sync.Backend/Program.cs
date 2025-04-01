using Microsoft.AspNetCore.Cors;
using Sync.Backend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add WebSocket service
builder.Services.AddSingleton<WebSocketService>();

// Configure CORS
var corsOrigins = builder.Configuration.GetValue<string>("CORS_ORIGINS")?.Split(',') ?? 
                 new[] { "http://localhost:5025" };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Register our services
builder.Services.AddSingleton<IEditorService, EditorService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

// Add WebSocket middleware
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
});

// Configure WebSocket endpoint
app.Map("/ws/{editorId}/{userId}", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var editorId = context.Request.RouteValues["editorId"]?.ToString();
        var userId = context.Request.RouteValues["userId"]?.ToString();

        if (editorId != null && userId != null)
        {
            var webSocketService = context.RequestServices.GetRequiredService<WebSocketService>();
            await webSocketService.HandleWebSocketConnection(webSocket, editorId, userId);
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

app.Run();
