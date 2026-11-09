namespace SDVChatVsStreamer.UI;

public enum ChatPlatform { Twitch, TikTok, YouTube }

public record ChatMessage(
    string Username,
    string Text,
    string RenderedText,   // HTML with emote <img> tags for overlay
    ChatPlatform Platform,
    DateTime Timestamp,
    string MessageId = ""
);

public class ChatFeed
{
    private readonly int _maxMessages;
    private readonly object _lock = new();
    private readonly List<ChatMessage> _messages = new();

    public event Action<ChatMessage>? OnNewMessage;
    public event Action<string>? OnRemoveUser;
    public event Action<string>? OnRemoveMessage;
    public event Action? OnClear;

    public ChatFeed(int maxMessages = 100)
    {
        _maxMessages = maxMessages;
    }

    public void Add(string username, string text, ChatPlatform platform, string messageId = "", string? renderedText = null)
    {
        var msg = new ChatMessage(username, text, renderedText ?? HtmlEscape(text), platform, DateTime.UtcNow, messageId);
        lock (_lock)
        {
            _messages.Add(msg);
            while (_messages.Count > _maxMessages)
                _messages.RemoveAt(0);
        }

        OnNewMessage?.Invoke(msg);
    }

    public static string HtmlEscape(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    public void RemoveByUser(string username)
    {
        lock (_lock)
        {
            _messages.RemoveAll(m => m.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        }
        OnRemoveUser?.Invoke(username);
    }

    public void RemoveById(string messageId)
    {
        lock (_lock)
        {
            _messages.RemoveAll(m => m.MessageId == messageId);
        }
        OnRemoveMessage?.Invoke(messageId);
    }

    public void Clear()
    {
        lock (_lock)
        {
            _messages.Clear();
        }
        OnClear?.Invoke();
    }

    public IReadOnlyList<ChatMessage> GetRecent(int count)
    {
        lock (_lock)
        {
            return _messages.Skip(Math.Max(0, _messages.Count - count)).ToList();
        }
    }
}
