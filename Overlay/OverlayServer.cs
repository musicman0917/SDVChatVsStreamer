using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using SDVChatVsStreamer.Sabotage;
using SDVChatVsStreamer.UI;
using StardewModdingAPI;

namespace SDVChatVsStreamer.Overlay;

public class OverlayServer
{
    private readonly ModConfig _config;
    private readonly SabotageEngine _sabotage;
    private readonly IMonitor _monitor;
    private readonly Economy.ViewerLedger _ledger;

    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private string _overlayHtmlPath = "";
    private string _mobileHtmlPath  = "";
    private string _chatHtmlPath    = "";
    private ChatFeed? _chatFeed;
    private TikTok.TikTokManager? _tikTokManager;

    private readonly List<WebSocket> _clients     = new();
    private readonly List<WebSocket> _chatClients = new();
    private readonly object _clientLock = new();

    // Thread-safe queue for HUD messages to fire on the main thread
    private readonly Queue<string> _pendingHudMessages = new();
    private readonly object _hudLock = new();

    /// <summary>Call this from the game update tick to fire queued HUD messages on the main thread.</summary>
    public void ProcessPendingNotifications()
    {
        lock (_hudLock)
        {
            while (_pendingHudMessages.Count > 0)
            {
                var msg = _pendingHudMessages.Dequeue();
                StardewValley.Game1.addHUDMessage(
                    new StardewValley.HUDMessage(msg, StardewValley.HUDMessage.newQuest_type));
            }
        }
    }

    private void QueueHudMessage(string message)
    {
        lock (_hudLock)
            _pendingHudMessages.Enqueue(message);
    }

    public OverlayServer(ModConfig config, SabotageEngine sabotage, Economy.ViewerLedger ledger, IMonitor monitor)
    {
        _config   = config;
        _sabotage = sabotage;
        _ledger   = ledger;
        _monitor  = monitor;
    }

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    public void SetTikTokManager(TikTok.TikTokManager manager) => _tikTokManager = manager;

    public void Start(string overlayHtmlPath, string mobileHtmlPath, string chatHtmlPath, UI.ChatFeed chatFeed)
    {
        _overlayHtmlPath = overlayHtmlPath;
        _mobileHtmlPath  = mobileHtmlPath;
        _chatHtmlPath    = chatHtmlPath;
        _chatFeed        = chatFeed;
        _cts             = new CancellationTokenSource();
        _listener        = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{_config.OverlayPort}/");

        try
        {
            _listener.Start();
            _monitor.Log($"[OverlayServer] Listening on http://localhost:{_config.OverlayPort}/", LogLevel.Info);
            Task.Run(() => AcceptLoop(_cts.Token));
        }
        catch (Exception ex)
        {
            _monitor.Log($"[OverlayServer] Failed to start: {ex.Message}", LogLevel.Error);
        }
    }

    public void Stop()
    {
        try
        {
            _cts?.Cancel();
            _listener?.Stop();
            _listener?.Close();
        }
        catch { /* ignore on shutdown */ }
        _monitor.Log("[OverlayServer] Stopped.", LogLevel.Info);
    }

    // ─── Accept Loop ──────────────────────────────────────────────────────────

    private async Task AcceptLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var ctx = await _listener!.GetContextAsync().ConfigureAwait(false);

                if (ctx.Request.IsWebSocketRequest)
                    _ = Task.Run(() => HandleWebSocketAsync(ctx, ct), ct);
                else
                    HandleHttp(ctx);
            }
            catch (ObjectDisposedException) { break; }
            catch (HttpListenerException) { break; }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _monitor.Log($"[OverlayServer] Accept error: {ex.Message}", LogLevel.Warn);
            }
        }
    }

    // ─── HTTP ─────────────────────────────────────────────────────────────────

    private void HandleHttp(HttpListenerContext ctx)
    {
        try
        {
            var path = ctx.Request.Url?.AbsolutePath ?? "/";
            var filePath = path.TrimEnd('/') switch
            {
                "/mobile"      => _mobileHtmlPath,
                "/chat"        => _chatHtmlPath,
                "/tiktok-test" => _chatHtmlPath, // serves same page, WS handles the logic
                _              => _overlayHtmlPath
            };

            if (!File.Exists(filePath))
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.Close();
                return;
            }

            var html = File.ReadAllBytes(filePath);
            ctx.Response.ContentType     = "text/html; charset=utf-8";
            ctx.Response.ContentLength64 = html.Length;
            ctx.Response.OutputStream.Write(html, 0, html.Length);
            ctx.Response.OutputStream.Close();
        }
        catch (Exception ex)
        {
            _monitor.Log($"[OverlayServer] HTTP error: {ex.Message}", LogLevel.Warn);
        }
    }

    // ─── WebSocket ────────────────────────────────────────────────────────────

    private async Task HandleWebSocketAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        WebSocket? ws = null;
        bool isChatClient   = ctx.Request.Url?.AbsolutePath.TrimEnd('/') == "/chat";
        bool isTikTokTest   = ctx.Request.Url?.AbsolutePath.TrimEnd('/') == "/tiktok-test";
        try
        {
            var wsCtx = await ctx.AcceptWebSocketAsync(subProtocol: null).ConfigureAwait(false);
            ws = wsCtx.WebSocket;

            if (isTikTokTest)
            {
                _monitor.Log("[OverlayServer] TikTok test client connected.", LogLevel.Debug);
                var tbuf = new byte[65536];
                var tsb  = new System.Text.StringBuilder();
                while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
                {
                    tsb.Clear();
                    WebSocketReceiveResult tres;
                    do
                    {
                        tres = await ws.ReceiveAsync(new ArraySegment<byte>(tbuf), ct).ConfigureAwait(false);
                        if (tres.MessageType == WebSocketMessageType.Close) goto tikTokDone;
                        tsb.Append(Encoding.UTF8.GetString(tbuf, 0, tres.Count));
                    } while (!tres.EndOfMessage);
                    var tjson = tsb.ToString();
                    _monitor.Log($"[OverlayServer] TikTok test event: {tjson}", LogLevel.Debug);
                    _tikTokManager?.InjectTestEvent(tjson);
                }
                tikTokDone:
                return;
            }

            if (isChatClient)
            {
                lock (_clientLock) _chatClients.Add(ws);
                _monitor.Log("[OverlayServer] Chat client connected.", LogLevel.Debug);

                // Send recent messages on connect
                if (_chatFeed != null)
                {
                    foreach (var msg in _chatFeed.GetRecent(50))
                        await SendToClientAsync(ws, BuildChatMessage(msg), ct).ConfigureAwait(false);
                }

                var buf2 = new byte[512];
                while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
                {
                    var r = await ws.ReceiveAsync(new ArraySegment<byte>(buf2), ct).ConfigureAwait(false);
                    if (r.MessageType == WebSocketMessageType.Close) break;
                }
                return;
            }

            lock (_clientLock) _clients.Add(ws);
            _monitor.Log("[OverlayServer] Overlay client connected.", LogLevel.Debug);

            if (_config.NotifyOverlayConnected)
                QueueHudMessage("🖥️ Chaos overlay connected!");

            // Send initial state on connect
            await SendToClientAsync(ws, BuildFullState(), ct).ConfigureAwait(false);

            // Keep alive — handle incoming messages
            var buffer = new byte[512];
            while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, ct).ConfigureAwait(false);
                    break;
                }
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await HandleClientMessageAsync(ws, json, ct).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (WebSocketException) { }
        catch (Exception ex)
        {
            _monitor.Log($"[OverlayServer] WebSocket error: {ex.Message}", LogLevel.Debug);
        }
        finally
        {
            if (ws != null)
            {
                if (isChatClient)
                    lock (_clientLock) _chatClients.Remove(ws);
                else
                    lock (_clientLock) _clients.Remove(ws);
                ws.Dispose();
            }
        }
    }

    private async Task HandleClientMessageAsync(WebSocket ws, string json, CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var type = root.TryGetProperty("type", out var t) ? t.GetString() : "";

            if (type == "balance_request")
            {
                var username = root.TryGetProperty("username", out var u) ? u.GetString() ?? "" : "";
                var viewer   = _ledger.GetViewer(username.ToLower());
                var payload  = JsonSerializer.Serialize(new
                {
                    type     = "balance_result",
                    username,
                    points   = viewer?.Points ?? 0,
                    found    = viewer != null
                });
                await SendToClientAsync(ws, payload, ct).ConfigureAwait(false);
            }
            else if (type == "debug_raid")
            {
                var username    = root.TryGetProperty("username",    out var u) ? u.GetString() ?? "debugraid" : "debugraid";
                var viewerCount = root.TryGetProperty("viewerCount", out var vc) ? vc.GetInt32() : 1;

                _monitor.Log($"[OverlayServer] Debug raid: {username} with {viewerCount} viewers", LogLevel.Info);

                ModEntry.PendingActions.Enqueue(() =>
                    _sabotage.TriggerRaidEvent(username, viewerCount, msg => PushChatMessage(
                        new UI.ChatMessage("Chat vs Streamer", msg, UI.ChatFeed.HtmlEscape(msg),
                            UI.ChatPlatform.Twitch, DateTime.UtcNow))));
            }
        }
        catch (Exception ex)
        {
            _monitor.Log($"[OverlayServer] Client message error: {ex.Message}", LogLevel.Debug);
        }
    }

    // ─── Push Methods ─────────────────────────────────────────────────────────

    public void PushChatRemoveUser(string username)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new { type = "remove_user", username });
        List<WebSocket> snapshot;
        lock (_clientLock) snapshot = new List<WebSocket>(_chatClients);
        foreach (var ws in snapshot)
            _ = SendToClientAsync(ws, json, _cts?.Token ?? CancellationToken.None);
    }

    public void PushChatClear()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new { type = "clear_chat" });
        List<WebSocket> snapshot;
        lock (_clientLock) snapshot = new List<WebSocket>(_chatClients);
        foreach (var ws in snapshot)
            _ = SendToClientAsync(ws, json, _cts?.Token ?? CancellationToken.None);
    }

    public void PushChatRemoveMessage(string messageId)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new { type = "remove_msg", id = messageId });
        List<WebSocket> snapshot;
        lock (_clientLock) snapshot = new List<WebSocket>(_chatClients);
        foreach (var ws in snapshot)
            _ = SendToClientAsync(ws, json, _cts?.Token ?? CancellationToken.None);
    }

    public void PushChatMessage(UI.ChatMessage msg)
    {
        if (!_config.EnableChatBrowserSource) return;
        var json = BuildChatMessage(msg);
        List<WebSocket> snapshot;
        lock (_clientLock) snapshot = new List<WebSocket>(_chatClients);
        foreach (var ws in snapshot)
            _ = SendToClientAsync(ws, json, _cts?.Token ?? CancellationToken.None);
    }

    private static string BuildChatMessage(UI.ChatMessage msg)
    {
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            type         = "chat",
            id           = msg.MessageId,
            username     = msg.Username,
            text         = msg.Text,
            renderedText = msg.RenderedText,
            platform     = msg.Platform.ToString().ToLower(),
            ts           = msg.Timestamp.ToString("HH:mm")
        });
    }

    public void PushFeedEvent(string username, string sabotageName, string description, int cost, string eventType = "buy")
    {
        var payload = JsonSerializer.Serialize(new
        {
            type = "feed",
            username,
            sabotageName,
            description,
            cost,
            eventType,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });
        Broadcast(payload);
    }

    public void PushShopUpdate()
    {
        Broadcast(BuildShopState());
    }

    public void PushLeaderboard(List<(string Username, int Points)> top)
    {
        var payload = JsonSerializer.Serialize(new
        {
            type    = "leaderboard",
            entries = top.Select(e => new { e.Username, e.Points }).ToList()
        });
        Broadcast(payload);
    }

    // ─── State Builders ───────────────────────────────────────────────────────

    private string BuildFullState()
    {
        var shopItems = _sabotage.GetShopList().Select(d => new
        {
            name              = d.Name,
            buyCommand        = d.BuyCommand,
            description       = d.Description,
            cost              = d.Cost,
            cooldownTotal     = d.Sabotage.CooldownSeconds,
            onCooldown        = d.IsOnCooldown,
            cooldownRemaining = d.CooldownRemaining
        });

        return JsonSerializer.Serialize(new
        {
            type   = "init",
            shop   = shopItems,
            config = BuildConfigPayload()
        });
    }

    private string BuildShopState()
    {
        var shopItems = _sabotage.GetShopList().Select(d => new
        {
            buyCommand        = d.BuyCommand,
            cooldownTotal     = d.Sabotage.CooldownSeconds,
            onCooldown        = d.IsOnCooldown,
            cooldownRemaining = d.CooldownRemaining
        });

        return JsonSerializer.Serialize(new { type = "shop_update", shop = shopItems });
    }

    public void PushMetaEffects(bool doublePoints, bool halveCosts, int doublePointsSecondsLeft, int halveCostsSecondsLeft)
    {
        var payload = JsonSerializer.Serialize(new
        {
            type = "meta",
            doublePoints,
            halveCosts,
            doublePointsSecondsLeft,
            halveCostsSecondsLeft
        });
        Broadcast(payload);
    }

    public void PushConfig()
    {
        var payload = JsonSerializer.Serialize(new
        {
            type   = "config",
            config = BuildConfigPayload()
        });
        Broadcast(payload);
    }

    private object BuildConfigPayload() => new
    {
        mode              = _config.OverlayMode,
        panelOrder        = _config.OverlayPanelOrder,
        showShop          = _config.OverlayShowShop,
        showFeed          = _config.OverlayShowFeed,
        showLeaderboard   = _config.OverlayShowLeaderboard,
        showMetaEffects   = _config.OverlayShowMetaEffects,
        maxShopItems      = _config.OverlayMaxShopItems,
        maxFeedItems      = _config.OverlayMaxFeedItems,
        maxLeaderboardItems = _config.OverlayMaxLeaderboardItems,
        width             = _config.OverlayWidth,
        fontSize          = _config.OverlayFontSize,
        theme             = _config.OverlayTheme,
        tickerPosition    = _config.OverlayTickerPosition,
        tickerSpeed       = _config.OverlayTickerSpeed,
        customBg          = _config.OverlayCustomBg,
        customAccent      = _config.OverlayCustomAccent,
        customText        = _config.OverlayCustomText,
    };

    // ─── Broadcast ────────────────────────────────────────────────────────────

    private void Broadcast(string json)
    {
        List<WebSocket> snapshot;
        lock (_clientLock) snapshot = new List<WebSocket>(_clients);

        foreach (var client in snapshot)
        {
            var capturedClient = client;
            _ = Task.Run(async () =>
            {
                try
                {
                    await SendToClientAsync(capturedClient, json, CancellationToken.None).ConfigureAwait(false);
                }
                catch { /* client disconnected */ }
            });
        }
    }

    private static async Task SendToClientAsync(WebSocket ws, string json, CancellationToken ct)
    {
        if (ws.State != WebSocketState.Open) return;
        var bytes = Encoding.UTF8.GetBytes(json);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
    }
}