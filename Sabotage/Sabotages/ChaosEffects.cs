using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;
using System.Linq;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

// ─── INFESTATION — spawn monster swarm anywhere including indoors ─────────────

public class InfestationSabotage : ISabotage
{
    public string Name         => "Infestation";
    public string BuyCommand   => "infestation";
    public string Description  => "spawns a monster swarm wherever you are";
    public int Cost            => 400;
    public int CooldownSeconds => 180;

    private static readonly Random _rng = new();

    // Pull from existing monster pool — works indoors too
    private static readonly Func<Vector2, GameLocation, Monster>[] MonsterFactory = {
        (pos, loc) => new GreenSlime(pos, loc.GetSeasonIndex()),
        (pos, loc) => new Bat(pos, -555),   // Frost bat
        (pos, loc) => new Serpent(pos),
        (pos, loc) => new ShadowBrute(pos),
        (pos, loc) => new Ghost(pos),
        (pos, loc) => new DustSpirit(pos, false),
        (pos, loc) => new SquidKid(pos),
    };

    public void Execute(string triggeredBy)
    {
        var target = MultiplayerTargeting.Resolve(triggeredBy);
        var loc    = target.currentLocation;
        var origin = new Vector2(target.TilePoint.X, target.TilePoint.Y);
        var tiles  = MonsterSpawnHelper.FindSpawnTiles(loc, origin, 8, maxRadius: 10);
        var factory = MonsterFactory[_rng.Next(MonsterFactory.Length)];

        foreach (var pos in tiles)
            loc.characters.Add(factory(pos, loc));

        MultiplayerTargeting.Notify(target,
            $"🐛 {triggeredBy} triggered an Infestation! Monsters everywhere!",
            HUDMessage.error_type);
    }
}

// ─── BLINDFOLD — darken screen for 30 seconds ────────────────────────────────

public class BlindfoldSabotage : ISabotage, ILocallyRelayable
{
    public string Name         => "Blindfold";
    public string BuyCommand   => "blindfold";
    public string Description  => "darkens your screen for 30 seconds";
    public int Cost            => 250;
    public int CooldownSeconds => 180;

    public static bool IsActive       { get; private set; }
    public static DateTime ExpiresAt  { get; private set; }

    public void Execute(string triggeredBy)
    {
        var target = MultiplayerTargeting.Resolve(triggeredBy);
        if (target != Game1.player && PeerRelay.TryRelay(target, BuyCommand, triggeredBy))
        {
            MultiplayerTargeting.Notify(target,
                $"🙈 {triggeredBy} used Blindfold! Good luck seeing anything!",
                HUDMessage.error_type);
            return;
        }
        ExecuteLocal(triggeredBy);
    }

    public void ExecuteLocal(string triggeredBy)
    {
        IsActive  = true;
        ExpiresAt = DateTime.UtcNow.AddSeconds(30);

        Game1.addHUDMessage(new HUDMessage(
            $"🙈 {triggeredBy} used Blindfold! Good luck seeing anything!",
            HUDMessage.error_type));
    }

    public static void Tick()
    {
        if (IsActive && DateTime.UtcNow >= ExpiresAt)
            IsActive = false;
    }

    // Called from RenderedWorld event — draws a dark overlay
    public static void Draw(SpriteBatch sb)
    {
        if (!IsActive) return;
        var rect = new Microsoft.Xna.Framework.Rectangle(0, 0,
            Game1.graphics.GraphicsDevice.Viewport.Width,
            Game1.graphics.GraphicsDevice.Viewport.Height);
        sb.Draw(Game1.fadeToBlackRect, rect, Color.Black * 0.85f);

        // Show countdown in center of screen
        int secsLeft = Math.Max(0, (int)(ExpiresAt - DateTime.UtcNow).TotalSeconds);
        var text     = $"Blindfold: {secsLeft}s";
        var font     = Game1.dialogueFont;
        var size     = font.MeasureString(text);
        var pos      = new Vector2(
            (Game1.graphics.GraphicsDevice.Viewport.Width  - size.X) / 2,
            (Game1.graphics.GraphicsDevice.Viewport.Height - size.Y) / 2);
        sb.DrawString(font, text, pos + new Vector2(2, 2), Color.Black);
        sb.DrawString(font, text, pos, Color.White);
    }
}

// ─── CONFUSED — inverts WASD controls for 60 seconds ─────────────────────────

public class ConfusedSabotage : ISabotage, ILocallyRelayable
{
    public string Name         => "Confused";
    public string BuyCommand   => "confused";
    public string Description  => "inverts your movement controls for 60 seconds";
    public int Cost            => 300;
    public int CooldownSeconds => 180;

    public static bool IsActive      { get; private set; }
    public static DateTime ExpiresAt { get; private set; }

    public void Execute(string triggeredBy)
    {
        var target = MultiplayerTargeting.Resolve(triggeredBy);
        if (target != Game1.player && PeerRelay.TryRelay(target, BuyCommand, triggeredBy))
        {
            MultiplayerTargeting.Notify(target,
                $"🌀 {triggeredBy} used Confused! Controls inverted for 60 seconds!",
                HUDMessage.error_type);
            return;
        }
        ExecuteLocal(triggeredBy);
    }

    public void ExecuteLocal(string triggeredBy)
    {
        IsActive  = true;
        ExpiresAt = DateTime.UtcNow.AddSeconds(60);

        Game1.addHUDMessage(new HUDMessage(
            $"🌀 {triggeredBy} used Confused! Controls inverted for 60 seconds!",
            HUDMessage.error_type));
    }

    public static void Tick()
    {
        if (IsActive && DateTime.UtcNow >= ExpiresAt)
            IsActive = false;
    }

    public static int SecsLeft =>
        IsActive ? Math.Max(0, (int)(ExpiresAt - DateTime.UtcNow).TotalSeconds) : 0;
}

// ─── MASHED — movement controls fight each other ─────────────────────────────

public class MashedSabotage : ISabotage, ILocallyRelayable
{
    public string Name         => "Mashed";
    public string BuyCommand   => "mashed";
    public string Description  => "your movement controls fight each other for 60 seconds";
    public int Cost            => 250;
    public int CooldownSeconds => 180;

    public static bool IsActive      { get; private set; }
    public static DateTime ExpiresAt { get; private set; }

    private static readonly Random _rng = new();
    private static readonly int[] Directions = { 0, 1, 2, 3 };

    public void Execute(string triggeredBy)
    {
        var target = MultiplayerTargeting.Resolve(triggeredBy);
        if (target != Game1.player && PeerRelay.TryRelay(target, BuyCommand, triggeredBy))
        {
            MultiplayerTargeting.Notify(target,
                $"🎮 {triggeredBy} mashed your controls! Good luck moving!",
                HUDMessage.error_type);
            return;
        }
        ExecuteLocal(triggeredBy);
    }

    public void ExecuteLocal(string triggeredBy)
    {
        IsActive  = true;
        ExpiresAt = DateTime.UtcNow.AddSeconds(60);
        Game1.addHUDMessage(new HUDMessage(
            $"🎮 {triggeredBy} mashed your controls! Good luck moving!",
            HUDMessage.error_type));
    }

    public static void Tick()
    {
        if (!IsActive) return;
        if (DateTime.UtcNow >= ExpiresAt) { IsActive = false; return; }
        if (_rng.Next(3) == 0)
            Game1.player.movementDirections.Add(Directions[_rng.Next(4)]);
    }

    public static int SecsLeft =>
        IsActive ? Math.Max(0, (int)(ExpiresAt - DateTime.UtcNow).TotalSeconds) : 0;
}

// ─── FREEZE TIME — freezes the clock for 2 minutes ───────────────────────────

public class FreezeTimeSabotage : ISabotage
{
    public string Name         => "Freeze Time";
    public string BuyCommand   => "freezetime";
    public string Description  => "freezes the clock for 2 minutes";
    public int Cost            => 200;
    public int CooldownSeconds => 240;

    public static bool IsActive      { get; private set; }
    public static DateTime ExpiresAt { get; private set; }
    private static int _frozenTime   = 600;

    public void Execute(string triggeredBy)
    {
        IsActive    = true;
        ExpiresAt   = DateTime.UtcNow.AddMinutes(2);
        _frozenTime = Game1.timeOfDay;

        Game1.addHUDMessage(new HUDMessage(
            $"❄️ {triggeredBy} froze time! Clock is stopped for 2 minutes!",
            HUDMessage.error_type));
    }

    public static void Tick()
    {
        if (!IsActive) return;
        if (DateTime.UtcNow >= ExpiresAt)
        {
            IsActive = false;
            return;
        }
        // Keep resetting timeOfDay to the frozen value
        Game1.timeOfDay = _frozenTime;
    }

    public static int SecsLeft =>
        IsActive ? Math.Max(0, (int)(ExpiresAt - DateTime.UtcNow).TotalSeconds) : 0;
}

// ─── WARP WHISTLE — 5% chance to warp to random building on entry ────────────

public class WarpWhistleSabotage : ISabotage, ILocallyRelayable
{
    public string Name         => "Warp Whistle";
    public string BuyCommand   => "warpwhistle";
    public string Description  => "5% chance to warp to a random building every time you enter one (5 min)";
    public int Cost            => 250;
    public int CooldownSeconds => 180;

    public void Execute(string triggeredBy)
    {
        var target = MultiplayerTargeting.Resolve(triggeredBy);
        if (target != Game1.player && PeerRelay.TryRelay(target, BuyCommand, triggeredBy))
        {
            MultiplayerTargeting.Notify(target,
                $"🎺 {triggeredBy} used Warp Whistle! Every door is a gamble for 5 minutes!",
                HUDMessage.error_type);
            return;
        }
        ExecuteLocal(triggeredBy);
    }

    public void ExecuteLocal(string triggeredBy)
    {
        WarpWhistleState.Activate(triggeredBy, DateTime.UtcNow.AddMinutes(5), 0.05);
        Game1.addHUDMessage(new HUDMessage(
            $"🎺 {triggeredBy} used Warp Whistle! Every door is a gamble for 5 minutes!",
            HUDMessage.error_type));
    }
}

public class WarpWhistlePlusSabotage : ISabotage, ILocallyRelayable
{
    public string Name         => "Warp Whistle+";
    public string BuyCommand   => "warpwhistleplus";
    public string Description  => "50% chance to warp to a random building every time you enter one (all day)";
    public int Cost            => 600;
    public int CooldownSeconds => 600;

    public void Execute(string triggeredBy)
    {
        var target = MultiplayerTargeting.Resolve(triggeredBy);
        if (target != Game1.player && PeerRelay.TryRelay(target, BuyCommand, triggeredBy))
        {
            MultiplayerTargeting.Notify(target,
                $"🎺 {triggeredBy} used Warp Whistle+! 50% chance on every door for the rest of the day!",
                HUDMessage.error_type);
            return;
        }
        ExecuteLocal(triggeredBy);
    }

    public void ExecuteLocal(string triggeredBy)
    {
        WarpWhistleState.Activate(triggeredBy, DateTime.UtcNow.AddHours(20), 0.50);
        Game1.addHUDMessage(new HUDMessage(
            $"🎺 {triggeredBy} used Warp Whistle+! 50% chance on every door for the rest of the day!",
            HUDMessage.error_type));
    }
}

public class WarpWhistleMaxSabotage : ISabotage, ILocallyRelayable
{
    public string Name         => "Warp Whistle MAX";
    public string BuyCommand   => "warpwhistlemax";
    public string Description  => "99.5% chance to warp to a random building every time you enter one (15 min)";
    public int Cost            => 2000;
    public int CooldownSeconds => 600;

    public void Execute(string triggeredBy)
    {
        var target = MultiplayerTargeting.Resolve(triggeredBy);
        if (target != Game1.player && PeerRelay.TryRelay(target, BuyCommand, triggeredBy))
        {
            MultiplayerTargeting.Notify(target,
                $"🎺 {triggeredBy} used Warp Whistle MAX! Every door is a trap for 15 minutes!",
                HUDMessage.error_type);
            return;
        }
        ExecuteLocal(triggeredBy);
    }

    public void ExecuteLocal(string triggeredBy)
    {
        WarpWhistleState.Activate(triggeredBy, DateTime.UtcNow.AddMinutes(15), 0.995);
        Game1.addHUDMessage(new HUDMessage(
            $"🎺 {triggeredBy} used Warp Whistle MAX! Every door is a trap for 15 minutes!",
            HUDMessage.error_type));
    }
}

public class WarpWhistleMaxPlusSabotage : ISabotage, ILocallyRelayable
{
    public string Name         => "Warp Whistle MAX+";
    public string BuyCommand   => "warpwhistlemaxplus";
    public string Description  => "99.5% chance to warp to a random building every time you enter one (all day)";
    public int Cost            => 5000;
    public int CooldownSeconds => 900;

    public void Execute(string triggeredBy)
    {
        var target = MultiplayerTargeting.Resolve(triggeredBy);
        if (target != Game1.player && PeerRelay.TryRelay(target, BuyCommand, triggeredBy))
        {
            MultiplayerTargeting.Notify(target,
                $"🎺 {triggeredBy} used Warp Whistle MAX+! Every door is a trap for the rest of the day. Good luck.",
                HUDMessage.error_type);
            return;
        }
        ExecuteLocal(triggeredBy);
    }

    public void ExecuteLocal(string triggeredBy)
    {
        WarpWhistleState.Activate(triggeredBy, DateTime.UtcNow.AddHours(20), 0.995);
        Game1.addHUDMessage(new HUDMessage(
            $"🎺 {triggeredBy} used Warp Whistle MAX+! Every door is a trap for the rest of the day. Good luck.",
            HUDMessage.error_type));
    }
}

// ─── Shared state for both Warp Whistle variants ─────────────────────────────

public static class WarpWhistleState
{
    public static bool IsActive      { get; private set; }
    public static DateTime ExpiresAt { get; private set; }
    public static string TriggeredBy { get; private set; } = "";
    public static double Chance      { get; private set; } = 0.05;

    private static readonly Random _rng = new();

    // Every destination in Buildings is itself indoors, so landing on one re-satisfies
    // OnWarped's "!loc.IsOutdoors" check and would immediately re-roll on the very next
    // warp event -- at the high-probability tiers that's a near-certain runaway chain
    // bouncing the player between buildings every frame with no chance to regain
    // control. This grace period stops a whistle-triggered warp from re-triggering
    // itself while still re-rolling normally on the player's next real building entry.
    private static readonly TimeSpan RetriggerGracePeriod = TimeSpan.FromSeconds(2);
    private static DateTime _lastWhistleWarp = DateTime.MinValue;

    private static readonly (string Location, int X, int Y)[] Buildings = {
        ("SeedShop",        4,  20),
        ("Saloon",          11, 20),
        ("Hospital",        6,  14),
        ("Blacksmith",      4,  14),
        ("Museum",          4,  9),
        ("FishShop",        5,  9),
        ("AnimalShop",      12, 16),
        ("WizardHouse",     5,  10),
        ("ManorHouse",      9,  7),
        ("ArchaeologyHouse",4,  9),
        ("ScienceHouse",    8,  10),
        ("AdventureGuild",  5,  9),
        ("FarmHouse",       6,  4),
        ("Greenhouse",      3,  6),
    };

    public static void Activate(string triggeredBy, DateTime expiresAt, double chance = 0.05)
    {
        IsActive    = true;
        ExpiresAt   = expiresAt;
        TriggeredBy = triggeredBy;
        Chance      = chance;
        ModEntry.Logger?.Log($"[WarpWhistleState] Activated by {triggeredBy}, chance={chance:P0}, expires={expiresAt}", LogLevel.Info);
    }

    public static void OnDayStarted()
    {
        if (IsActive && ExpiresAt < DateTime.UtcNow.AddHours(10))
            IsActive = false;
        // WarpWhistlePlus lasts all day so we don't deactivate on day start
    }

    public static void Tick()
    {
        if (IsActive && DateTime.UtcNow >= ExpiresAt)
        {
            IsActive = false;
            Game1.addHUDMessage(new HUDMessage(
                "🎺 The Warp Whistle has stopped. Doors are safe again. Probably.",
                HUDMessage.newQuest_type));
        }
    }

    public static void OnWarped(string newLocation)
    {
        if (!IsActive) return;
        if (DateTime.UtcNow >= ExpiresAt) { IsActive = false; return; }

        ModEntry.Logger?.Log($"[WarpWhistleState] OnWarped fired. New location: {newLocation}", LogLevel.Debug);

        // Only trigger when entering a building (not outdoors)
        var loc = Game1.getLocationFromName(newLocation);
        if (loc == null || loc.IsOutdoors)
        {
            ModEntry.Logger?.Log($"[WarpWhistleState] Skipped — location is null or outdoors (IsOutdoors={loc?.IsOutdoors}).", LogLevel.Debug);
            return;
        }

        if (DateTime.UtcNow - _lastWhistleWarp < RetriggerGracePeriod)
        {
            ModEntry.Logger?.Log("[WarpWhistleState] Skipped — inside the post-warp grace period.", LogLevel.Debug);
            return;
        }

        // Roll based on current chance
        double roll = _rng.NextDouble();
        ModEntry.Logger?.Log($"[WarpWhistleState] Roll: {roll:P1} vs chance {Chance:P1}", LogLevel.Debug);
        if (roll > Chance) return;

        // Pick a random different building, trying each until one safely warps
        var options = Buildings
            .Where(b => !b.Location.Equals(newLocation, StringComparison.OrdinalIgnoreCase))
            .OrderBy(_ => _rng.Next())
            .ToList();

        foreach (var dest in options)
        {
            ModEntry.Logger?.Log($"[WarpWhistleState] Trying warp to {dest.Location} ({dest.X},{dest.Y})", LogLevel.Info);
            if (SDVChatVsStreamer.Sabotage.WarpHelper.SafeWarpToRandomSpot(dest.Location, dest.X, dest.Y, _rng))
            {
                ModEntry.Logger?.Log($"[WarpWhistleState] Successfully warped to {dest.Location}.", LogLevel.Info);
                _lastWhistleWarp = DateTime.UtcNow;
                Game1.addHUDMessage(new HUDMessage(
                    $"🎺 The Warp Whistle activated! You ended up in {dest.Location}!",
                    HUDMessage.error_type));
                Game1.playSound("wand");
                return;
            }
        }

        ModEntry.Logger?.Log("[WarpWhistleState] All building destinations failed SafeWarp.", LogLevel.Warn);
        Game1.addHUDMessage(new HUDMessage(
            "🎺 The Warp Whistle sparked, but nowhere safe was found!",
            HUDMessage.error_type));
    }

    public static int SecsLeft =>
        IsActive ? Math.Max(0, (int)(ExpiresAt - DateTime.UtcNow).TotalSeconds) : 0;

    public static void Draw(SpriteBatch sb, ref float y)
    {
        if (!IsActive) return;
        var chance = (int)(Chance * 100);
        var text = SecsLeft > 3600
            ? $"Warp Whistle ({chance}%): All day"
            : $"Warp Whistle ({chance}%): {SecsLeft}s";
        var font = Game1.smallFont;
        var pos  = new Vector2(16, y);
        sb.DrawString(font, text, pos + new Vector2(2, 2), Color.Black);
        sb.DrawString(font, text, pos, Color.Gold);
        y += 32f;
    }
}