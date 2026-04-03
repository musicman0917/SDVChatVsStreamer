using StardewModdingAPI;
using SDVChatVsStreamer.Sabotage;

namespace SDVChatVsStreamer.Economy;

public class PointsEngine
{
    private readonly ViewerLedger _ledger;
    private readonly IMonitor _monitor;
    private readonly ModConfig _config;
    private RaidEventSystem? _raidEvents;

    public void SetRaidEventSystem(RaidEventSystem raidEvents) => _raidEvents = raidEvents;

    private DateTime _lastPassiveTick = DateTime.UtcNow;
    private readonly Dictionary<string, DateTime> _lastChatBonus = new();

    private Dictionary<SubTier, float> GetMultipliers() => new()
    {
        { SubTier.None,  _config.MultiplierNone  },
        { SubTier.Prime, _config.MultiplierPrime },
        { SubTier.T1,    _config.MultiplierT1    },
        { SubTier.T2,    _config.MultiplierT2    },
        { SubTier.T3,    _config.MultiplierT3    }
    };

    public PointsEngine(ViewerLedger ledger, IMonitor monitor, ModConfig config)
    {
        _ledger  = ledger;
        _monitor = monitor;
        _config  = config;
    }

    // ─── Passive Tick ─────────────────────────────────────────────────────────

    public void Update(List<string> activeViewers)
    {
        if ((DateTime.UtcNow - _lastPassiveTick).TotalMinutes < _config.PassiveTickMinutes)
            return;

        _lastPassiveTick = DateTime.UtcNow;
        _monitor.Log("[PointsEngine] Passive tick firing...", LogLevel.Debug);

        var multipliers = GetMultipliers();

        foreach (var username in activeViewers)
        {
            _ledger.EnsureViewer(username);
            var viewer = _ledger.GetViewer(username);
            if (viewer == null) continue;

            var multiplier = multipliers[viewer.SubTier];
            var points     = (int)Math.Floor(_config.BasePassivePoints * multiplier);
            if (_raidEvents != null) points = _raidEvents.ApplyPointsModifier(points);
            _ledger.AddPoints(username, points);

            _monitor.Log($"[PointsEngine] +{points}pts (x{multiplier}) → {username}", LogLevel.Trace);
        }
    }

    // ─── Chat Bonus ───────────────────────────────────────────────────────────

    public void OnChat(string username)
    {
        _ledger.EnsureViewer(username);
        _ledger.UpdateLastChat(username);

        if (_lastChatBonus.TryGetValue(username, out var lastBonus))
            if ((DateTime.UtcNow - lastBonus).TotalSeconds < _config.ChatBonusCooldownSeconds)
                return;

        _lastChatBonus[username] = DateTime.UtcNow;
        var chatPoints = _raidEvents != null
            ? _raidEvents.ApplyPointsModifier(_config.ChatBonusPoints)
            : _config.ChatBonusPoints;
        _ledger.AddPoints(username, chatPoints);
        _monitor.Log($"[PointsEngine] +{chatPoints}pts (chat bonus) → {username}", LogLevel.Trace);
    }

    // ─── Event Bonuses ────────────────────────────────────────────────────────

    public void OnFollow(string username)
    {
        _ledger.EnsureViewer(username);
        _ledger.AddPoints(username, _config.FollowBonus);
        _monitor.Log($"[PointsEngine] +{_config.FollowBonus}pts (follow) → {username}", LogLevel.Info);
    }

    public void OnSub(string username, SubTier tier)
    {
        _ledger.EnsureViewer(username);
        _ledger.SetSubTier(username, tier);
        _ledger.AddPoints(username, _config.SubBonus);
        _monitor.Log($"[PointsEngine] +{_config.SubBonus}pts (sub) + tier {tier} → {username}", LogLevel.Info);
    }

    public void OnGiftSub(string gifterUsername, int giftCount)
    {
        _ledger.EnsureViewer(gifterUsername);
        var total = _config.GiftSubBonusEach * giftCount;
        _ledger.AddPoints(gifterUsername, total);
        _monitor.Log($"[PointsEngine] +{total}pts (gifted {giftCount}) → {gifterUsername}", LogLevel.Info);
    }

    public void OnRaid(string raidLeader, int viewerCount, List<string> raiders)
    {
        _ledger.EnsureViewer(raidLeader);
        var leaderBonus = _config.RaidLeaderPointsPerViewer * viewerCount;
        _ledger.AddPoints(raidLeader, leaderBonus);
        _monitor.Log($"[PointsEngine] +{leaderBonus}pts (raid leader) → {raidLeader}", LogLevel.Info);

        foreach (var raider in raiders)
        {
            _ledger.EnsureViewer(raider);
            _ledger.AddPoints(raider, _config.RaidViewerBonus);
        }
    }

    public void OnBits(string username, int bitCount)
    {
        _ledger.EnsureViewer(username);
        var points = bitCount * _config.BitsPerPoint;
        _ledger.AddPoints(username, points);
        _monitor.Log($"[PointsEngine] +{points}pts ({bitCount} bits) → {username}", LogLevel.Info);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    public int GetBalance(string username) => _ledger.GetPoints(username);

    public float GetMultiplier(string username)
    {
        var viewer = _ledger.GetViewer(username);
        if (viewer == null) return 1.0f;
        return GetMultipliers()[viewer.SubTier];
    }
}