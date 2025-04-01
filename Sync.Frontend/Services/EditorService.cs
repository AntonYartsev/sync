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
        }

        public async Task<EditorState?> GetEditorAsync(string id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<EditorState>($"api/editor/{id}", _jsonOptions);
            }
            catch (Exception)
            {
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
                return null;
            }
            catch (Exception)
            {
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
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
} 