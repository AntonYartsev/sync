namespace Sync.Backend.Models;

public class EditorState
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Content { get; set; } = string.Empty;
    public List<string> ConnectedUsers { get; set; } = new();
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
} 