using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace SDVChatVsStreamer.Sabotage;

public enum RaidTier { Small, Medium, Large, Massive }
public enum RaidEventType { Chaos, Blessing, Meta }

public class RaidEventSystem
{
    private readonly IMonitor _monitor;
    private readonly Random _rng = new();

    // Active meta effects
    public DateTime _doublePointsExpiry   = DateTime.MinValue;
    public DateTime _halveCostsExpiry     = DateTime.MinValue;

    public bool IsDoublePoints => DateTime.UtcNow < _doublePointsExpiry;
    public bool IsHalvedCosts  => DateTime.UtcNow < _halveCostsExpiry;

    public RaidEventSystem(IMonitor monitor)
    {
        _monitor = monitor;
    }

    // ─── Public Entry Point ───────────────────────────────────────────────────

    public RaidEventResult Execute(string raidLeader, int viewerCount, Action<string> sendChatMessage)
    {
        var tier = GetTier(viewerCount);
        var type = RollEventType(tier);

        _monitor.Log($"[RaidEvent] {raidLeader} raid — {viewerCount} viewers, tier={tier}, type={type}", LogLevel.Info);

        RaidEventResult result;

        switch (type)
        {
            case RaidEventType.Chaos:
                result = ExecuteChaos(raidLeader, tier);
                break;
            case RaidEventType.Blessing:
                result = ExecuteBlessing(raidLeader, tier);
                break;
            case RaidEventType.Meta:
            default:
                result = ExecuteMeta(raidLeader, tier, viewerCount);
                break;
        }

        // Announce both in chat and HUD
        var hudColor = type == RaidEventType.Chaos ? HUDMessage.error_type : HUDMessage.newQuest_type;
        Game1.addHUDMessage(new HUDMessage(result.HudMessage, hudColor));
        sendChatMessage(result.ChatMessage);

        return result;
    }

    // ─── Tier / Type Logic ────────────────────────────────────────────────────

    private static RaidTier GetTier(int viewerCount) => viewerCount switch
    {
        < 10  => RaidTier.Small,
        < 50  => RaidTier.Medium,
        < 100 => RaidTier.Large,
        _     => RaidTier.Massive
    };

    private RaidEventType RollEventType(RaidTier tier)
    {
        // Chaos / Blessing / Meta weights by tier
        var (chaos, blessing, meta) = tier switch
        {
            RaidTier.Small   => (70, 15, 15),
            RaidTier.Medium  => (60, 20, 20),
            RaidTier.Large   => (50, 25, 25),
            RaidTier.Massive => (40, 30, 30),
            _                => (60, 20, 20)
        };

        int roll = _rng.Next(100);
        if (roll < chaos)   return RaidEventType.Chaos;
        if (roll < chaos + blessing) return RaidEventType.Blessing;
        return RaidEventType.Meta;
    }

    // ─── Chaos ────────────────────────────────────────────────────────────────

    private RaidEventResult ExecuteChaos(string raidLeader, RaidTier tier)
    {
        // Pick sabotages based on tier
        var pool = tier switch
        {
            RaidTier.Small   => new[] { "drain energy", "rain", "make dizzy", "trash" },
            RaidTier.Medium  => new[] { "spawn slime", "kill crops", "warp player", "storm" },
            RaidTier.Large   => new[] { "spawn swarm", "bomb", "force sleep", "kill farm" },
            RaidTier.Massive => new[] { "spawn swarm", "kill farm", "bomb", "force sleep" },
            _                => new[] { "rain" }
        };

        var events = new List<string>();

        // Massive raids fire TWO chaos events
        int count = tier == RaidTier.Massive ? 2 : 1;
        var shuffled = pool.OrderBy(_ => _rng.Next()).Take(count).ToList();

        foreach (var eventName in shuffled)
        {
            var msg = FireNamedChaos(raidLeader, eventName);
            events.Add(msg);
        }

        var desc = string.Join(" + ", events);
        return new RaidEventResult
        {
            Type       = RaidEventType.Chaos,
            HudMessage = $"🚨 Raid chaos! {raidLeader}'s raid triggered: {desc}!",
            ChatMessage = $"🚨 RAID CHAOS from {raidLeader}! {desc}!"
        };
    }

    private string FireNamedChaos(string triggeredBy, string name)
    {
        switch (name)
        {
            case "drain energy":
                Game1.player.Stamina = Math.Max(0, Game1.player.Stamina - 50f);
                return "drained your energy";

            case "rain":
                Game1.weatherForTomorrow = "Rain";
                return "called the rain";

            case "make dizzy":
                Game1.player.buffs.Apply(new Buff(
                    id: "CVS_RaidTipsy", source: "Chat vs Streamer",
                    displaySource: triggeredBy, duration: 30000,
                    effects: new StardewValley.Buffs.BuffEffects() { Speed = { -1 } },
                    displayName: "Raid Dizzy", description: "Raid made you dizzy!"));
                return "made you dizzy";

            case "trash":
                var emptySlots = Enumerable.Range(0, Game1.player.Items.Count)
                    .Where(i => Game1.player.Items[i] == null).ToList();
                if (emptySlots.Count > 0)
                {
                    Game1.player.Items[emptySlots[_rng.Next(emptySlots.Count)]] =
                        new StardewValley.Object("168", 1);
                    return "filled a slot with trash";
                }
                return "tried to add trash but you're full";

            case "spawn slime":
                var slimePos = new Vector2(Game1.player.TilePoint.X + 2, Game1.player.TilePoint.Y) * 64f;
                Game1.player.currentLocation.characters.Add(
                    new StardewValley.Monsters.GreenSlime(slimePos, Game1.player.currentLocation.GetSeasonIndex()));
                return "sent a slime";

            case "kill crops":
                var farm1 = Game1.getFarm();
                var crops1 = farm1.terrainFeatures.Values.OfType<HoeDirt>()
                    .Where(d => d.crop != null && !d.crop.dead.Value).ToList();
                if (crops1.Count > 0) crops1[_rng.Next(crops1.Count)].crop.Kill();
                return "sent crows";

            case "warp player":
                var dests = new[] { ("Town", 50, 80), ("Beach", 30, 5), ("Mountain", 30, 20) };
                var dest  = dests[_rng.Next(dests.Length)];
                Game1.warpFarmer(dest.Item1, dest.Item2, dest.Item3, false);
                return "warped you";

            case "storm":
                Game1.weatherForTomorrow = "Storm";
                return "called a storm";

            case "spawn swarm":
                var season = Game1.player.currentLocation.GetSeasonIndex();
                for (int i = 0; i < 5; i++)
                {
                    var pos = new Vector2(
                        Game1.player.TilePoint.X + _rng.Next(-3, 4),
                        Game1.player.TilePoint.Y + _rng.Next(-3, 4)) * 64f;
                    Game1.player.currentLocation.characters.Add(
                        new StardewValley.Monsters.GreenSlime(pos, season));
                }
                return "sent a slime swarm";

            case "bomb":
                Game1.player.currentLocation.explode(
                    new Vector2(Game1.player.TilePoint.X, Game1.player.TilePoint.Y), 3, Game1.player);
                return "dropped a bomb";

            case "force sleep":
                Game1.timeOfDay = 2600;
                return "forced 2am";

            case "kill farm":
                var farm2 = Game1.getFarm();
                var crops2 = farm2.terrainFeatures.Values.OfType<HoeDirt>()
                    .Where(d => d.crop != null && !d.crop.dead.Value).ToList();
                foreach (var c in crops2) c.crop.Kill();
                return $"killed all {crops2.Count} crops";

            default:
                return "caused chaos";
        }
    }

    // ─── Blessing ─────────────────────────────────────────────────────────────

    private RaidEventResult ExecuteBlessing(string raidLeader, RaidTier tier)
    {
        var blessings = new List<Action<List<string>>>
        {
            msgs => {
                Game1.player.Stamina = Game1.player.MaxStamina;
                msgs.Add("full energy restore ⚡");
            },
            msgs => {
                int gold = tier switch
                {
                    RaidTier.Small   => 500,
                    RaidTier.Medium  => 1000,
                    RaidTier.Large   => 2500,
                    RaidTier.Massive => 5000,
                    _ => 500
                };
                Game1.player.Money += gold;
                msgs.Add($"+{gold}g gold bonus 💰");
            },
            msgs => {
                var farm = Game1.getFarm();
                int watered = 0;
                foreach (var tf in farm.terrainFeatures.Values.OfType<HoeDirt>())
                {
                    if (tf.crop != null) { tf.state.Value = HoeDirt.watered; watered++; }
                }
                msgs.Add($"watered {watered} crops 💧");
            }
        };

        // Pick 1 random blessing
        var messages = new List<string>();
        blessings[_rng.Next(blessings.Count)](messages);
        var desc = string.Join(", ", messages);

        return new RaidEventResult
        {
            Type        = RaidEventType.Blessing,
            HudMessage  = $"✨ Raid blessing from {raidLeader}! {desc}!",
            ChatMessage = $"✨ RAID BLESSING from {raidLeader}! {desc}!"
        };
    }

    // ─── Meta ─────────────────────────────────────────────────────────────────

    public RaidEventResult ExecuteMeta(string raidLeader, RaidTier tier, int viewerCount)
    {
        var metaEvents = new List<(string Desc, Action Execute)>
        {
            ("double points for 10 min 🌟", () => {
                _doublePointsExpiry = DateTime.UtcNow.AddMinutes(10);
            }),
            ("sabotage costs halved for 5 min 🔽", () => {
                _halveCostsExpiry = DateTime.UtcNow.AddMinutes(5);
            }),
            ("raiders get bonus points 🎁", () => {
                // Bonus points awarded per raider when they chat — handled in PointsEngine via config
                // We store the bonus amount for TwitchManager to use
                RaiderBonusPoints = tier switch
                {
                    RaidTier.Small   => 25,
                    RaidTier.Medium  => 50,
                    RaidTier.Large   => 100,
                    RaidTier.Massive => 200,
                    _ => 25
                };
                RaiderBonusExpiry = DateTime.UtcNow.AddMinutes(10);
            }),
        };

        var chosen = metaEvents[_rng.Next(metaEvents.Count)];
        chosen.Execute();

        return new RaidEventResult
        {
            Type        = RaidEventType.Meta,
            HudMessage  = $"🌟 Raid meta event from {raidLeader}! {chosen.Desc}!",
            ChatMessage = $"🌟 RAID META from {raidLeader}! {chosen.Desc}!"
        };
    }

    // ─── Meta State (public for TwitchManager) ───────────────────────────────

    public int RaiderBonusPoints { get; private set; } = 0;
    public DateTime RaiderBonusExpiry { get; private set; } = DateTime.MinValue;
    public bool IsRaiderBonusActive => DateTime.UtcNow < RaiderBonusExpiry;

    /// <summary>Apply multiplier to a point cost if costs are halved.</summary>
    public int ApplyCostModifier(int cost) =>
        IsHalvedCosts ? Math.Max(1, cost / 2) : cost;

    /// <summary>Apply multiplier to points earned if double points is active.</summary>
    public int ApplyPointsModifier(int points) =>
        IsDoublePoints ? points * 2 : points;
}

public class RaidEventResult
{
    public RaidEventType Type { get; init; }
    public string HudMessage { get; init; } = "";
    public string ChatMessage { get; init; } = "";
}
