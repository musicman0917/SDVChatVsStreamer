using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using SDVChatVsStreamer.Economy;
using SDVChatVsStreamer.Sabotage;
using StardewModdingAPI;
using StardewValley;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;

namespace SDVChatVsStreamer.Twitch;

public class TwitchManager
{
    private readonly ModConfig _config;
    private readonly PointsEngine _points;
    private readonly SabotageEngine _sabotage;
    private readonly ViewerLedger _ledger;
    private readonly IMonitor _monitor;

    private TwitchClient _client = null!;
    private string _currentToken = "";
    private ClientWebSocket? _pubSocket;
    private CancellationTokenSource? _pubCts;

    private bool _gameActive = false;

    private readonly Dictionary<string, DateTime> _activeViewers = new();
    private readonly TimeSpan _viewerTimeout = TimeSpan.FromMinutes(30);
    private UI.ChatFeed? _chatFeed;
    private Overlay.OverlayServer? _overlay;

    public TwitchManager(
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

    // ─── Connection ───────────────────────────────────────────────────────────

    public void Connect()
    {
        var token = TwitchAuth.LoadToken();
        if (token == null)
        {
            _monitor.Log("[TwitchManager] No token found — starting auth flow.", LogLevel.Warn);
            TwitchAuth.StartAuth(
                onSuccess: t => ConnectWithToken(t),
                onError:   e => _monitor.Log($"[TwitchManager] Auth failed: {e}", LogLevel.Error)
            );
            return;
        }

        // Validate before connecting — an expired token will otherwise let
        // IRC's socket "reconnect" successfully while auth silently fails
        Task.Run(async () =>
        {
            bool valid = await TwitchAuth.IsTokenValidAsync(token);
            if (!valid)
            {
                _monitor.Log("[TwitchManager] Stored token is expired — refreshing before connecting...", LogLevel.Warn);
                var refreshed = await TwitchAuth.RefreshAccessTokenAsync();
                if (refreshed != null)
                {
                    token = refreshed;
                    _monitor.Log("[TwitchManager] Token refreshed successfully before initial connect.", LogLevel.Info);
                }
                else
                {
                    _monitor.Log("[TwitchManager] Refresh failed — re-authentication may be required.", LogLevel.Error);
                }
            }
            ConnectWithToken(token);
        });
    }

    private void ConnectWithToken(string token)
    {
        try
        {
            SetupIrcClient(token);
            SetupPubSub(token);
            StartTokenRefreshTimer();
        }
        catch (Exception ex)
        {
            _monitor.Log($"[TwitchManager] Connection error: {ex.Message}", LogLevel.Error);
        }
    }

    public void Disconnect()
    {
        try
        {
            if (_client?.IsConnected == true) _client.Disconnect();
            _pubCts?.Cancel();
            _pubSocket?.Dispose();
            _refreshCts?.Cancel();
            _monitor.Log("[TwitchManager] Disconnected.", LogLevel.Info);
        }
        catch (Exception ex)
        {
            _monitor.Log($"[TwitchManager] Disconnect error: {ex.Message}", LogLevel.Error);
        }
    }

    public void SetGameActive(bool active)
    {
        _gameActive = active;
        _monitor.Log($"[TwitchManager] Game active: {active}", LogLevel.Debug);
    }

    // ─── IRC Client ───────────────────────────────────────────────────────────

    private void SetupIrcClient(string token)
    {
        _currentToken = token;
        var credentials = new ConnectionCredentials(_config.BotUsername, $"oauth:{token}");
        _client = new TwitchClient();
        _client.Initialize(credentials, _config.ChannelName);

        _client.OnConnected             += OnIrcConnected;
        _client.OnDisconnected          += OnIrcDisconnected;
        _client.OnConnectionError       += OnIrcConnectionError;
        _client.OnMessageReceived       += OnMessageReceived;
        _client.OnNewSubscriber         += OnNewSubscriber;
        _client.OnReSubscriber          += OnReSubscriber;
        _client.OnGiftedSubscription    += OnGiftedSubscription;
        _client.OnCommunitySubscription += OnCommunitySubscription;
        _client.OnRaidNotification      += OnRaidNotification;
        _client.OnUserJoined            += OnUserJoined;
        _client.OnUserBanned            += OnUserBanned;
        _client.OnUserTimedout          += OnUserTimedout;
        _client.OnMessageCleared        += OnMessageCleared;
        _client.OnChatCleared           += OnChatCleared;
        _client.OnJoinedChannel         += OnIrcJoinedChannel;

        _client.Connect();
    }

    private void OnIrcConnected(object? sender, OnConnectedArgs e)
    {
        _monitor.Log($"[TwitchManager] IRC connected as {_config.BotUsername}.", LogLevel.Info);
        Game1.addHUDMessage(new HUDMessage(
            "✅ Chat vs Streamer connected to Twitch!",
            HUDMessage.newQuest_type));

        // Don't rely solely on Initialize(credentials, channel)'s auto-join — join explicitly
        // so a bad channel name or a flaky auto-join shows up as a clear log line either way.
        if (_client?.JoinedChannels.Count == 0)
            _client?.JoinChannel(_config.ChannelName);

        SyncCoopChannels();
    }

    // ─── Multiplayer Targeting — co-op player channels ─────────────────────────

    private IEnumerable<(bool Enabled, string Channel, bool AllowReplies)> CoopSlots() => new[]
    {
        (_config.MultiplayerPlayer2Enabled, _config.MultiplayerPlayer2Channel, _config.MultiplayerPlayer2AllowReplies),
        (_config.MultiplayerPlayer3Enabled, _config.MultiplayerPlayer3Channel, _config.MultiplayerPlayer3AllowReplies),
        (_config.MultiplayerPlayer4Enabled, _config.MultiplayerPlayer4Channel, _config.MultiplayerPlayer4AllowReplies),
    };

    /// <summary>
    /// Whether the bot is allowed to post into the given channel. Your own channel is always
    /// allowed; a co-op channel is allowed unless that player's "Allow Bot Replies" toggle is
    /// off. The bot still joins and reads an opted-out channel — reading is what makes targeting
    /// work — it just never types anything back into it.
    /// </summary>
    private bool CanPostTo(string channel)
    {
        if (channel.Equals(_config.ChannelName, StringComparison.OrdinalIgnoreCase)) return true;

        foreach (var (enabled, coopChannel, allowReplies) in CoopSlots())
        {
            if (enabled && !string.IsNullOrWhiteSpace(coopChannel) &&
                coopChannel.Trim().Equals(channel, StringComparison.OrdinalIgnoreCase))
                return allowReplies;
        }

        return true; // not one of our configured channels — nothing to suppress
    }

    /// <summary>
    /// Joins every enabled co-op player channel and parts any previously-joined co-op channel
    /// that's since been disabled or renamed, based on the current config. Safe to call anytime
    /// — a no-op before the IRC client is connected (the normal post-connect join covers that
    /// case), and callable again from GMCM's save callback so toggling a slot takes effect
    /// immediately instead of requiring a restart.
    /// </summary>
    public void SyncCoopChannels()
    {
        if (_client == null || !_client.IsConnected) return;

        var desired = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (_config.MultiplayerTargetingEnabled)
        {
            foreach (var (enabled, channel, _) in CoopSlots())
                if (enabled && !string.IsNullOrWhiteSpace(channel))
                    desired.Add(channel.Trim());
        }

        foreach (var channel in desired)
        {
            if (_client.JoinedChannels.Any(c => c.Channel.Equals(channel, StringComparison.OrdinalIgnoreCase)))
                continue;
            _monitor.Log($"[TwitchManager] Joining co-op player channel: {channel}", LogLevel.Info);
            _client.JoinChannel(channel);
        }

        foreach (var joined in _client.JoinedChannels.ToList())
        {
            if (joined.Channel.Equals(_config.ChannelName, StringComparison.OrdinalIgnoreCase)) continue;
            if (desired.Contains(joined.Channel)) continue;
            _monitor.Log($"[TwitchManager] Leaving co-op player channel: {joined.Channel}", LogLevel.Info);
            _client.LeaveChannel(joined.Channel);
        }
    }

    private void OnIrcJoinedChannel(object? sender, TwitchLib.Client.Events.OnJoinedChannelArgs e)
    {
        _monitor.Log($"[TwitchManager] Joined channel: {e.Channel}", LogLevel.Info);
    }

    private bool _reconnecting = false;
    private DateTime _lastDisconnectEvent = DateTime.MinValue;

    private void OnIrcDisconnected(object? sender, OnDisconnectedEventArgs e)
    {
        // Debounce — TwitchLib can fire multiple disconnect events for one underlying disconnect
        if ((DateTime.UtcNow - _lastDisconnectEvent).TotalSeconds < 3)
        {
            _monitor.Log("[TwitchManager] Ignoring duplicate disconnect event.", LogLevel.Trace);
            return;
        }
        _lastDisconnectEvent = DateTime.UtcNow;
        _monitor.Log("[TwitchManager] IRC disconnected.", LogLevel.Warn);

        if (_reconnecting)
        {
            _monitor.Log("[TwitchManager] Reconnect already in progress — skipping.", LogLevel.Debug);
            return;
        }

        _reconnecting = true; // set synchronously to close the race window
        Task.Run(() => IrcReconnectLoop());
    }

    private void OnIrcConnectionError(object? sender, OnConnectionErrorArgs e)
    {
        if ((DateTime.UtcNow - _lastDisconnectEvent).TotalSeconds < 3)
        {
            _monitor.Log("[TwitchManager] Ignoring duplicate connection error event.", LogLevel.Trace);
            return;
        }
        _lastDisconnectEvent = DateTime.UtcNow;
        _monitor.Log($"[TwitchManager] IRC connection error: {e.Error.Message}", LogLevel.Warn);

        if (_reconnecting)
        {
            _monitor.Log("[TwitchManager] Reconnect already in progress — skipping.", LogLevel.Debug);
            return;
        }

        _reconnecting = true;
        Task.Run(() => IrcReconnectLoop());
    }

    private async Task IrcReconnectLoop()
    {
        // Everything in this method runs on a fire-and-forget Task.Run — an unhandled
        // exception anywhere in here would otherwise leave _reconnecting stuck true
        // forever, silently disabling auto-reconnect for the rest of the session.
        try
        {
            await IrcReconnectLoopCore();
            _monitor.Log("[TwitchManager] IRC reconnected successfully.", LogLevel.Info);
        }
        catch (Exception ex)
        {
            _monitor.Log($"[TwitchManager] IrcReconnectLoop failed unexpectedly: {ex.Message}", LogLevel.Error);
        }
        finally
        {
            _reconnecting = false;
        }
    }

    private async Task IrcReconnectLoopCore()
    {
        int attempt = 0;
        bool justRecreatedClient = false;

        // Give TwitchLib a moment to settle, then check the SOCKET state —
        // but don't trust this as proof of a working login. A socket can
        // reconnect successfully even while the underlying token is expired,
        // since auth failure doesn't always tear the connection back down
        // immediately. Validate the actual token against Twitch's API instead.
        await Task.Delay(2000);

        bool tokenValid = await TwitchAuth.IsTokenValidAsync(_currentToken);
        if (!tokenValid)
        {
            _monitor.Log("[TwitchManager] Current token is invalid/expired — refreshing immediately...", LogLevel.Warn);
            var refreshed = await TwitchAuth.RefreshAccessTokenAsync();
            if (refreshed != null)
            {
                _monitor.Log("[TwitchManager] Token refreshed — reinitializing IRC...", LogLevel.Info);
                try
                {
                    if (_client?.IsConnected == true) _client.Disconnect();
                    await Task.Delay(1000);
                    SetupIrcClient(refreshed);
                    await Task.Delay(4000);
                    justRecreatedClient = true;
                }
                catch (Exception ex)
                {
                    _monitor.Log($"[TwitchManager] Reinitializing IRC after token refresh failed: {ex.Message}", LogLevel.Warn);
                }
            }
            else
            {
                _monitor.Log("[TwitchManager] Refresh failed — falling back to standard reconnect attempts.", LogLevel.Error);
            }
        }
        else if (_client?.IsConnected ?? false)
        {
            _monitor.Log("[TwitchManager] IRC already reconnected on its own and token is valid.", LogLevel.Info);
            return;
        }

        while (!(_client?.IsConnected ?? false))
        {
            attempt++;
            int delay = attempt <= 5
                ? Math.Min(30, (int)Math.Pow(2, attempt)) * 1000
                : 60000; // back off to a full minute after 5 tries to avoid hammering Twitch

            _monitor.Log($"[TwitchManager] IRC reconnect attempt {attempt} in {delay / 1000}s...", LogLevel.Info);
            await Task.Delay(delay);

            if (_client?.IsConnected ?? false) break;

            // The client we just created above may still be mid-handshake —
            // give it this extra delay to finish instead of calling Reconnect()
            // on a connection attempt that's already in flight.
            if (justRecreatedClient)
            {
                justRecreatedClient = false;
                continue;
            }

            try
            {
                if (attempt % 3 == 0)
                {
                    _monitor.Log("[TwitchManager] Attempting token refresh before reconnect...", LogLevel.Info);
                    var newToken = await TwitchAuth.RefreshAccessTokenAsync();
                    if (newToken != null)
                    {
                        _monitor.Log("[TwitchManager] Token refreshed — reinitializing IRC...", LogLevel.Info);
                        if (_client?.IsConnected == true) _client.Disconnect();
                        await Task.Delay(1000);
                        SetupIrcClient(newToken);
                        await Task.Delay(4000);
                        break;
                    }
                }
                else
                {
                    _client?.Reconnect();
                    await Task.Delay(4000);
                }
            }
            catch (Exception ex)
            {
                _monitor.Log($"[TwitchManager] IRC reconnect attempt {attempt} failed: {ex.Message}", LogLevel.Warn);
            }
        }
    }

    private bool IsIgnored(string username)
    {
        return _config.IgnoredUsers
            .Split(',')
            .Select(u => u.Trim().ToLower())
            .Contains(username.ToLower());
    }

    private void OnUserJoined(object? sender, OnUserJoinedArgs e)
    {
        if (IsIgnored(e.Username)) return;
        TrackViewer(e.Username);
    }

    private void OnUserBanned(object? sender, OnUserBannedArgs e)
    {
        var username = e.UserBan.Username;
        _monitor.Log($"[TwitchManager] {username} was banned — removing from chat feed", LogLevel.Debug);
        _chatFeed?.RemoveByUser(username);
        _overlay?.PushChatRemoveUser(username);
    }

    private void OnUserTimedout(object? sender, OnUserTimedoutArgs e)
    {
        var username = e.UserTimeout.Username;
        _monitor.Log($"[TwitchManager] {username} was timed out — removing from chat feed", LogLevel.Debug);
        _chatFeed?.RemoveByUser(username);
        _overlay?.PushChatRemoveUser(username);
    }

    private void OnMessageCleared(object? sender, OnMessageClearedArgs e)
    {
        // TwitchLib's OnMessageCleared fires when a specific message is deleted
        // Remove by username as a safe fallback since the exact ID API varies by version
        try
        {
            // Try to get the username from the args — different TwitchLib versions expose different properties
            var type    = e.GetType();
            var userProp = type.GetProperty("TargetUserLogin")
                        ?? type.GetProperty("Username")
                        ?? type.GetProperty("Message");
            var val = userProp?.GetValue(e)?.ToString() ?? "";
            if (!string.IsNullOrWhiteSpace(val))
            {
                _monitor.Log($"[TwitchManager] Message cleared for {val}", LogLevel.Debug);
                _chatFeed?.RemoveByUser(val);
                _overlay?.PushChatRemoveUser(val);
            }
        }
        catch (Exception ex)
        {
            _monitor.Log($"[TwitchManager] Message clear handler error: {ex.Message}", LogLevel.Debug);
        }
    }

    private void OnChatCleared(object? sender, OnChatClearedArgs e)
    {
        _monitor.Log($"[TwitchManager] Chat cleared", LogLevel.Debug);
        _chatFeed?.Clear();
        _overlay?.PushChatClear();
    }

    private void OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        if (!_config.EnableChatCommands) return;

        // Names which channel this message came from for MultiplayerTargeting.Resolve — cleared
        // in the finally below. Safe as an ambient static: everything downstream of this handler
        // (command parsing, SabotageEngine.TryBuy, ISabotage.Execute) runs synchronously within
        // this one TwitchLib event callback.
        MultiplayerTargeting.CurrentChannel = e.ChatMessage.Channel;
        try
        {
            HandleMessage(e);
        }
        finally
        {
            MultiplayerTargeting.CurrentChannel = null;
        }
    }

    private void HandleMessage(OnMessageReceivedArgs e)
    {
        var username = e.ChatMessage.Username;
        var message  = e.ChatMessage.Message.Trim();
        var channel  = e.ChatMessage.Channel;

        if (IsIgnored(username)) return;

        TrackViewer(username);
        _points.OnChat(username);

        // Content filter
        if (UI.ContentFilter.IsBlocked(message, _config.BlockedKeywords))
        {
            _monitor.Log($"[TwitchManager] Blocked message from {username} — suppressed", LogLevel.Debug);
            _chatFeed?.RemoveByUser(username); // also clear any prior messages if slur detected
            _overlay?.PushChatRemoveUser(username);
            return;
        }

        _chatFeed?.Add(username, message, UI.ChatPlatform.Twitch, e.ChatMessage.Id,
            BuildRenderedMessage(e.ChatMessage.Message.Trim(), e.ChatMessage.EmoteSet?.Emotes));

        // Raider bonus — if active, award extra points on first chat
        if (_sabotage.RaidEvents.IsRaiderBonusActive)
        {
            int bonus = _sabotage.RaidEvents.RaiderBonusPoints;
            _ledger.AddPoints(username, bonus);
            _monitor.Log($"[TwitchManager] Raider bonus +{bonus}pts → {username}", LogLevel.Debug);
        }

        if (message.Equals("!balance", StringComparison.OrdinalIgnoreCase))
        {
            var balance = _points.GetBalance(username);
            SendMessage(channel, $"@{username} you have {balance} chaos points! 🎯");
            return;
        }

        if (message.Equals("!shop", StringComparison.OrdinalIgnoreCase))
        {
            SendShopList(channel);
            return;
        }

        if (message.StartsWith("!info ", StringComparison.OrdinalIgnoreCase))
        {
            var cmd = message.Substring(6).Trim().ToLower();
            var def = _sabotage.GetDefinition(cmd);
            if (def == null)
                SendMessage(channel, $"@{username} '{cmd}' wasn't found in the shop. Type !shop to see all commands.");
            else
                SendMessage(channel, $"📖 !buy {def.BuyCommand} — {def.Description} | Cost: {def.Cost}pts | Cooldown: {def.Sabotage.CooldownSeconds}s");
            return;
        }

        if (message.StartsWith("!buy ", StringComparison.OrdinalIgnoreCase))
        {
            if (!_gameActive)
            {
                SendMessage(channel, $"@{username} sabotages are disabled outside of a game session!");
                return;
            }
            var sabotageName = message.Substring(5).Trim();
            HandleBuy(username, sabotageName, channel);
            return;
        }

        if (message.StartsWith("!give ", StringComparison.OrdinalIgnoreCase))
        {
            HandleGive(username, e.ChatMessage.IsModerator, e.ChatMessage.IsBroadcaster, message, channel);
            return;
        }

        if (message.StartsWith("!setpoints ", StringComparison.OrdinalIgnoreCase))
        {
            HandleSetPoints(username, e.ChatMessage.IsModerator, e.ChatMessage.IsBroadcaster, message, channel);
        }
    }

    private void HandleSetPoints(string sender, bool isMod, bool isBroadcaster, string message, string channel)
    {
        if (!isMod && !isBroadcaster)
        {
            SendMessage(channel, $"@{sender} only mods and the broadcaster can set points.");
            return;
        }

        // Parse: !setpoints <username> <amount>
        var parts = message.Substring(11).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            SendMessage(channel, $"@{sender} usage: !setpoints <username> <amount>");
            return;
        }

        var target = parts[0].TrimStart('@');
        if (!int.TryParse(parts[1], out var amount) || amount < 0)
        {
            SendMessage(channel, $"@{sender} amount must be a non-negative number.");
            return;
        }

        var current = _ledger.GetPoints(target);
        _ledger.SetPoints(target, amount);
        SendMessage(channel, $"✨ {sender} set @{target}'s chaos points to {amount}pts (was {current}pts)");
        _monitor.Log($"[TwitchManager] {sender} set {target}'s points to {amount} (was {current})", LogLevel.Info);
    }

    private void HandleGive(string sender, bool isMod, bool isBroadcaster, string message, string channel)
    {
        if (!isMod && !isBroadcaster)
        {
            SendMessage(channel, $"@{sender} only mods and the broadcaster can give points.");
            return;
        }

        // Parse: !give <username> <amount>
        var parts = message.Substring(6).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            SendMessage(channel, $"@{sender} usage: !give <username> <amount>");
            return;
        }

        var target = parts[0].TrimStart('@');
        if (!int.TryParse(parts[1], out var amount) || amount <= 0)
        {
            SendMessage(channel, $"@{sender} amount must be a positive number.");
            return;
        }

        _ledger.AddPoints(target, amount);
        var newBalance = _points.GetBalance(target);
        SendMessage(channel, $"✨ {sender} gave {amount} chaos points to @{target}! (balance: {newBalance}pts)");
        _monitor.Log($"[TwitchManager] {sender} gave {amount}pts to {target}", LogLevel.Info);
    }

    private void OnNewSubscriber(object? sender, OnNewSubscriberArgs e)
    {
        var username = e.Subscriber.DisplayName;
        if (IsIgnored(username)) return;
        var tier     = ParseSubTier(e.Subscriber.SubscriptionPlan.ToString());
        _points.OnSub(username, tier);
        _monitor.Log($"[TwitchManager] New sub: {username} ({tier})", LogLevel.Info);
    }

    private void OnReSubscriber(object? sender, OnReSubscriberArgs e)
    {
        var username = e.ReSubscriber.DisplayName;
        if (IsIgnored(username)) return;
        var tier     = ParseSubTier(e.ReSubscriber.SubscriptionPlan.ToString());
        _points.OnSub(username, tier);
        _monitor.Log($"[TwitchManager] Resub: {username} ({tier})", LogLevel.Info);
    }

    private void OnGiftedSubscription(object? sender, OnGiftedSubscriptionArgs e)
    {
        var gifter = e.GiftedSubscription.DisplayName;
        if (IsIgnored(gifter)) return;
        _points.OnGiftSub(gifter, 1);
        _monitor.Log($"[TwitchManager] Gift sub from {gifter}", LogLevel.Info);
    }

    private void OnCommunitySubscription(object? sender, OnCommunitySubscriptionArgs e)
    {
        var gifter = e.GiftedSubscription.DisplayName;
        if (IsIgnored(gifter)) return;
        var count  = e.GiftedSubscription.MsgParamMassGiftCount;
        _points.OnGiftSub(gifter, count);
        _monitor.Log($"[TwitchManager] Gift bomb: {gifter} gifted {count} subs", LogLevel.Info);
    }

    private void OnRaidNotification(object? sender, OnRaidNotificationArgs e)
    {
        var raider = e.RaidNotification.DisplayName;
        if (!int.TryParse(e.RaidNotification.MsgParamViewerCount, out var viewerCount))
            viewerCount = 1;

        _monitor.Log($"[TwitchManager] Raid from {raider} — {viewerCount} viewers, gameActive={_gameActive}, enableRaidEvents={_config.EnableRaidEvents}", LogLevel.Info);

        _points.OnRaid(raider, viewerCount, new List<string>());

        if (_config.EnableRaidEvents && _gameActive)
        {
            _monitor.Log($"[TwitchManager] Triggering raid event for {raider}", LogLevel.Info);
            ModEntry.PendingActions.Enqueue(() =>
            {
                var result = _sabotage.RaidEvents.Execute(raider, viewerCount, SendMessage);
                _overlay?.PushFeedEvent(raider, "Raid", result.HudMessage, 0, "raid");
                _sabotage.TryClipForRaid(raider);
            });
        }
        else
        {
            _monitor.Log($"[TwitchManager] Raid event skipped — gameActive={_gameActive}, enableRaidEvents={_config.EnableRaidEvents}", LogLevel.Debug);
        }

        Game1.addHUDMessage(new HUDMessage(
            $"🚨 {raider} is raiding with {viewerCount} viewers!",
            HUDMessage.newQuest_type));
    }

    // ─── Raw PubSub WebSocket ─────────────────────────────────────────────────

    private CancellationTokenSource? _refreshCts;

    private void StartTokenRefreshTimer()
    {
        _refreshCts = new CancellationTokenSource();
        Task.Run(() => TokenRefreshLoop(_refreshCts.Token));
    }

    private async Task TokenRefreshLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // Refresh every 24 hours proactively
            await Task.Delay(TimeSpan.FromHours(24), ct);
            if (ct.IsCancellationRequested) break;

            _monitor.Log("[TwitchManager] Proactive token refresh starting...", LogLevel.Info);
            var newToken = await TwitchAuth.RefreshAccessTokenAsync();
            if (newToken != null)
            {
                _monitor.Log("[TwitchManager] Token refreshed — reinitializing IRC...", LogLevel.Info);
                try
                {
                    if (_client?.IsConnected == true) _client.Disconnect();
                    await Task.Delay(1000, ct);
                    SetupIrcClient(newToken);
                }
                catch (Exception ex)
                {
                    _monitor.Log($"[TwitchManager] IRC reinit after refresh failed: {ex.Message}", LogLevel.Warn);
                }
            }
            else
            {
                _monitor.Log("[TwitchManager] Proactive token refresh failed — will retry next cycle.", LogLevel.Warn);
            }
        }
    }

    private void SetupPubSub(string token)
    {
        _pubCts = new CancellationTokenSource();
        Task.Run(() => PubSubLoop(token, _pubCts.Token));
    }

    private async Task PubSubLoop(string token, CancellationToken ct)
    {
        // Auto-fetch broadcaster user ID if not set
        if (string.IsNullOrWhiteSpace(_config.BroadcasterUserId) ||
            _config.BroadcasterUserId == "your_broadcaster_user_id")
        {
            await FetchBroadcasterUserId(token, ct).ConfigureAwait(false);
        }

        while (!ct.IsCancellationRequested)
        {
            try
            {
                _pubSocket = new ClientWebSocket();
                await _pubSocket.ConnectAsync(new Uri("wss://pubsub-edge.twitch.tv"), ct).ConfigureAwait(false);
                _monitor.Log("[TwitchManager] PubSub connected.", LogLevel.Info);

                await PubSubSubscribe(token, ct).ConfigureAwait(false);
                _ = Task.Run(() => PubSubPingLoop(ct), ct);
                await PubSubReceiveLoop(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _monitor.Log($"[TwitchManager] PubSub error: {ex.Message} — reconnecting in 5s", LogLevel.Warn);
                await Task.Delay(5000, ct).ConfigureAwait(false);
            }
        }
    }

    private async Task FetchBroadcasterUserId(string token, CancellationToken ct)
    {
        try
        {
            using var http = new System.Net.Http.HttpClient();
            http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            http.DefaultRequestHeaders.Add("Client-Id", TwitchAuth.ClientId);

            var resp = await http.GetStringAsync("https://api.twitch.tv/helix/users", ct).ConfigureAwait(false);
            var doc  = JsonDocument.Parse(resp);
            var id   = doc.RootElement
                          .GetProperty("data")[0]
                          .GetProperty("id")
                          .GetString() ?? "";

            if (!string.IsNullOrWhiteSpace(id))
            {
                _config.BroadcasterUserId = id;
                _monitor.Log($"[TwitchManager] Auto-fetched broadcaster ID: {id}", LogLevel.Info);
            }
        }
        catch (Exception ex)
        {
            _monitor.Log($"[TwitchManager] Failed to fetch broadcaster ID: {ex.Message}", LogLevel.Warn);
        }
    }

    private async Task PubSubSubscribe(string token, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new
        {
            type  = "LISTEN",
            nonce = Guid.NewGuid().ToString("N"),
            data  = new
            {
                topics = new[]
                {
                    $"channel-bits-events-v2.{_config.BroadcasterUserId}",
                    $"channel-points-channel-v1.{_config.BroadcasterUserId}",
                    $"following.{_config.BroadcasterUserId}"
                },
                auth_token = token
            }
        });

        var bytes = Encoding.UTF8.GetBytes(payload);
        await _pubSocket!.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
        _monitor.Log("[TwitchManager] PubSub topics subscribed.", LogLevel.Debug);
    }

    private async Task PubSubPingLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(3), ct).ConfigureAwait(false);
            if (_pubSocket?.State != WebSocketState.Open) break;

            var ping = Encoding.UTF8.GetBytes("{\"type\":\"PING\"}");
            await _pubSocket.SendAsync(new ArraySegment<byte>(ping), WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
            _monitor.Log("[TwitchManager] PubSub ping sent.", LogLevel.Trace);
        }
    }

    private async Task PubSubReceiveLoop(CancellationToken ct)
    {
        var buffer = new byte[16384];

        while (_pubSocket?.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            var result = await _pubSocket.ReceiveAsync(new ArraySegment<byte>(buffer), ct).ConfigureAwait(false);
            if (result.MessageType == WebSocketMessageType.Close) break;

            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            HandlePubSubMessage(json);
        }
    }

    private void HandlePubSubMessage(string json)
    {
        try
        {
            using var doc  = JsonDocument.Parse(json);
            var root       = doc.RootElement;
            var type       = root.TryGetProperty("type", out var t) ? t.GetString() : "";

            if (type == "PONG" || type == "RESPONSE") return;
            if (type != "MESSAGE") return;

            var data  = root.GetProperty("data");
            var topic = data.GetProperty("topic").GetString() ?? "";

            using var innerDoc = JsonDocument.Parse(data.GetProperty("message").GetString() ?? "{}");
            var inner          = innerDoc.RootElement;

            if (topic.StartsWith("channel-bits-events-v2"))
                HandlePubSubBits(inner);
            else if (topic.StartsWith("channel-points-channel-v1"))
                HandlePubSubChannelPoints(inner);
            else if (topic.StartsWith("following"))
                HandlePubSubFollow(inner);
        }
        catch (Exception ex)
        {
            _monitor.Log($"[TwitchManager] PubSub parse error: {ex.Message}", LogLevel.Warn);
        }
    }

    private void HandlePubSubBits(JsonElement inner)
    {
        if (!_config.EnableBitEvents) return;

        var data     = inner.GetProperty("data");
        var username = data.GetProperty("user_name").GetString() ?? "";
        var bits     = data.GetProperty("bits_used").GetInt32();

        if (IsIgnored(username)) return;

        _points.OnBits(username, bits);

        if (_gameActive)
        {
            if (bits >= _config.LargeBitThreshold)
                _sabotage.TriggerBitEvent(username, BitTier.Large);
            else if (bits >= _config.MediumBitThreshold)
                _sabotage.TriggerBitEvent(username, BitTier.Medium);
            else if (bits >= _config.SmallBitThreshold)
                _sabotage.TriggerBitEvent(username, BitTier.Small);
        }

        _monitor.Log($"[TwitchManager] {username} cheered {bits} bits", LogLevel.Info);
    }

    private void HandlePubSubChannelPoints(JsonElement inner)
    {
        if (!_config.EnableChannelPoints) return;

        var redemption  = inner.GetProperty("data").GetProperty("redemption");
        var username    = redemption.GetProperty("user").GetProperty("login").GetString() ?? "";
        var rewardTitle = redemption.GetProperty("reward").GetProperty("title").GetString() ?? "";

        _monitor.Log($"[TwitchManager] {username} redeemed: {rewardTitle}", LogLevel.Info);

        // Starter points redemptions
        if (_config.EnableStarterRedemption)
        {
            int awarded = 0;
            if (rewardTitle.Equals(_config.StarterRedemptionTitleSmall,  StringComparison.OrdinalIgnoreCase))
                awarded = _config.StarterRedemptionPointsSmall;
            else if (rewardTitle.Equals(_config.StarterRedemptionTitleMedium, StringComparison.OrdinalIgnoreCase))
                awarded = _config.StarterRedemptionPointsMedium;
            else if (rewardTitle.Equals(_config.StarterRedemptionTitleLarge,  StringComparison.OrdinalIgnoreCase))
                awarded = _config.StarterRedemptionPointsLarge;

            if (awarded > 0)
            {
                _ledger.AddPoints(username, awarded);
                var newBalance = _points.GetBalance(username);
                SendMessage($"✨ @{username} redeemed {rewardTitle} and received {awarded} chaos points! (balance: {newBalance}pts)");
                _monitor.Log($"[TwitchManager] CPR starter: +{awarded}pts → {username}", LogLevel.Info);
                return;
            }
        }

        if (_gameActive)
            _sabotage.TryFireByName(rewardTitle, username);
    }

    private void HandlePubSubFollow(JsonElement inner)
    {
        if (!_config.EnableFollowBonus) return;

        var username = inner.GetProperty("display_name").GetString() ?? "";
        if (IsIgnored(username)) return;
        _points.OnFollow(username);
        _monitor.Log($"[TwitchManager] New follower: {username}", LogLevel.Info);

        SendMessage($"Welcome {username}! 🎉 You've earned {_config.FollowBonus} chaos points for following!");
    }

    // ─── Buy Handler ──────────────────────────────────────────────────────────

    private void HandleBuy(string username, string fullCommand, string channel)
    {
        // Split command from optional args: "renameanimal Pengu" -> command="renameanimal", args="Pengu"
        var parts   = fullCommand.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var command = parts[0].ToLower();
        var args    = parts.Length > 1 ? parts[1].Trim() : "";

        var result = _sabotage.TryBuy(username, command, args);

        switch (result.Status)
        {
            case BuyStatus.Success:
                SendMessage(channel, $"@{username} spent {result.Cost} pts — {result.Description} 😈");
                break;
            case BuyStatus.NotFound:
                SendMessage(channel, $"@{username} '{command}' isn't in the shop. Type !shop to see options.");
                break;
            case BuyStatus.InsufficientFunds:
                SendMessage(channel, $"@{username} you need {result.Cost} pts but only have {result.Balance} pts.");
                break;
            case BuyStatus.OnCooldown:
                SendMessage(channel, $"@{username} {command} is on cooldown for {result.CooldownRemaining}s.");
                break;
            case BuyStatus.Rejected:
                SendMessage(channel, $"@{username} {result.Description} Your points have been refunded.");
                break;
        }
    }

    // ─── Shop List ────────────────────────────────────────────────────────────

    private void SendShopList(string channel)
    {
        if (!_sabotage.GetShopList().Any()) { SendMessage(channel, "The chaos shop is empty!"); return; }

        SendMessage(channel, $"🛒 Full chaos shop & prices: {_config.ShopUrl} — !buy <command> | !info <command> for details");
    }

    // ─── Active Viewer Tracking ───────────────────────────────────────────────

    private static string BuildRenderedMessage(string message, List<TwitchLib.Client.Models.Emote>? emotes)
    {
        // HTML-escape the plain text first
        var rendered = UI.ChatFeed.HtmlEscape(message);

        if (emotes == null || emotes.Count == 0)
            return rendered;

        // Replace each unique emote name with an img tag
        // Use name-based replacement to avoid byte-offset issues with emoji
        var seen = new HashSet<string>();
        foreach (var emote in emotes)
        {
            if (string.IsNullOrWhiteSpace(emote.Name) || !seen.Add(emote.Name)) continue;
            var imgTag = $"<img class='emote' src='https://static-cdn.jtvnw.net/emoticons/v2/{emote.Id}/default/dark/1.0' title='{UI.ChatFeed.HtmlEscape(emote.Name)}' alt='{UI.ChatFeed.HtmlEscape(emote.Name)}'>";
            rendered = rendered.Replace(UI.ChatFeed.HtmlEscape(emote.Name), imgTag);
        }

        return rendered;
    }

    private static string HtmlEscape(string s) => UI.ChatFeed.HtmlEscape(s);

    private void TrackViewer(string username)
    {
        _activeViewers[username.ToLower()] = DateTime.UtcNow;
    }

    public List<string> GetActiveViewers()
    {
        var cutoff = DateTime.UtcNow - _viewerTimeout;
        return _activeViewers
            .Where(kv => kv.Value >= cutoff)
            .Select(kv => kv.Key)
            .ToList();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Posts to your own channel. Use the (channel, message) overload to reply into
    /// whichever channel actually triggered the message — bit/follow/channel-point events are
    /// always host-channel concepts, so they keep using this default.</summary>
    public void SendMessage(string message) => SendMessage(_config.ChannelName, message);

    /// <summary>Posts into a specific channel, silently suppressed if that channel's "Allow Bot
    /// Replies" toggle is off.</summary>
    public void SendMessage(string channel, string message)
    {
        if (_client?.IsConnected != true) return;
        if (!CanPostTo(channel))
        {
            _monitor.Log($"[TwitchManager] Suppressed reply to '{channel}' — bot replies are disabled for that channel.", LogLevel.Debug);
            return;
        }
        _client.SendMessage(channel, message);
    }

    private static SubTier ParseSubTier(string plan) => plan switch
    {
        "Prime" => SubTier.Prime,
        "Tier1" => SubTier.T1,
        "Tier2" => SubTier.T2,
        "Tier3" => SubTier.T3,
        _       => SubTier.T1
    };
}