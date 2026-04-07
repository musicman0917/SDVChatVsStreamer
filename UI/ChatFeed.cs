using System.Collections.Concurrent;

namespace SDVChatVsStreamer.UI;

public enum ChatPlatform { Twitch, TikTok }

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
    private readonly ConcurrentQueue<ChatMessage> _messages = new();

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
        _messages.Enqueue(msg);

        while (_messages.Count > _maxMessages)
            _messages.TryDequeue(out _);

        OnNewMessage?.Invoke(msg);
    }

    public static string HtmlEscape(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    public void RemoveByUser(string username)
    {
        var remaining = _messages.Where(m =>
            !m.Username.Equals(username, StringComparison.OrdinalIgnoreCase)).ToList();
        while (_messages.TryDequeue(out _)) { }
        foreach (var m in remaining) _messages.Enqueue(m);
        OnRemoveUser?.Invoke(username);
    }

    public void RemoveById(string messageId)
    {
        var remaining = _messages.Where(m => m.MessageId != messageId).ToList();
        while (_messages.TryDequeue(out _)) { }
        foreach (var m in remaining) _messages.Enqueue(m);
        OnRemoveMessage?.Invoke(messageId);
    }

    public void Clear()
    {
        while (_messages.TryDequeue(out _)) { }
        OnClear?.Invoke();
    }

    public IReadOnlyList<ChatMessage> GetRecent(int count)
    {
        return _messages.Reverse().Take(count).Reverse().ToList();
    }
}