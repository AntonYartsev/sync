namespace Sync.Frontend.Models
{
    public class EditorUpdateMessage
    {
        public string Type { get; set; } = "contentUpdate";
        public string Content { get; set; } = string.Empty;
    }
} 