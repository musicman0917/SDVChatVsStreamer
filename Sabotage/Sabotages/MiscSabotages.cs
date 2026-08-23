using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System.Linq;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

// ─── AUTO-PETTER — pets every animal on the farm instantly ────────────────────

public class AutoPetterSabotage : ISabotage
{
    public string Name         => "Auto-Petter";
    public string BuyCommand   => "autopetter";
    public string Description  => "instantly pets every farm animal";
    public int Cost            => 150;
    public int CooldownSeconds => 120;

    public void Execute(string triggeredBy)
    {
        int count = 0;
        foreach (var loc in Game1.locations)
        {
            foreach (var animal in loc.Animals.Values)
            {
                animal.pet(Game1.player);
                count++;
            }
        }

        Game1.addHUDMessage(new HUDMessage(
            $"🐾 {triggeredBy} used the Auto-Petter! {count} animals were petted!",
            HUDMessage.newQuest_type));
    }
}

// ─── CARE PACKAGE — spawns a survival kit chest at the streamer's feet ────────

public class CarePackageSabotage : ISabotage
{
    public string Name         => "Care Package";
    public string BuyCommand   => "carepackage";
    public string Description  => "drops a survival kit chest at your feet";
    public int Cost            => 200;
    public int CooldownSeconds => 180;

    public void Execute(string triggeredBy)
    {
        var loc = Game1.player.currentLocation;

        var items = new[]
        {
            StardewValley.ItemRegistry.Create("(O)286", 3), // Bomb
            StardewValley.ItemRegistry.Create("(O)196", 3), // Salad
            StardewValley.ItemRegistry.Create("(O)395", 2), // Coffee
        };

        var tile = FindEmptyTile(loc, Game1.player.Tile);
        if (tile.HasValue)
        {
            var chest = new Chest(true) { Name = "Care Package" };
            foreach (var item in items) chest.Items.Add(item);
            loc.Objects[tile.Value] = chest;
        }
        else
        {
            // No free tile nearby — drop the items on the ground instead of overwriting
            // whatever's already placed there (sprinkler, scarecrow, another chest, etc.)
            foreach (var item in items)
                Game1.createItemDebris(item, Game1.player.Position, -1, loc);
        }

        Game1.addHUDMessage(new HUDMessage(
            $"📦 {triggeredBy} sent a care package! A chest appeared nearby with some essentials.",
            HUDMessage.newQuest_type));
    }

    private static Vector2? FindEmptyTile(GameLocation loc, Vector2 playerTile)
    {
        for (int radius = 1; radius <= 3; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (Math.Abs(dx) != radius && Math.Abs(dy) != radius) continue;
                var tile = playerTile + new Vector2(dx, dy);
                if (loc.Objects.ContainsKey(tile)) continue;
                if (!loc.isTilePassable(new xTile.Dimensions.Location((int)tile.X, (int)tile.Y), Game1.viewport)) continue;
                return tile;
            }
        }
        return null;
    }
}

// ─── AIRDROP — jackpot chest at a random, unoccupied spot on the whole farm ───

public class AirdropSabotage : ISabotage
{
    public string Name         => "Airdrop";
    public string BuyCommand   => "airdrop";
    public string Description  => "drops a loot-packed chest somewhere random on the farm — good luck finding it!";
    public int Cost            => 2000;
    public int CooldownSeconds => 300;

    private readonly Random _rng = new();

    public void Execute(string triggeredBy)
    {
        var farm = Game1.getFarm();

        var items = new[]
        {
            StardewValley.ItemRegistry.Create("(O)337", 5), // Iridium Bar
            StardewValley.ItemRegistry.Create("(O)72",  2), // Diamond
            StardewValley.ItemRegistry.Create("(O)787", 5), // Battery Pack
            StardewValley.ItemRegistry.Create("(O)773", 1), // Magic Rock Candy
            StardewValley.ItemRegistry.Create("(O)446", 1), // Rabbit's Foot
        };

        Vector2? tile = null;
        for (int attempt = 0; attempt < 200; attempt++)
        {
            int x = _rng.Next(1, farm.Map.Layers[0].LayerWidth  - 1);
            int y = _rng.Next(1, farm.Map.Layers[0].LayerHeight - 1);
            var candidate = new Vector2(x, y);

            if (farm.isTileLocationOpen(new xTile.Dimensions.Location(x, y)) &&
                !farm.terrainFeatures.ContainsKey(candidate) &&
                !farm.objects.ContainsKey(candidate))
            {
                tile = candidate;
                break;
            }
        }

        if (tile.HasValue)
        {
            var chest = new Chest(true) { Name = "Airdrop" };
            foreach (var item in items) chest.Items.Add(item);
            farm.objects[tile.Value] = chest;

            Game1.addHUDMessage(new HUDMessage(
                $"🎁 {triggeredBy} called in an airdrop! A loot-packed chest landed somewhere on your farm!",
                HUDMessage.newQuest_type));
        }
        else
        {
            // Farm's too built-up to find an open tile — drop the loot at the player's feet instead
            foreach (var item in items)
                Game1.createItemDebris(item, Game1.player.Position, -1, Game1.player.currentLocation);

            Game1.addHUDMessage(new HUDMessage(
                $"🎁 {triggeredBy} called in an airdrop, but there was nowhere to land it — the loot dropped at your feet instead!",
                HUDMessage.newQuest_type));
        }
    }
}

// ─── GEODE CRACK — instantly processes all geodes in inventory ───────────────

public class GeodeCrackSabotage : ISabotage
{
    public string Name         => "Geode Crack";
    public string BuyCommand   => "geodecrack";
    public string Description  => "instantly cracks open all geodes in your inventory";
    public int Cost            => 200;
    public int CooldownSeconds => 180;

    private static readonly HashSet<string> GeodeIds = new()
    {
        "(O)535", // Geode
        "(O)536", // Frozen Geode
        "(O)537", // Magma Geode
        "(O)749", // Omni Geode
    };

    public void Execute(string triggeredBy)
    {
        int cracked = 0;
        var items = Game1.player.Items;

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item == null) continue;
            if (!GeodeIds.Contains(item.QualifiedItemId)) continue;

            int stack = item.Stack;
            for (int s = 0; s < stack; s++)
            {
                var loot = StardewValley.Utility.getTreasureFromGeode(item);
                if (loot != null)
                    Game1.player.addItemToInventoryBool(loot);
                cracked++;
            }

            items[i] = null;
        }

        Game1.addHUDMessage(new HUDMessage(
            cracked > 0
                ? $"💎 {triggeredBy} cracked {cracked} geode(s) for you! No Clint required."
                : $"💎 {triggeredBy} tried to crack your geodes, but you didn't have any!",
            HUDMessage.newQuest_type));
    }
}

// ─── HICCUPS — random jump animation every 3-8 seconds for 2 minutes ─────────

public class HiccupsSabotage : ISabotage
{
    public string Name         => "Hiccups";
    public string BuyCommand   => "hiccups";
    public string Description  => "randomly forces a jump animation every 3-8 seconds for 2 minutes";
    public int Cost            => 150;
    public int CooldownSeconds => 120;

    public static bool IsActive      { get; private set; }
    public static DateTime ExpiresAt { get; private set; }
    private static DateTime _nextHiccup = DateTime.UtcNow;
    private static readonly Random _rng = new();

    public void Execute(string triggeredBy)
    {
        IsActive     = true;
        ExpiresAt    = DateTime.UtcNow.AddMinutes(2);
        _nextHiccup  = DateTime.UtcNow.AddSeconds(_rng.Next(3, 9));

        Game1.addHUDMessage(new HUDMessage(
            $"🤭 {triggeredBy} gave you the hiccups! *hic*",
            HUDMessage.error_type));
    }

    public static void Tick()
    {
        if (!IsActive) return;
        if (DateTime.UtcNow >= ExpiresAt) { IsActive = false; return; }
        if (DateTime.UtcNow < _nextHiccup) return;

        _nextHiccup = DateTime.UtcNow.AddSeconds(_rng.Next(3, 9));
        Game1.player.jump();
        Game1.playSound("hiccup");
    }

    public static int SecsLeft =>
        IsActive ? Math.Max(0, (int)(ExpiresAt - DateTime.UtcNow).TotalSeconds) : 0;
}

// ─── PARANOIA — random scary sound effects, no actual danger ─────────────────

public class ParanoiaSabotage : ISabotage
{
    public string Name         => "Paranoia";
    public string BuyCommand   => "paranoia";
    public string Description  => "randomly plays scary sound effects (serpent hiss, bat screech, bomb fuse) for 90 seconds — nothing is actually there. No on-screen warning is shown.";
    public int Cost            => 150;
    public int CooldownSeconds => 120;

    public static bool IsActive      { get; private set; }
    public static DateTime ExpiresAt { get; private set; }
    private static DateTime _nextScare = DateTime.UtcNow;
    private static readonly Random _rng = new();
    private static readonly string[] ScarySounds = { "fuse", "batScreech", "serpentHit" };

    private static StardewValley.ICue? _activeCue;
    private static DateTime _cueStopAt = DateTime.MinValue;

    public void Execute(string triggeredBy)
    {
        IsActive   = true;
        ExpiresAt  = DateTime.UtcNow.AddSeconds(90);
        _nextScare = DateTime.UtcNow.AddSeconds(_rng.Next(5, 15));

        // Intentionally no HUD message — the whole point is the streamer
        // doesn't get a warning that this is what's happening
    }

    public static void Tick()
    {
        // Force-stop any cue that's overrun its short window, regardless of activity state
        if (_activeCue != null && DateTime.UtcNow >= _cueStopAt)
        {
            try { if (_activeCue.IsPlaying) _activeCue.Stop(Microsoft.Xna.Framework.Audio.AudioStopOptions.Immediate); }
            catch { /* cue may already be disposed */ }
            _activeCue = null;
        }

        if (!IsActive) return;
        if (DateTime.UtcNow >= ExpiresAt) { IsActive = false; return; }
        if (DateTime.UtcNow < _nextScare) return;

        _nextScare = DateTime.UtcNow.AddSeconds(_rng.Next(8, 20));
        var sound = ScarySounds[_rng.Next(ScarySounds.Length)];

        try
        {
            Game1.playSound(sound, out var cue);
            _activeCue  = cue;
            _cueStopAt  = DateTime.UtcNow.AddMilliseconds(1200); // hard cap so nothing lingers
        }
        catch { /* cue id may not exist on this game version — skip silently */ }
    }

    public static int SecsLeft =>
        IsActive ? Math.Max(0, (int)(ExpiresAt - DateTime.UtcNow).TotalSeconds) : 0;
}

// ─── HEAVY POCKETS — movement speed scales down with inventory fullness ──────

public class HeavyPocketsSabotage : ISabotage
{
    public string Name         => "Heavy Pockets";
    public string BuyCommand   => "heavypockets";
    public string Description  => "movement speed decreases the fuller your inventory is, for 2 minutes";
    public int Cost            => 200;
    public int CooldownSeconds => 150;

    public static bool IsActive      { get; private set; }
    public static DateTime ExpiresAt { get; private set; }

    private const string BuffId = "CVS_HeavyPockets";

    // The applied buff only lasts 1500ms, so it must be refreshed well before
    // that to avoid a gap — but we don't need to reapply every single tick.
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMilliseconds(1000);
    private static DateTime _nextRefresh = DateTime.MinValue;
    private static float _lastPenalty = float.NaN;

    public void Execute(string triggeredBy)
    {
        IsActive     = true;
        ExpiresAt    = DateTime.UtcNow.AddMinutes(2);
        _nextRefresh = DateTime.MinValue;
        _lastPenalty = float.NaN;

        Game1.addHUDMessage(new HUDMessage(
            $"🎒 {triggeredBy} made your pockets heavy! A full inventory will slow you down for 2 minutes.",
            HUDMessage.error_type));
    }

    public static void Tick()
    {
        if (!IsActive) return;

        if (DateTime.UtcNow >= ExpiresAt)
        {
            IsActive = false;
            Game1.player.buffs.Remove(BuffId);
            return;
        }

        int filled = Game1.player.Items.Count(i => i != null);
        int total  = Game1.player.Items.Count;
        float fullness = total > 0 ? (float)filled / total : 0f;

        // Up to -3 speed at 100% full inventory
        float speedPenalty = -(fullness * 3f);

        // Only reapply (and reallocate the Buff) when the penalty actually
        // changed, or when the short-lived buff is due to expire otherwise.
        bool penaltyChanged = Math.Abs(speedPenalty - _lastPenalty) > 0.001f;
        if (!penaltyChanged && DateTime.UtcNow < _nextRefresh) return;

        _lastPenalty = speedPenalty;
        _nextRefresh = DateTime.UtcNow + RefreshInterval;

        Game1.player.buffs.Apply(new Buff(
            id:            BuffId,
            source:        "Chat vs Streamer",
            displaySource: "Chat vs Streamer",
            duration:      1500,
            effects:       new StardewValley.Buffs.BuffEffects() { Speed = { speedPenalty } },
            displayName:   "Heavy Pockets"));
    }

    public static int SecsLeft =>
        IsActive ? Math.Max(0, (int)(ExpiresAt - DateTime.UtcNow).TotalSeconds) : 0;
}