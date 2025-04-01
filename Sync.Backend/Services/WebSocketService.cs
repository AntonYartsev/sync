using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Sync.Backend.Services
{
    public class WebSocketService
    {
        private readonly ILogger<WebSocketService> _logger;
        private readonly IEditorService _editorService;
        private readonly ConcurrentDictionary<string, HashSet<WebSocket>> _editorConnections = new();

        public WebSocketService(ILogger<WebSocketService> logger, IEditorService editorService)
        {
            _logger = logger;
            _editorService = editorService;
        }

        public async Task HandleWebSocketConnection(WebSocket webSocket, string editorId, string userId)
        {
            try
            {
                // Add user to connected users
                _editorService.AddUser(editorId, userId);
                _logger.LogInformation("User {UserId} connected to editor {EditorId}", userId, editorId);
                
                // Add WebSocket to connections
                _editorConnections.AddOrUpdate(
                    editorId,
                    new HashSet<WebSocket> { webSocket },
                    (_, connections) =>
                    {
                        connections.Add(webSocket);
                        return connections;
                    });

                // Send current content to the new user
                var currentContent = _editorService.GetContent(editorId);
                if (!string.IsNullOrEmpty(currentContent))
                {
                    var message = new EditorUpdateMessage
                    {
                        Type = "contentUpdate",
                        Content = currentContent
                    };
                    await SendMessageAsync(webSocket, message);
                    _logger.LogInformation("Sent initial content to user {UserId} in editor {EditorId}", userId, editorId);
                }

                // Handle incoming messages
                var buffer = new byte[1024 * 4];
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await HandleDisconnection(webSocket, editorId, userId);
                        break;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _logger.LogDebug("Received message from user {UserId} in editor {EditorId}: {Message}", 
                        userId, editorId, message);
                    await ProcessMessage(editorId, message, userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling WebSocket connection for user {UserId} in editor {EditorId}", 
                    userId, editorId);
                await HandleDisconnection(webSocket, editorId, userId);
            }
        }

        private async Task ProcessMessage(string editorId, string message, string userId)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var update = JsonSerializer.Deserialize<EditorUpdateMessage>(message, options);
                if (update != null)
                {
                    _logger.LogDebug("Processing message from user {UserId} in editor {EditorId}: Type={Type}, ContentLength={ContentLength}", 
                        userId, editorId, update.Type, update.Content?.Length ?? 0);

                    // Update content in service
                    _editorService.UpdateContent(editorId, update.Content);

                    // Broadcast to all connected users
                    if (_editorConnections.TryGetValue(editorId, out var connections))
                    {
                        foreach (var connection in connections)
                        {
                            if (connection.State == WebSocketState.Open)
                            {
                                await SendMessageAsync(connection, update);
                            }
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to deserialize message from user {UserId} in editor {EditorId}: {Message}", 
                        userId, editorId, message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from user {UserId} in editor {EditorId}: {Message}", 
                    userId, editorId, message);
            }
        }

        private async Task HandleDisconnection(WebSocket webSocket, string editorId, string userId)
        {
            _logger.LogInformation("User {UserId} disconnecting from editor {EditorId}", userId, editorId);
            
            if (_editorConnections.TryGetValue(editorId, out var connections))
            {
                connections.Remove(webSocket);
                if (connections.Count == 0)
                {
                    _editorConnections.TryRemove(editorId, out _);
                }
            }

            _editorService.RemoveUser(editorId, userId);
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "User disconnected", CancellationToken.None);
        }

        private async Task SendMessageAsync(WebSocket webSocket, EditorUpdateMessage message)
        {
            try
            {
                var json = JsonSerializer.Serialize(message);
                var buffer = Encoding.UTF8.GetBytes(json);
                await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to WebSocket");
            }
        }
    }

    public class EditorUpdateMessage
    {
        public required string Type { get; set; }
        public required string Content { get; set; }
    }
} 