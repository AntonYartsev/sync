namespace Sync.Mono.Models;

public class EditorState
{
    public string Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Language { get; set; } = "plaintext";
    public HashSet<string> ConnectedUsers { get; set; } = new();
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    public EditorState(Guid? guid = null)
    {
        Id = guid?.ToString() ?? Guid.NewGuid().ToString();
    }
}

public class EditorUpdate
{
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class EditorUpdateMessage
{
    public string Type { get; set; } = "contentUpdate";
    public string Content { get; set; } = string.Empty;
    public string? Language { get; set; }
    public HashSet<string>? ConnectedUsers { get; set; }
}