using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using SDVChatVsStreamer.Economy;
using SDVChatVsStreamer.Sabotage;
using StardewModdingAPI;
using StardewValley;

namespace SDVChatVsStreamer.TikTok;

public enum TikTokGiftTier { Nuisance, Disruptive, Painful, Devastating }

public class TikTokManager
{
    private readonly ModConfig    _config;
    private readonly PointsEngine _points;
    private readonly ViewerLedger _ledger;
    private readonly IMonitor     _monitor;
    private SabotageEngine?       _sabotage;

    private ClientWebSocket?      _ws;
    private CancellationTokenSource? _cts;

    private readonly Dictionary<string, DateTime> _chatCooldowns = new();
    private UI.ChatFeed? _chatFeed;
    private static bool _pendingReturnToTitle = false;

    public static void ProcessPendingReturnToTitle()
    {
        if (_pendingReturnToTitle && Game1.activeClickableMenu == null)
        {
            _pendingReturnToTitle = false;
            // SDV 1.6: force return to title by setting the game state
            Game1.timeOfDay = 2600; // trigger end of day which returns to title
        }
    }

    public TikTokManager(ModConfig config, PointsEngine points, ViewerLedger ledger, IMonitor monitor, UI.ChatFeed? chatFeed = null, SabotageEngine? sabotage = null)
    {
        _config   = config;
        _points   = points;
        _ledger   = ledger;
        _monitor  = monitor;
        _chatFeed = chatFeed;
        _sabotage = sabotage;
    }

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    public void Connect()
    {
        if (!_config.EnableTikTok) return;
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => ConnectLoop(_cts.Token));
    }

    public void Disconnect()
    {
        _cts?.Cancel();
        _ws?.Dispose();
        _ws = null;
    }

    private async Task ConnectLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                _ws = new ClientWebSocket();
                var uri = new Uri($"ws://localhost:{_config.TikTokPort}/");
                await _ws.ConnectAsync(uri, ct).ConfigureAwait(false);
                _monitor.Log("[TikTokManager] Connected to Tikfinity.", LogLevel.Info);
                await ReceiveLoop(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _monitor.Log($"[TikTokManager] Connection error: {ex.Message} — retrying in 5s", LogLevel.Warn);
            }
            finally
            {
                _ws?.Dispose();
                _ws = null;
            }

            if (!ct.IsCancellationRequested)
                await Task.Delay(5000, ct).ConfigureAwait(false);
        }
    }

    private async Task ReceiveLoop(CancellationToken ct)
    {
        var buffer = new byte[65536];
        var sb     = new StringBuilder();

        while (_ws!.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            sb.Clear();
            WebSocketReceiveResult result;
            do
            {
                result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close) return;
                sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            }
            while (!result.EndOfMessage);

            try { HandleMessage(sb.ToString()); }
            catch (Exception ex) { _monitor.Log($"[TikTokManager] Parse error: {ex.Message}", LogLevel.Warn); }
        }
    }

    // ─── Message Handling ─────────────────────────────────────────────────────

    public void InjectTestEvent(string json) => HandleMessage(json);

    private void HandleMessage(string json)
    {
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("event", out var eventProp)) return;
        if (!root.TryGetProperty("data",  out var dataProp))  return;

        var eventName = eventProp.GetString() ?? "";
        _monitor.Log($"[TikTokManager] Event: {eventName}", LogLevel.Trace);

        switch (eventName.ToLower())
        {
            case "chat":      HandleChat(dataProp);      break;
            case "gift":      HandleGift(dataProp);      break;
            case "follow":    HandleFollow(dataProp);    break;
            case "like":      HandleLike(dataProp);      break;
            case "share":     HandleShare(dataProp);     break;
            case "subscribe": HandleSubscribe(dataProp); break;
            case "msgdelete":
            case "chatdelete":
                HandleMsgDelete(dataProp);
                break;
        }
    }

    // ─── Event Handlers ───────────────────────────────────────────────────────

    private void HandleMsgDelete(JsonElement data)
    {
        // Try userId or uniqueId — remove all messages from that user
        var username = GetUsername(data);
        if (!string.IsNullOrWhiteSpace(username))
        {
            _monitor.Log($"[TikTokManager] Message delete event for {username} — removing from feed", LogLevel.Debug);
            _chatFeed?.RemoveByUser(username);
        }
    }

    private void HandleChat(JsonElement data)
    {
        var username = GetUsername(data);
        if (string.IsNullOrWhiteSpace(username)) return;

        // Chat cooldown
        var now = DateTime.UtcNow;
        if (_chatCooldowns.TryGetValue(username, out var last) &&
            (now - last).TotalSeconds < _config.TikTokChatCooldown) return;

        _chatCooldowns[username] = now;

        string comment = data.TryGetProperty("comment", out var c) ? c.GetString() ?? "" : "";

        // Content filter — hard-blocked slurs + configurable keywords
        if (UI.ContentFilter.IsBlocked(comment, _config.BlockedKeywords))
        {
            _monitor.Log($"[TikTokManager] Blocked message from {username} — suppressed", LogLevel.Debug);
            return;
        }

        _ledger.AddPoints(username, _config.TikTokChatBonus);
        _chatFeed?.Add(username, comment, UI.ChatPlatform.TikTok, renderedText: UI.ChatFeed.HtmlEscape(comment));
        _monitor.Log($"[TikTokManager] +{_config.TikTokChatBonus}pts (chat) → {username}", LogLevel.Debug);
    }

    private void HandleGift(JsonElement data)
    {
        _monitor.Log($"[TikTokManager] Raw gift payload: {data.GetRawText()}", LogLevel.Debug);
        var username = GetUsername(data);
        if (string.IsNullOrWhiteSpace(username)) return;

        // Support both old flat structure and new nested giftDetails structure
        int giftType = 0;
        string giftName = "";

        if (data.TryGetProperty("giftDetails", out var giftDetails))
        {
            // New nested structure
            if (giftDetails.TryGetProperty("giftType", out var gt2)) giftType = gt2.GetInt32();
            if (giftDetails.TryGetProperty("giftName", out var gn2)) giftName = gn2.GetString() ?? "";
        }
        else
        {
            // Old flat structure fallback
            if (data.TryGetProperty("giftType", out var gt)) giftType = gt.GetInt32();
            if (data.TryGetProperty("giftName", out var gn)) giftName = gn.GetString() ?? "";
        }

        // Also try extendedGiftInfo.name as another fallback for gift name
        if (string.IsNullOrWhiteSpace(giftName) &&
            data.TryGetProperty("extendedGiftInfo", out var extName) &&
            extName.TryGetProperty("name", out var eName))
            giftName = eName.GetString() ?? "";

        // giftType 1 = streakable — only process when streak ends
        if (giftType == 1)
            if (data.TryGetProperty("repeatEnd", out var re) && !re.GetBoolean()) return;

        int diamondCount = 0;
        if (data.TryGetProperty("extendedGiftInfo", out var ext) &&
            ext.TryGetProperty("diamond_count", out var dc))
            diamondCount = dc.GetInt32();

        int repeatCount = 1;
        if (data.TryGetProperty("repeatCount", out var rc)) repeatCount = rc.GetInt32();

        int totalDiamonds = diamondCount * repeatCount;

        // Points award
        int points = totalDiamonds * _config.TikTokPointsPerDiamond;
        if (points <= 0) points = _config.TikTokChatBonus;
        _ledger.AddPoints(username, points);
        _monitor.Log($"[TikTokManager] +{points}pts (gift '{giftName}' {diamondCount}💎×{repeatCount}) → {username}", LogLevel.Debug);

        // ─── Named Gift Overrides ─────────────────────────────────────────────
        var giftLower = giftName.ToLower();

        // TikTok Universe — Mr. Qi forces end of day
        if (giftLower.Contains("universe"))
        {
            _monitor.Log($"[TikTokManager] TikTok Universe from {username} — triggering Mr. Qi!", LogLevel.Info);
            ModEntry.PendingActions.Enqueue(() =>
            {
                Game1.addHUDMessage(new HUDMessage(
                    $"⚡ {username} sent a TikTok Universe!", HUDMessage.error_type));

                MrQiDialogue.Show(new[]
                {
                    "...",
                    "A far-off observer has grown weary of your interference, farmer.",
                    "You have disrupted the flow of this day one too many times...",
                    "It is time to begin again.",
                    "*The world resets.*"
                }, onDismissed: () => { _pendingReturnToTitle = true; });
            });
            return;
        }

        // Heart Me — heart emote
        if (giftLower.Contains("heart me") || giftLower == "heart")
        {
            ModEntry.PendingActions.Enqueue(() =>
            {
                Game1.player.performPlayerEmote("heart");
                Game1.addHUDMessage(new HUDMessage(
                    $"💖 {username} sent a Heart Me!", HUDMessage.newQuest_type));
            });
            return;
        }

        // Rose — happy emote
        if (giftLower.Contains("rose"))
        {
            ModEntry.PendingActions.Enqueue(() =>
            {
                Game1.player.performPlayerEmote("happy");
                Game1.addHUDMessage(new HUDMessage(
                    $"🌹 {username} sent a Rose!", HUDMessage.newQuest_type));
            });
            return;
        }

        // Ice Cream — adds an ice cream to inventory
        if (giftLower.Contains("ice cream") || giftLower.Contains("icecream"))
        {
            ModEntry.PendingActions.Enqueue(() =>
            {
                var iceCream = ItemRegistry.Create("(O)233");
                if (Game1.player.addItemToInventory(iceCream) == null)
                    Game1.addHUDMessage(new HUDMessage(
                        $"🍦 {username} sent you an Ice Cream!", HUDMessage.newQuest_type));
                else
                    Game1.addHUDMessage(new HUDMessage(
                        $"🍦 {username} sent an Ice Cream but your inventory is full!", HUDMessage.error_type));
            });
            return;
        }

        // Mystery Box — adds a mystery box AND triggers a devastating sabotage
        if (giftLower.Contains("mystery"))
        {
            ModEntry.PendingActions.Enqueue(() =>
            {
                var box = ItemRegistry.Create("(O)MysteryBox");
                if (Game1.player.addItemToInventory(box) == null)
                    Game1.addHUDMessage(new HUDMessage(
                        $"📦 {username} sent a Mystery Box! But something feels wrong...", HUDMessage.error_type));
                else
                    Game1.addHUDMessage(new HUDMessage(
                        $"📦 {username} sent a Mystery Box! Inventory full — and chaos incoming!", HUDMessage.error_type));
                _sabotage?.TriggerRandomTikTokSabotage(username, TikTokGiftTier.Devastating);
            });
            return;
        }

        // Fireworks — mega bomb explosion at player's feet
        if (giftLower.Contains("firework"))
        {
            ModEntry.PendingActions.Enqueue(() =>
            {
                Game1.addHUDMessage(new HUDMessage(
                    $"💣 {username} launched Fireworks! INCOMING!", HUDMessage.error_type));
                var loc  = Game1.player.currentLocation;
                var tile = new Microsoft.Xna.Framework.Vector2(
                    Game1.player.TilePoint.X, Game1.player.TilePoint.Y);
                loc.explode(tile, 5, Game1.player);
            });
            return;
        }

        // ─── Tier System ──────────────────────────────────────────────────────
        FireGiftTier(username, giftName, totalDiamonds);
    }

    private void FireGiftTier(string username, string giftName, int diamonds)
    {
        if (diamonds <= 0) return;

        string hudMsg;
        string emoji;

        if (diamonds >= 500)
        {
            // Blessing tier
            emoji   = "🌟";
            hudMsg  = $"{emoji} {username} sent an epic gift ({giftName}, {diamonds}💎)! A blessing falls upon the farm!";
            _monitor.Log($"[TikTokManager] Gift tier: Blessing ({diamonds}💎) from {username}", LogLevel.Debug);
            ModEntry.PendingActions.Enqueue(() =>
            {
                Game1.addHUDMessage(new HUDMessage(hudMsg, HUDMessage.newQuest_type));
                _sabotage?.TriggerRandomTikTokBlessing(username);
            });
        }
        else if (diamonds >= 200)
        {
            emoji  = "💀";
            hudMsg = $"{emoji} {username} sent a devastating gift ({giftName}, {diamonds}💎)!";
            _monitor.Log($"[TikTokManager] Gift tier: Devastating ({diamonds}💎) from {username}", LogLevel.Debug);
            ModEntry.PendingActions.Enqueue(() =>
            {
                Game1.addHUDMessage(new HUDMessage(hudMsg, HUDMessage.error_type));
                _sabotage?.TriggerRandomTikTokSabotage(username, TikTokGiftTier.Devastating);
            });
        }
        else if (diamonds >= 50)
        {
            emoji  = "💥";
            hudMsg = $"{emoji} {username} sent a painful gift ({giftName}, {diamonds}💎)!";
            _monitor.Log($"[TikTokManager] Gift tier: Painful ({diamonds}💎) from {username}", LogLevel.Debug);
            ModEntry.PendingActions.Enqueue(() =>
            {
                Game1.addHUDMessage(new HUDMessage(hudMsg, HUDMessage.error_type));
                _sabotage?.TriggerRandomTikTokSabotage(username, TikTokGiftTier.Painful);
            });
        }
        else if (diamonds >= 10)
        {
            emoji  = "😈";
            hudMsg = $"{emoji} {username} sent a disruptive gift ({giftName}, {diamonds}💎)!";
            _monitor.Log($"[TikTokManager] Gift tier: Disruptive ({diamonds}💎) from {username}", LogLevel.Debug);
            ModEntry.PendingActions.Enqueue(() =>
            {
                Game1.addHUDMessage(new HUDMessage(hudMsg, HUDMessage.error_type));
                _sabotage?.TriggerRandomTikTokSabotage(username, TikTokGiftTier.Disruptive);
            });
        }
        else
        {
            emoji  = "😤";
            hudMsg = $"{emoji} {username} sent a nuisance gift ({giftName}, {diamonds}💎)!";
            _monitor.Log($"[TikTokManager] Gift tier: Nuisance ({diamonds}💎) from {username}", LogLevel.Debug);
            ModEntry.PendingActions.Enqueue(() =>
            {
                Game1.addHUDMessage(new HUDMessage(hudMsg, HUDMessage.error_type));
                _sabotage?.TriggerRandomTikTokSabotage(username, TikTokGiftTier.Nuisance);
            });
        }
    }

    private void HandleFollow(JsonElement data)
    {
        var username = GetUsername(data);
        if (string.IsNullOrWhiteSpace(username)) return;
        _ledger.AddPoints(username, _config.TikTokFollowBonus);
        _monitor.Log($"[TikTokManager] +{_config.TikTokFollowBonus}pts (follow) → {username}", LogLevel.Debug);
    }

    private void HandleLike(JsonElement data)
    {
        var username = GetUsername(data);
        if (string.IsNullOrWhiteSpace(username)) return;

        // likeCount = number of likes in this batch event
        int likeCount = 1;
        if (data.TryGetProperty("likeCount", out var lc)) likeCount = lc.GetInt32();

        int points = likeCount * _config.TikTokLikeBonus;
        _ledger.AddPoints(username, points);
        _monitor.Log($"[TikTokManager] +{points}pts (like×{likeCount}) → {username}", LogLevel.Debug);
    }

    private void HandleShare(JsonElement data)
    {
        var username = GetUsername(data);
        if (string.IsNullOrWhiteSpace(username)) return;
        _ledger.AddPoints(username, _config.TikTokShareBonus);
        _monitor.Log($"[TikTokManager] +{_config.TikTokShareBonus}pts (share) → {username}", LogLevel.Debug);
    }

    private void HandleSubscribe(JsonElement data)
    {
        var username = GetUsername(data);
        if (string.IsNullOrWhiteSpace(username)) return;
        _ledger.AddPoints(username, _config.TikTokSubBonus);
        _monitor.Log($"[TikTokManager] +{_config.TikTokSubBonus}pts (subscribe) → {username}", LogLevel.Debug);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static string GetUsername(JsonElement data)
    {
        // In TikTok-Live-Connector, uniqueId is top-level
        if (data.TryGetProperty("uniqueId",  out var uid))  return uid.GetString() ?? "";
        if (data.TryGetProperty("nickname",  out var nick)) return nick.GetString() ?? "";
        // Fallback: nested user object
        if (data.TryGetProperty("user", out var user))
        {
            if (user.TryGetProperty("uniqueId", out var u1)) return u1.GetString() ?? "";
            if (user.TryGetProperty("nickname", out var u2)) return u2.GetString() ?? "";
        }
        return "";
    }
}