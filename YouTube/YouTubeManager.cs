using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using SDVChatVsStreamer.Economy;
using SDVChatVsStreamer.Sabotage;
using StardewModdingAPI;
using StardewValley;

namespace SDVChatVsStreamer.YouTube;

public class YouTubeManager
{
    private readonly ModConfig      _config;
    private readonly PointsEngine   _points;
    private readonly SabotageEngine _sabotage;
    private readonly ViewerLedger   _ledger;
    private readonly IMonitor       _monitor;
    private UI.ChatFeed?            _chatFeed;
    private Overlay.OverlayServer?  _overlay;

    private ClientWebSocket?        _ws;
    private CancellationTokenSource? _cts;
    private bool _gameActive = false;

    public YouTubeManager(
        ModConfig config,
        PointsEngine points,
        SabotageEngine sabotage,
        ViewerLedger ledger,
        IMonitor monitor,
        UI.ChatFeed? chatFeed = null,
        Overlay.OverlayServer? overlay = null)
    {
        _config   = config;
        _points   = points;
        _sabotage = sabotage;
        _ledger   = ledger;
        _monitor  = monitor;
        _chatFeed = chatFeed;
        _overlay  = overlay;
    }

    public void SetGameActive(bool active) => _gameActive = active;

    public void Connect()
    {
        if (!_config.YouTubeEnabled)
        {
            _monitor.Log("[YouTubeManager] YouTube integration disabled in config.", LogLevel.Info);
            return;
        }

        _cts = new CancellationTokenSource();
        _ = Task.Run(() => ConnectLoop(_cts.Token));
        _monitor.Log("[YouTubeManager] Connecting to Streamer.bot WebSocket...", LogLevel.Info);
    }

    public void Disconnect()
    {
        _cts?.Cancel();
        _ws?.Abort();
        _ws = null;
        _monitor.Log("[YouTubeManager] Disconnected.", LogLevel.Info);
    }

    private async Task ConnectLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                _ws = new ClientWebSocket();
                var uri = new Uri($"ws://localhost:{_config.StreamerbotPort}/");
                await _ws.ConnectAsync(uri, ct);
                _monitor.Log("[YouTubeManager] Connected to Streamer.bot.", LogLevel.Info);

                // Subscribe to events
                var sub = JsonSerializer.Serialize(new
                {
                    request = "Subscribe",
                    id      = "yt-sub",
                    events  = new { YouTube = new[] { "Message", "MembershipItem", "SuperChat" } }
                });
                var subBytes = Encoding.UTF8.GetBytes(sub);
                await _ws.SendAsync(subBytes, WebSocketMessageType.Text, true, ct);

                await ReceiveLoop(_ws, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _monitor.Log($"[YouTubeManager] WebSocket error: {ex.Message} — retrying in 5s", LogLevel.Warn);
                await Task.Delay(5000, ct);
            }
        }
    }

    private async Task ReceiveLoop(ClientWebSocket ws, CancellationToken ct)
    {
        var buffer = new byte[8192];
        while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
        {
            var result = await ws.ReceiveAsync(buffer, ct);
            if (result.MessageType == WebSocketMessageType.Close) break;

            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            try { HandleMessage(json); }
            catch (Exception ex)
            {
                _monitor.Log($"[YouTubeManager] Parse error: {ex.Message}", LogLevel.Debug);
            }
        }
    }

    private void HandleMessage(string json)
    {
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Only handle YouTube chat message events
        if (!root.TryGetProperty("event", out var evt)) return;
        if (!evt.TryGetProperty("source", out var src)) return;
        if (src.GetString() != "YouTube") return;
        if (!evt.TryGetProperty("type", out var type)) return;

        var eventType = type.GetString();
        if (!root.TryGetProperty("data", out var data)) return;

        var username = data.TryGetProperty("user", out var u) ? u.GetString() ?? "viewer" : "viewer";
        var message  = data.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "";
        var isMember = data.TryGetProperty("isSubscribed", out var s) && s.GetBoolean();

        switch (eventType)
        {
            case "Message":
                HandleChatMessage(username, message, isMember);
                break;
            case "MembershipItem":
                HandleMembership(username);
                break;
            case "SuperChat":
                var amount = data.TryGetProperty("amount", out var a) ? a.GetDouble() : 0;
                HandleSuperChat(username, amount);
                break;
        }
    }

    private void HandleChatMessage(string username, string message, bool isMember)
    {
        _monitor.Log($"[YouTubeManager] Chat: {username}: {message}", LogLevel.Trace);

        // Award chat points
        ModEntry.PendingActions.Enqueue(() =>
        {
            _points.OnChat(username);

            // Push to chat feed
            _chatFeed?.Add(username, message, UI.ChatPlatform.YouTube,
                renderedText: UI.ChatFeed.HtmlEscape(message));
        });

        message = message.Trim();

        if (message.Equals("!balance", StringComparison.OrdinalIgnoreCase))
        {
            ModEntry.PendingActions.Enqueue(() =>
            {
                var bal = _ledger.GetPoints(username);
                _monitor.Log($"[YouTubeManager] {username} balance: {bal}pts", LogLevel.Info);
            });
            return;
        }

        if (message.Equals("!shop", StringComparison.OrdinalIgnoreCase))
        {
            ModEntry.PendingActions.Enqueue(() =>
                _monitor.Log($"[YouTubeManager] {username} requested shop", LogLevel.Info));
            return;
        }

        if (message.StartsWith("!info ", StringComparison.OrdinalIgnoreCase))
        {
            var cmd = message.Substring(6).Trim().ToLower();
            ModEntry.PendingActions.Enqueue(() =>
            {
                var def = _sabotage.GetDefinition(cmd);
                _monitor.Log(def == null
                    ? $"[YouTubeManager] {username} info: {cmd} not found"
                    : $"[YouTubeManager] {username} info: {def.Name} — {def.Description} | {def.Cost}pts",
                    LogLevel.Info);
            });
            return;
        }

        if (message.StartsWith("!buy ", StringComparison.OrdinalIgnoreCase))
        {
            if (!_gameActive) return;
            var parts   = message.Substring(5).Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var command = parts[0].ToLower();
            var args    = parts.Length > 1 ? parts[1].Trim() : "";
            HandleBuy(username, command, args);
        }
    }

    private void HandleBuy(string username, string command, string args)
    {
        ModEntry.PendingActions.Enqueue(() =>
        {
            var result = _sabotage.TryBuy(username, command, args);
            switch (result.Status)
            {
                case BuyStatus.Success:
                    _monitor.Log($"[YouTubeManager] {username} bought {command}", LogLevel.Info);
                    break;
                case BuyStatus.InsufficientFunds:
                    _monitor.Log($"[YouTubeManager] {username} insufficient funds for {command}", LogLevel.Debug);
                    break;
                case BuyStatus.OnCooldown:
                    _monitor.Log($"[YouTubeManager] {command} on cooldown", LogLevel.Debug);
                    break;
                case BuyStatus.Rejected:
                    _monitor.Log($"[YouTubeManager] {command} rejected: {result.Description}", LogLevel.Debug);
                    break;
                case BuyStatus.NotFound:
                    _monitor.Log($"[YouTubeManager] {command} not found", LogLevel.Debug);
                    break;
            }
        });
    }

    private void HandleMembership(string username)
    {
        _monitor.Log($"[YouTubeManager] New member: {username}", LogLevel.Info);
        ModEntry.PendingActions.Enqueue(() =>
        {
            _points.OnSub(username, SubTier.T1);
            Game1.addHUDMessage(new HUDMessage(
                $"🎉 {username} became a YouTube member! +{_config.SubBonus}pts",
                HUDMessage.newQuest_type));
        });
    }

    private void HandleSuperChat(string username, double amount)
    {
        _monitor.Log($"[YouTubeManager] SuperChat from {username}: ${amount}", LogLevel.Info);
        ModEntry.PendingActions.Enqueue(() =>
        {
            // Convert dollars to bits equivalent then to points
            int bits = (int)(amount * 100);
            int pts  = Math.Max(1, bits / Math.Max(1, _config.BitsPerPoint));
            _ledger.AddPoints(username, pts);
            Game1.addHUDMessage(new HUDMessage(
                $"💛 {username} sent a Super Chat! +{pts}pts",
                HUDMessage.newQuest_type));
        });
    }
}