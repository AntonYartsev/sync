using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Sync.Mono.Models;

namespace Sync.Mono.Services;

public class WebSocketService
{
    private readonly ILogger<WebSocketService> _logger;
    private readonly IEditorService _editorService;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, WebSocket>> _connections = new();

    public WebSocketService(ILogger<WebSocketService> logger, IEditorService editorService)
    {
        _logger = logger;
        _editorService = editorService;
    }

    public async Task HandleConnectionAsync(WebSocket webSocket, string editorId, string userId)
    {
        try
        {
            await _editorService.AddConnectedUserAsync(editorId, userId);
            
            if (!_connections.TryGetValue(editorId, out var editorConnections))
            {
                editorConnections = new ConcurrentDictionary<string, WebSocket>();
                _connections[editorId] = editorConnections;
            }
            
            editorConnections[userId] = webSocket;
            
            var editor = await _editorService.GetEditorStateAsync(editorId);
            await SendToClientAsync(webSocket, new EditorUpdateMessage
            {
                Type = "contentUpdate",
                Content = editor.Content,
                Language = editor.Language,
                ConnectedUsers = editor.ConnectedUsers
            });
            
            await BroadcastConnectedUsersAsync(editorId);
            
            var buffer = new byte[4096];
            var receiveResult = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!receiveResult.CloseStatus.HasValue)
            {
                if (receiveResult.MessageType == WebSocketMessageType.Text)
                {
                    var messageJson = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                    await HandleMessageAsync(editorId, userId, messageJson);
                }
                
                receiveResult = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            
            await CleanupConnectionAsync(editorId, userId, webSocket, receiveResult.CloseStatus.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in WebSocket connection for editor {EditorId}, user {UserId}", editorId, userId);
            await CleanupConnectionAsync(editorId, userId, webSocket, WebSocketCloseStatus.InternalServerError);
        }
    }
    
    private async Task HandleMessageAsync(string editorId, string userId, string messageJson)
    {
        try
        {
            var message = JsonSerializer.Deserialize<EditorUpdateMessage>(messageJson);
            
            if (message == null)
            {
                _logger.LogWarning("Received null message from {UserId} in editor {EditorId}", userId, editorId);
                return;
            }
            
            if (message.Type == "contentUpdate" && !string.IsNullOrEmpty(message.Content))
            {
                await _editorService.UpdateEditorStateAsync(editorId, message.Content);
                await BroadcastContentUpdateAsync(editorId, message.Content, excludeUserId: userId);
            }
            else if (message.Type == "languageUpdate" && !string.IsNullOrEmpty(message.Language))
            {
                await _editorService.SetLanguageAsync(editorId, message.Language);
                await BroadcastLanguageUpdateAsync(editorId, message.Language);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message from {UserId} in editor {EditorId}", userId, editorId);
        }
    }
    
    private async Task CleanupConnectionAsync(string editorId, string userId, WebSocket webSocket, WebSocketCloseStatus closeStatus)
    {
        try
        {
            if (_connections.TryGetValue(editorId, out var editorConnections))
            {
                editorConnections.TryRemove(userId, out _);
                
                if (editorConnections.IsEmpty)
                {
                    _connections.TryRemove(editorId, out _);
                }
            }
            
            await _editorService.RemoveConnectedUserAsync(editorId, userId);
            
            if (webSocket.State == WebSocketState.Open)
            {
                await webSocket.CloseAsync(
                    closeStatus,
                    "Connection closed by the server",
                    CancellationToken.None);
            }
            
            await BroadcastConnectedUsersAsync(editorId);
            
            _logger.LogInformation("WebSocket connection closed for user {UserId} in editor {EditorId}", userId, editorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up WebSocket connection for editor {EditorId}, user {UserId}", editorId, userId);
        }
    }
    
    private async Task BroadcastContentUpdateAsync(string editorId, string content, string? excludeUserId = null)
    {
        if (_connections.TryGetValue(editorId, out var editorConnections))
        {
            var message = new EditorUpdateMessage
            {
                Type = "contentUpdate",
                Content = content
            };
            
            await BroadcastMessageAsync(editorConnections, message, excludeUserId);
        }
    }
    
    private async Task BroadcastLanguageUpdateAsync(string editorId, string language)
    {
        if (_connections.TryGetValue(editorId, out var editorConnections))
        {
            var message = new EditorUpdateMessage
            {
                Type = "languageUpdate",
                Language = language
            };
            
            await BroadcastMessageAsync(editorConnections, message);
        }
    }
    
    private async Task BroadcastConnectedUsersAsync(string editorId)
    {
        try
        {
            if (_connections.TryGetValue(editorId, out var editorConnections))
            {
                var connectedUsers = await _editorService.GetConnectedUsersAsync(editorId);
                
                var message = new EditorUpdateMessage
                {
                    Type = "usersUpdate",
                    ConnectedUsers = new HashSet<string>(connectedUsers)
                };
                
                await BroadcastMessageAsync(editorConnections, message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting connected users for editor {EditorId}", editorId);
        }
    }
    
    private async Task BroadcastMessageAsync(
        ConcurrentDictionary<string, WebSocket> connections,
        EditorUpdateMessage message,
        string? excludeUserId = null)
    {
        var messageJson = JsonSerializer.Serialize(message);
        var messageBytes = Encoding.UTF8.GetBytes(messageJson);
        
        foreach (var (userId, socket) in connections)
        {
            if (excludeUserId == userId)
            {
                continue;
            }
            
            if (socket.State == WebSocketState.Open)
            {
                try
                {
                    await socket.SendAsync(
                        new ArraySegment<byte>(messageBytes),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending message to user {UserId}", userId);
                }
            }
        }
    }
    
    private async Task SendToClientAsync(WebSocket socket, EditorUpdateMessage message)
    {
        if (socket.State != WebSocketState.Open)
        {
            return;
        }
        
        try
        {
            var messageJson = JsonSerializer.Serialize(message);
            var messageBytes = Encoding.UTF8.GetBytes(messageJson);
            
            await socket.SendAsync(
                new ArraySegment<byte>(messageBytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to client");
        }
    }
} 