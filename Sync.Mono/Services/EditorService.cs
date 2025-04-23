using System.Collections.Concurrent;
using Sync.Mono.Models;

namespace Sync.Mono.Services;

public interface IEditorService
{
    Task<EditorState> GetEditorStateAsync(string id);
    Task<EditorState> CreateEditorStateAsync(string? guid = null);
    Task<EditorState> UpdateEditorStateAsync(string id, string content);
    Task SetLanguageAsync(string editorId, string language);
    Task<string> GetLanguageAsync(string editorId);
    Task<IEnumerable<string>> GetConnectedUsersAsync(string editorId);
    Task AddConnectedUserAsync(string editorId, string userId);
    Task RemoveConnectedUserAsync(string editorId, string userId);
    EditorState GetEditorState(string editorId);
}

public class EditorService : IEditorService
{
    private readonly ILogger<EditorService> _logger;
    private readonly ConcurrentDictionary<string, EditorState> _editors = new();

    public EditorService(ILogger<EditorService> logger)
    {
        _logger = logger;
    }

    public Task<EditorState> GetEditorStateAsync(string id)
    {
        if (_editors.TryGetValue(id, out var editor))
        {
            return Task.FromResult(editor);
        }

        throw new KeyNotFoundException($"Editor with ID {id} not found");
    }

    public EditorState GetEditorState(string editorId)
    {
        if (_editors.TryGetValue(editorId, out var editor))
        {
            return editor;
        }

        throw new KeyNotFoundException($"Editor with ID {editorId} not found");
    }

    public Task<EditorState> CreateEditorStateAsync(string? guid = null)
    {

        var editor = new EditorState(Guid.Parse(guid ?? Guid.NewGuid().ToString()));
        _editors[editor.Id] = editor;
        _logger.LogInformation("Created new editor with ID: {EditorId}", editor.Id);
        return Task.FromResult(editor);
    }

    public Task<EditorState> UpdateEditorStateAsync(string id, string content)
    {
        if (_editors.TryGetValue(id, out var editor))
        {
            editor.Content = content;
            editor.LastModified = DateTime.UtcNow;
            return Task.FromResult(editor);
        }

        throw new KeyNotFoundException($"Editor with ID {id} not found");
    }

    public Task SetLanguageAsync(string editorId, string language)
    {
        if (_editors.TryGetValue(editorId, out var editor))
        {
            editor.Language = language;
            return Task.CompletedTask;
        }

        throw new KeyNotFoundException($"Editor with ID {editorId} not found");
    }

    public Task<string> GetLanguageAsync(string editorId)
    {
        if (_editors.TryGetValue(editorId, out var editor))
        {
            return Task.FromResult(editor.Language);
        }

        return Task.FromResult("plaintext");
    }

    public Task<IEnumerable<string>> GetConnectedUsersAsync(string editorId)
    {
        if (_editors.TryGetValue(editorId, out var editor))
        {
            return Task.FromResult(editor.ConnectedUsers as IEnumerable<string>);
        }

        return Task.FromResult(Enumerable.Empty<string>());
    }

    public Task AddConnectedUserAsync(string editorId, string userId)
    {
        if (_editors.TryGetValue(editorId, out var editor))
        {
            editor.ConnectedUsers.Add(userId);
            _logger.LogInformation("User {UserId} connected to editor {EditorId}", userId, editorId);
        }
        else
        {
            var newEditor = new EditorState { Id = editorId };
            newEditor.ConnectedUsers.Add(userId);
            _editors[editorId] = newEditor;
            _logger.LogInformation("Created new editor {EditorId} for user {UserId}", editorId, userId);
        }

        return Task.CompletedTask;
    }

    public Task RemoveConnectedUserAsync(string editorId, string userId)
    {
        if (_editors.TryGetValue(editorId, out var editor))
        {
            editor.ConnectedUsers.Remove(userId);
            _logger.LogInformation("User {UserId} disconnected from editor {EditorId}", userId, editorId);

            if (editor.ConnectedUsers.Count == 0)
            {
                _editors.TryRemove(editorId, out _);
                _logger.LogInformation("Removed editor {EditorId} as it has no connected users", editorId);
            }
        }

        return Task.CompletedTask;
    }
}