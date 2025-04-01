using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Sync.Frontend.Models;

namespace Sync.Frontend.Services
{
    public class WebSocketService : IDisposable
    {
        private ClientWebSocket? _webSocket;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isConnected = false;
        private string _currentEditorId = string.Empty;
        private string _currentUserId = string.Empty;

        public event Action<string>? OnContentUpdated;
        public event Action<string>? OnConnectionStatusChanged;

        public async Task ConnectAsync(string editorId, string userId)
        {
            if (_isConnected)
            {
                await DisconnectAsync();
            }

            _currentEditorId = editorId;
            _currentUserId = userId;
            _cancellationTokenSource = new CancellationTokenSource();
            _webSocket = new ClientWebSocket();

            try
            {
                var backendUrl = Environment.GetEnvironmentVariable("BACKEND_URL") ?? "http://localhost:5001";
                var wsUrl = backendUrl.Replace("http", "ws");
                var uri = new Uri($"{wsUrl}/ws/{editorId}/{userId}");
                await _webSocket.ConnectAsync(uri, _cancellationTokenSource.Token);
                _isConnected = true;
                OnConnectionStatusChanged?.Invoke("Connected");

                _ = ReceiveMessagesAsync();
            }
            catch (Exception ex)
            {
                OnConnectionStatusChanged?.Invoke($"Connection failed: {ex.Message}");
                _isConnected = false;
            }
        }

        public async Task SendUpdateAsync(string content)
        {
            if (!_isConnected || _webSocket == null)
            {
                OnConnectionStatusChanged?.Invoke("Not connected");
                return;
            }

            try
            {
                var update = new EditorUpdateMessage 
                { 
                    Type = "contentUpdate",
                    Content = content 
                };
                var json = JsonSerializer.Serialize(update, _jsonOptions);
                var buffer = Encoding.UTF8.GetBytes(json);
                await _webSocket.SendAsync(
                    new ArraySegment<byte>(buffer),
                    WebSocketMessageType.Text,
                    true,
                    _cancellationTokenSource?.Token ?? CancellationToken.None);
            }
            catch (Exception ex)
            {
                OnConnectionStatusChanged?.Invoke($"Send failed: {ex.Message}");
                _isConnected = false;
            }
        }

        private async Task ReceiveMessagesAsync()
        {
            if (_webSocket == null || _cancellationTokenSource == null) return;

            var buffer = new byte[4096];
            try
            {
                while (_webSocket.State == WebSocketState.Open)
                {
                    var result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        _cancellationTokenSource.Token);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            string.Empty,
                            CancellationToken.None);
                        _isConnected = false;
                        OnConnectionStatusChanged?.Invoke("Disconnected");
                        break;
                    }
                    
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    try
                    {
                        var update = JsonSerializer.Deserialize<EditorUpdateMessage>(json, _jsonOptions);
                        if (update != null)
                        {
                            OnContentUpdated?.Invoke(update.Content);
                        }
                    }
                    catch (JsonException) { }
                }
            }
            catch (Exception ex)
            {
                _isConnected = false;
                OnConnectionStatusChanged?.Invoke($"Connection error: {ex.Message}");
            }
        }

        public async Task DisconnectAsync()
        {
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                _cancellationTokenSource?.Cancel();
                
                await _webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Client disconnecting",
                    CancellationToken.None);
                
                _isConnected = false;
                OnConnectionStatusChanged?.Invoke("Disconnected");
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _webSocket?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
        
        public bool IsConnected => _isConnected;
        public string CurrentEditorId => _currentEditorId;
        public string CurrentUserId => _currentUserId;
    }
} 