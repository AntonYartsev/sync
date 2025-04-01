using System.Collections.Concurrent;
using Sync.Backend.Models;

namespace Sync.Backend.Services;

public interface IEditorService
{
    Task<EditorState> GetEditorStateAsync(string id);
    Task<EditorState> CreateEditorStateAsync();
    Task<EditorState> UpdateEditorStateAsync(string id, string content);
    Task AddUserToEditorAsync(string id, string userId);
    Task RemoveUserFromEditorAsync(string id, string userId);
    Task<string> GetContentAsync(string editorId);
    Task SetContentAsync(string editorId, string content);
    Task<string> GetLanguageAsync(string editorId);
    Task SetLanguageAsync(string editorId, string language);
    Task<IEnumerable<string>> GetConnectedUsersAsync(string editorId);
    Task AddConnectedUserAsync(string editorId, string userId);
    Task RemoveConnectedUserAsync(string editorId, string userId);
    void UpdateContent(string editorId, string content);
    string GetContent(string editorId);
    void AddUser(string editorId, string userId);
    void RemoveUser(string editorId, string userId);
    HashSet<string> GetConnectedUsers(string editorId);
}

public class EditorService : IEditorService
{
    private readonly ConcurrentDictionary<string, EditorState> _editors = new();
    private readonly ConcurrentDictionary<string, string> _editorContents = new();
    private readonly ConcurrentDictionary<string, string> _languages = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _connectedUsers = new();

    public Task<EditorState> GetEditorStateAsync(string id)
    {
        if (_editors.TryGetValue(id, out var editor))
        {
            return Task.FromResult(editor);
        }
        throw new KeyNotFoundException($"Editor with ID {id} not found");
    }

    public Task<EditorState> CreateEditorStateAsync()
    {
        var editor = new EditorState();
        _editors.TryAdd(editor.Id, editor);
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

    public Task AddUserToEditorAsync(string id, string userId)
    {
        if (_editors.TryGetValue(id, out var editor))
        {
            if (!editor.ConnectedUsers.Contains(userId))
            {
                editor.ConnectedUsers.Add(userId);
            }
        }
        else
        {
            throw new KeyNotFoundException($"Editor with ID {id} not found");
        }
        return Task.CompletedTask;
    }

    public Task RemoveUserFromEditorAsync(string id, string userId)
    {
        if (_editors.TryGetValue(id, out var editor))
        {
            editor.ConnectedUsers.Remove(userId);
        }
        return Task.CompletedTask;
    }

    public Task<string> GetContentAsync(string editorId)
    {
        return Task.FromResult(_editorContents.GetValueOrDefault(editorId, string.Empty));
    }

    public Task SetContentAsync(string editorId, string content)
    {
        _editorContents.AddOrUpdate(editorId, content, (_, _) => content);
        return Task.CompletedTask;
    }

    public Task<string> GetLanguageAsync(string editorId)
    {
        return Task.FromResult(_languages.GetValueOrDefault(editorId, "plaintext"));
    }

    public Task SetLanguageAsync(string editorId, string language)
    {
        _languages.AddOrUpdate(editorId, language, (_, _) => language);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetConnectedUsersAsync(string editorId)
    {
        var users = _connectedUsers.GetValueOrDefault(editorId, new HashSet<string>());
        return Task.FromResult(users as IEnumerable<string>);
    }

    public Task AddConnectedUserAsync(string editorId, string userId)
    {
        _connectedUsers.AddOrUpdate(
            editorId,
            new HashSet<string> { userId },
            (_, users) =>
            {
                users.Add(userId);
                return users;
            });
        return Task.CompletedTask;
    }

    public Task RemoveConnectedUserAsync(string editorId, string userId)
    {
        if (_connectedUsers.TryGetValue(editorId, out var users))
        {
            users.Remove(userId);
            if (users.Count == 0)
            {
                _connectedUsers.TryRemove(editorId, out _);
                _editorContents.TryRemove(editorId, out _);
            }
        }
        return Task.CompletedTask;
    }

    public void UpdateContent(string editorId, string content)
    {
        _editorContents.AddOrUpdate(editorId, content, (_, __) => content);
    }

    public string GetContent(string editorId)
    {
        return _editorContents.GetValueOrDefault(editorId, string.Empty);
    }

    public void AddUser(string editorId, string userId)
    {
        _connectedUsers.AddOrUpdate(
            editorId,
            new HashSet<string> { userId },
            (_, users) =>
            {
                users.Add(userId);
                return users;
            });
    }

    public void RemoveUser(string editorId, string userId)
    {
        if (_connectedUsers.TryGetValue(editorId, out var users))
        {
            users.Remove(userId);
            if (users.Count == 0)
            {
                _connectedUsers.TryRemove(editorId, out _);
                _editorContents.TryRemove(editorId, out _);
            }
        }
    }

    public HashSet<string> GetConnectedUsers(string editorId)
    {
        return _connectedUsers.GetValueOrDefault(editorId, new HashSet<string>());
    }
} 