using SDVChatVsStreamer.Economy;
using StardewModdingAPI;
using StardewValley;

namespace SDVChatVsStreamer.Sabotage;

public class SabotageEngine
{
    private readonly ViewerLedger _ledger;
    private readonly IMonitor _monitor;

    private readonly Dictionary<string, SabotageDefinition> _shop = new();
    private readonly List<SabotageDefinition> _raidPool            = new();
    private readonly List<SabotageDefinition> _smallBitPool        = new();
    private readonly List<SabotageDefinition> _mediumBitPool       = new();
    private readonly List<SabotageDefinition> _largeBitPool        = new();

    private readonly Random _rng = new();

    // Optional overlay reference for push updates
    private Overlay.OverlayServer? _overlay;

    // Raid event system
    public RaidEventSystem RaidEvents { get; private set; }

    public SabotageEngine(ViewerLedger ledger, IMonitor monitor)
    {
        _ledger     = ledger;
        _monitor    = monitor;
        RaidEvents  = new RaidEventSystem(monitor);
    }

    public void SetOverlay(Overlay.OverlayServer overlay) => _overlay = overlay;

    // ─── Registration ─────────────────────────────────────────────────────────

    public void Register(ISabotage sabotage)
    {
        var def = new SabotageDefinition { Sabotage = sabotage };
        _shop[sabotage.BuyCommand.ToLower()] = def;
        _monitor.Log($"[SabotageEngine] Registered: {sabotage.Name} ({sabotage.Cost}pts)", LogLevel.Debug);
    }

    public void RegisterRaidEvent(ISabotage sabotage)
    {
        _raidPool.Add(new SabotageDefinition { Sabotage = sabotage });
        _monitor.Log($"[SabotageEngine] Registered raid event: {sabotage.Name}", LogLevel.Debug);
    }

    public void RegisterBitEvent(ISabotage sabotage, BitTier tier)
    {
        var def  = new SabotageDefinition { Sabotage = sabotage };
        var pool = tier switch
        {
            BitTier.Small  => _smallBitPool,
            BitTier.Medium => _mediumBitPool,
            BitTier.Large  => _largeBitPool,
            _              => _smallBitPool
        };
        pool.Add(def);
        _monitor.Log($"[SabotageEngine] Registered bit event ({tier}): {sabotage.Name}", LogLevel.Debug);
    }

    // ─── Buy Flow ─────────────────────────────────────────────────────────────

    public BuyResult TryBuy(string username, string buyCommand)
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

        var balance      = _ledger.GetPoints(username);
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
        var validationError = def.Sabotage.Validate();
        if (validationError != null)
        {
            _ledger.AddPoints(username, effectiveCost);
            return new BuyResult
            {
                Status      = BuyStatus.Rejected,
                Description = validationError
            };
        }

        def.Fire(username);

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

    public void TriggerRaidEvent(string raidLeader, int viewerCount, Action<string> sendChatMessage)
    {
        ModEntry.PendingActions.Enqueue(() =>
            RaidEvents.Execute(raidLeader, viewerCount, sendChatMessage));
    }

    public void TriggerBitEvent(string username, BitTier tier)
    {
        ModEntry.PendingActions.Enqueue(() => FireBitEvent(username, tier));
    }

    private void FireBitEvent(string username, BitTier tier)
    {
        var pool = tier switch
        {
            BitTier.Small  => _smallBitPool,
            BitTier.Medium => _mediumBitPool,
            BitTier.Large  => _largeBitPool,
            _              => _smallBitPool
        };

        if (pool.Count == 0)
        {
            _monitor.Log($"[SabotageEngine] Bit pool ({tier}) is empty.", LogLevel.Warn);
            return;
        }

        var def = pool[_rng.Next(pool.Count)];
        def.Fire(username);

        _overlay?.PushFeedEvent(username, def.Name, def.Description, 0, "bits");
        _overlay?.PushShopUpdate();

        _monitor.Log($"[SabotageEngine] Bit event ({tier}): {def.Name} for {username}", LogLevel.Info);

        Game1.addHUDMessage(new HUDMessage(
            $"💰 {username}'s bits triggered: {def.Description}!",
            HUDMessage.error_type));
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

public enum BuyStatus { Success, NotFound, InsufficientFunds, OnCooldown, GameNotActive, Rejected }

public class BuyResult
{
    public BuyStatus Status { get; init; }
    public int Cost { get; init; }
    public int Balance { get; init; }
    public string Description { get; init; } = "";
    public int CooldownRemaining { get; init; }
}