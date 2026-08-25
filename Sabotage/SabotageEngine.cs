using Microsoft.Xna.Framework;
using SDVChatVsStreamer.Economy;
using SDVChatVsStreamer.Twitch;
using StardewModdingAPI;
using StardewValley;

namespace SDVChatVsStreamer.Sabotage;

public class SabotageEngine
{
    private readonly ViewerLedger _ledger;
    private readonly IMonitor     _monitor;
    private readonly ModConfig    _config;

    private readonly Dictionary<string, SabotageDefinition> _shop = new();

    private readonly Dictionary<BitTier, List<SabotageDefinition>> _bitPools = new()
    {
        [BitTier.Small]  = new(),
        [BitTier.Medium] = new(),
        [BitTier.Large]  = new(),
    };

    private readonly Dictionary<DonationTier, List<SabotageDefinition>> _donationPools = new()
    {
        [DonationTier.Small]   = new(),
        [DonationTier.Medium]  = new(),
        [DonationTier.Large]   = new(),
        [DonationTier.Massive] = new(),
    };

    private readonly Random _rng = new();

    // Optional overlay reference for push updates
    private Overlay.OverlayServer? _overlay;
    private ClipService?           _clipService;

    // Raid event system
    public RaidEventSystem RaidEvents { get; private set; }

    public SabotageEngine(ViewerLedger ledger, IMonitor monitor, ModConfig config)
    {
        _ledger     = ledger;
        _monitor    = monitor;
        _config     = config;
        RaidEvents  = new RaidEventSystem(monitor);
    }

    public void SetOverlay(Overlay.OverlayServer overlay) => _overlay = overlay;
    public void SetClipService(ClipService clipService)   => _clipService = clipService;

    // ─── Registration ─────────────────────────────────────────────────────────

    public void Register(ISabotage sabotage)
    {
        var def = new SabotageDefinition { Sabotage = sabotage };
        _shop[sabotage.BuyCommand.ToLower()] = def;
        _monitor.Log($"[SabotageEngine] Registered: {sabotage.Name} ({sabotage.Cost}pts)", LogLevel.Debug);
    }

    public void RegisterBitEvent(ISabotage sabotage, BitTier tier)
    {
        var def = new SabotageDefinition { Sabotage = sabotage };
        _bitPools[tier].Add(def);
        _monitor.Log($"[SabotageEngine] Registered bit event ({tier}): {sabotage.Name}", LogLevel.Debug);
    }

    public void RegisterDonationEvent(ISabotage sabotage, DonationTier tier)
    {
        var def = new SabotageDefinition { Sabotage = sabotage };
        _donationPools[tier].Add(def);
        _monitor.Log($"[SabotageEngine] Registered donation event ({tier}): {sabotage.Name}", LogLevel.Debug);
    }

    // ─── Buy Flow ─────────────────────────────────────────────────────────────

    public BuyResult TryBuy(string username, string buyCommand, string args = "")
    {
        var key = buyCommand.ToLower().Trim();

        if (!_shop.TryGetValue(key, out var def))
            return new BuyResult { Status = BuyStatus.NotFound };

        if (def.IsOnCooldown)
            return new BuyResult
            {
                Status            = BuyStatus.OnCooldown,
                CooldownRemaining = def.CooldownRemaining,
                Cost              = def.Cost
            };

        var balance       = _ledger.GetPoints(username);
        var effectiveCost = RaidEvents.ApplyCostModifier(def.Cost);

        if (balance < effectiveCost)
            return new BuyResult
            {
                Status  = BuyStatus.InsufficientFunds,
                Cost    = effectiveCost,
                Balance = balance
            };

        _ledger.DeductPoints(username, effectiveCost);

        // Validate before executing — refund if rejected
        var validationError = def.Sabotage.Validate(args);
        if (validationError != null)
        {
            _ledger.AddPoints(username, effectiveCost);
            return new BuyResult
            {
                Status      = BuyStatus.Rejected,
                Description = validationError
            };
        }

        def.Fire(username, args);

        RecordSabotage();

        // Auto-clip if configured for this tier
        _clipService?.TryClipForTier(def.Sabotage.Tier, def.BuyCommand, username, _config);

        _overlay?.PushFeedEvent(username, def.Name, def.Description, effectiveCost, "buy");
        _overlay?.PushShopUpdate();

        _monitor.Log($"[SabotageEngine] {username} bought '{def.Name}' for {effectiveCost}pts", LogLevel.Info);

        return new BuyResult
        {
            Status      = BuyStatus.Success,
            Cost        = effectiveCost,
            Balance     = _ledger.GetPoints(username),
            Description = def.Description
        };
    }

    // ─── Triggered Events ─────────────────────────────────────────────────────
    // Raids fire through RaidEvents.Execute(...) directly (see RaidEventSystem) —
    // it already covers Chaos/Blessing/Meta outcomes scaled by raid size.

    public void TriggerBitEvent(string username, BitTier tier)
    {
        ModEntry.PendingActions.Enqueue(() => FireBitEvent(username, tier));
    }

    private void FireBitEvent(string username, BitTier tier)
    {
        FireTieredEvent(_bitPools[tier], username, tier.ToString(), "bits", fireCount: 1,
            buildHudMessage: desc => $"💰 {username}'s bits triggered: {desc}!");
    }

    public void TriggerDonationEvent(string username, DonationTier tier, double amount)
    {
        ModEntry.PendingActions.Enqueue(() => FireDonationEvent(username, tier, amount));
    }

    private void FireDonationEvent(string username, DonationTier tier, double amount)
    {
        // Massive donations fire two effects instead of one — same "extra bang" idea as a big raid
        int fires = tier == DonationTier.Massive ? 2 : 1;
        FireTieredEvent(_donationPools[tier], username, tier.ToString(), "donation", fires,
            buildHudMessage: desc => $"💸 {username} donated ${amount:F2}! Triggered: {desc}!");
    }

    /// <summary>
    /// Shared firing logic for bit/donation pools: pick up to fireCount random entries,
    /// fire them, push feed/shop updates, log, and show one HUD message.
    /// </summary>
    private void FireTieredEvent(List<SabotageDefinition> pool, string username, string tierLabel,
        string eventType, int fireCount, Func<string, string> buildHudMessage)
    {
        if (pool.Count == 0)
        {
            _monitor.Log($"[SabotageEngine] {eventType} pool ({tierLabel}) is empty.", LogLevel.Warn);
            return;
        }

        var chosen = pool.OrderBy(_ => _rng.Next()).Take(Math.Min(fireCount, pool.Count)).ToList();

        var names = new List<string>();
        foreach (var def in chosen)
        {
            def.Fire(username);
            names.Add(def.Name);
            _overlay?.PushFeedEvent(username, def.Name, def.Description, 0, eventType);
        }
        _overlay?.PushShopUpdate();

        var desc = string.Join(" + ", names);
        _monitor.Log($"[SabotageEngine] {eventType} event ({tierLabel}): {desc} for {username}", LogLevel.Info);

        Game1.addHUDMessage(new HUDMessage(buildHudMessage(desc), HUDMessage.error_type));
    }

    // ─── Auto Trigger ─────────────────────────────────────────────────────────

    private DateTime _lastSabotageTime = DateTime.UtcNow;
    private static readonly Random _autoRng = new();

    public void RecordSabotage() => _lastSabotageTime = DateTime.UtcNow;

    public void TickAutoTrigger(Action<string> sendChat)
    {
        if (!_config.AutoTriggerEnabled) return;
        if (!StardewModdingAPI.Context.IsWorldReady) return;
        if ((DateTime.UtcNow - _lastSabotageTime).TotalMinutes < _config.AutoTriggerMinutes) return;

        var pool = _config.AutoTriggerPool
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().ToLower())
            .Where(s => _shop.ContainsKey(s))
            .ToList();

        if (pool.Count == 0) return;

        var command = pool[_autoRng.Next(pool.Count)];
        var def     = _shop[command];

        _lastSabotageTime = DateTime.UtcNow;
        def.Fire("The Chaos Gods");

        _overlay?.PushFeedEvent("The Chaos Gods", def.Name, def.Description, 0, "buy");
        _overlay?.PushShopUpdate();

        sendChat($"🌩️ The Chaos Gods grow restless... !buy {command} was triggered automatically! Type !shop to join the chaos!");
        _monitor.Log($"[SabotageEngine] Auto-trigger fired: {command}", LogLevel.Info);
    }

    public bool DebugBuy(string username, string buyCommand)
    {
        var def = _shop.Values.FirstOrDefault(d =>
            d.BuyCommand.Equals(buyCommand, StringComparison.OrdinalIgnoreCase));

        if (def == null) return false;

        def.Fire(username);
        _overlay?.PushFeedEvent(username, def.Name, def.Description, 0, "buy");
        _overlay?.PushShopUpdate();
        _clipService?.TryClipForTier(def.Sabotage.Tier, def.BuyCommand, username, _config);
        _monitor.Log($"[SabotageEngine] Debug buy: {username} triggered {buyCommand}", LogLevel.Info);
        return true;
    }

    public bool TryFireByName(string name, string username)
    {
        var def = _shop.Values.FirstOrDefault(d =>
            d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (def == null) return false;

        def.Fire(username);

        _overlay?.PushFeedEvent(username, def.Name, def.Description, 0, "channelpoints");
        _overlay?.PushShopUpdate();

        _monitor.Log($"[SabotageEngine] Channel point redemption: {def.Name} for {username}", LogLevel.Info);
        return true;
    }

    // ─── Shop Query ───────────────────────────────────────────────────────────

    // ─── TikTok Gift Tiers ────────────────────────────────────────────────────

    private static readonly string[] NuisancePool     = { "rain", "wind", "trash", "dizzy", "drain" };
    private static readonly string[] DisruptivePool   = { "slime", "bat", "crows", "broke", "speedup", "drunk" };
    private static readonly string[] PainfulPool      = { "bomb", "steal", "weeds", "swarm", "warp" };
    private static readonly string[] DevastatingPool  = { "sleep", "killfarm", "greenrain" };
    private static readonly string[] BlessingPool     = { "restoreenergy", "restorehealth", "givegold", "watercrops", "speedboost", "fertilize", "cleardebris", "sunny" };

    public void TriggerRandomTikTokSabotage(string username, TikTok.TikTokGiftTier tier)
    {
        var pool = tier switch
        {
            TikTok.TikTokGiftTier.Nuisance    => NuisancePool,
            TikTok.TikTokGiftTier.Disruptive  => DisruptivePool,
            TikTok.TikTokGiftTier.Painful     => PainfulPool,
            TikTok.TikTokGiftTier.Devastating => DevastatingPool,
            _                                 => NuisancePool
        };

        // Pick a random sabotage from the pool that exists in the shop
        var available = pool.Where(cmd => _shop.ContainsKey(cmd)).ToList();
        if (available.Count == 0) return;

        var cmd = available[_rng.Next(available.Count)];
        if (_shop.TryGetValue(cmd, out var def))
        {
            _monitor.Log($"[SabotageEngine] TikTok gift triggered: {def.Name} for {username}", LogLevel.Info);
            def.Fire(username);
            _overlay?.PushFeedEvent(username, def.Name, def.Description, 0, "tiktok");
        }
    }

    public void TriggerRandomTikTokBlessing(string username)
    {
        var available = BlessingPool.Where(cmd => _shop.ContainsKey(cmd)).ToList();
        if (available.Count == 0) return;

        var cmd = available[_rng.Next(available.Count)];
        if (_shop.TryGetValue(cmd, out var def))
        {
            _monitor.Log($"[SabotageEngine] TikTok blessing triggered: {def.Name} for {username}", LogLevel.Info);
            def.Fire(username);
            _overlay?.PushFeedEvent(username, def.Name, def.Description, 0, "tiktok");
        }
    }

    public List<SabotageDefinition> GetShopList() =>
        _shop.Values.OrderBy(d => d.Cost).ToList();

    public SabotageDefinition? GetDefinition(string buyCommand)
    {
        _shop.TryGetValue(buyCommand.ToLower(), out var def);
        return def;
    }
}

// ─── Supporting types ─────────────────────────────────────────────────────────

public enum BitTier { Small, Medium, Large }

public enum DonationTier { Small, Medium, Large, Massive }

public enum BuyStatus { Success, NotFound, InsufficientFunds, OnCooldown, GameNotActive, Rejected }

public class BuyResult
{
    public BuyStatus Status { get; init; }
    public int Cost { get; init; }
    public int Balance { get; init; }
    public string Description { get; init; } = "";
    public int CooldownRemaining { get; init; }
}