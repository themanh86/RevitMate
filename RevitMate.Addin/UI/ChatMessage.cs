using System;

namespace RevitMate.Addin.UI
{
    /// <summary>
    /// Source of a chat entry in the conversation panel.
    /// </summary>
    public enum Role
    {
        User,
        Assistant,
        System,
        Action
    }

    /// <summary>
    /// A single message rendered in the chat history.
    /// Immutable once constructed.
    /// </summary>
    public class ChatMessage
    {
        public ChatMessage(Role role, string text)
            : this(role, text, DateTime.Now)
        {
        }

        public ChatMessage(Role role, string text, DateTime timestamp)
        {
            Role = role;
            Text = text ?? string.Empty;
            Timestamp = timestamp;
        }

        public Role Role { get; }
        public string Text { get; }
        public DateTime Timestamp { get; }
    }
}
