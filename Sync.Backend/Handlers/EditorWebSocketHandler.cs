using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Sync.Backend.Models;
using Sync.Backend.Services;

namespace Sync.Backend.Handlers;

public class EditorWebSocketHandler
{
    private readonly IEditorService _editorService;
    private readonly ILogger<EditorWebSocketHandler> _logger;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, WebSocket>> _sessionClients = new();

    public EditorWebSocketHandler(IEditorService editorService, ILogger<EditorWebSocketHandler> logger)
    {
        _editorService = editorService;
        _logger = logger;
    }

    public async Task HandleWebSocketConnectionAsync(WebSocket webSocket, string editorId, string userId)
    {
        var sessionClients = _sessionClients.GetOrAdd(editorId, _ => new ConcurrentDictionary<string, WebSocket>());
        sessionClients.TryAdd(userId, webSocket);
        
        await _editorService.AddUserToEditorAsync(editorId, userId);

        try
        {
            await HandleWebSocketMessagesAsync(webSocket, editorId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling WebSocket connection");
        }
        finally
        {
            await CleanupConnectionAsync(editorId, userId);
        }
    }

    private async Task HandleWebSocketMessagesAsync(WebSocket webSocket, string editorId, string userId)
    {
        var buffer = new byte[1024 * 4];
        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client requested close", CancellationToken.None);
                break;
            }

            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            await ProcessMessageAsync(editorId, userId, message);
        }
    }

    private async Task ProcessMessageAsync(string editorId, string userId, string message)
    {
        try
        {
            var update = JsonSerializer.Deserialize<EditorUpdate>(message);
            if (update != null)
            {
                await _editorService.UpdateEditorStateAsync(editorId, update.Content);
                await BroadcastUpdateAsync(editorId, update);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
        }
    }

    private async Task BroadcastUpdateAsync(string editorId, EditorUpdate update)
    {
        var message = JsonSerializer.Serialize(update);
        var buffer = Encoding.UTF8.GetBytes(message);

        if (_sessionClients.TryGetValue(editorId, out var sessionClients))
        {
            foreach (var client in sessionClients)
            {
                if (client.Value.State == WebSocketState.Open)
                {
                    await client.Value.SendAsync(
                        new ArraySegment<byte>(buffer),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);
                }
            }
        }
    }

    private async Task CleanupConnectionAsync(string editorId, string userId)
    {
        if (_sessionClients.TryGetValue(editorId, out var sessionClients))
        {
            sessionClients.TryRemove(userId, out _);
            
            if (sessionClients.IsEmpty)
            {
                _sessionClients.TryRemove(editorId, out _);
            }
        }
        
        await _editorService.RemoveUserFromEditorAsync(editorId, userId);
    }
}

public class EditorUpdate
{
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
} 