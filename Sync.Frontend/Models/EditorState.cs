using System;
using System.Collections.Generic;

namespace Sync.Frontend.Models
{
    public class EditorState
    {
        public string Id { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public List<string> ConnectedUsers { get; set; } = new();
        public DateTime LastModified { get; set; }
    }

    public class EditorUpdate
    {
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
} 