using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Sync.Frontend.Models;

namespace Sync.Frontend.Services
{
    public interface IEditorService
    {
        Task<EditorState?> GetEditorAsync(string id);
        Task<EditorState?> CreateEditorAsync();
        Task<EditorState?> UpdateEditorAsync(string id, string content);
    }

    public class EditorService : IEditorService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public EditorService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            var backendUrl = Environment.GetEnvironmentVariable("BACKEND_URL") ?? "http://localhost:5001";
            _httpClient.BaseAddress = new Uri(backendUrl);
            Console.WriteLine($"Using backend URL: {backendUrl}"); // Debug log
        }

        public async Task<EditorState?> GetEditorAsync(string id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/editor/{id}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<EditorState>(_jsonOptions);
                }
                Console.WriteLine($"Failed to get editor: {response.StatusCode}"); // Debug log
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting editor: {ex.Message}"); // Debug log
                return null;
            }
        }

        public async Task<EditorState?> CreateEditorAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync("api/editor", null);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<EditorState>(_jsonOptions);
                }
                Console.WriteLine($"Failed to create editor: {response.StatusCode}"); // Debug log
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating editor: {ex.Message}"); // Debug log
                return null;
            }
        }

        public async Task<EditorState?> UpdateEditorAsync(string id, string content)
        {
            try
            {
                var response = await _httpClient.PutAsync($"api/editor/{id}", new StringContent(content));
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<EditorState>(_jsonOptions);
                }
                Console.WriteLine($"Failed to update editor: {response.StatusCode}"); // Debug log
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating editor: {ex.Message}"); // Debug log
                return null;
            }
        }
    }
} 