using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;

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
    private static readonly Func<Vector2, Monster>[] MonsterFactory = {
        pos => new GreenSlime(pos, Game1.player.currentLocation.GetSeasonIndex()),
        pos => new Bat(pos, -555),   // Frost bat
        pos => new Serpent(pos),
        pos => new ShadowBrute(pos),
        pos => new Ghost(pos),
        pos => new DustSpirit(pos, false),
        pos => new SquidKid(pos),
    };

    public void Execute(string triggeredBy)
    {
        var loc    = Game1.player.currentLocation;
        var origin = new Vector2(Game1.player.TilePoint.X, Game1.player.TilePoint.Y);
        var tiles  = MonsterSpawnHelper.FindSpawnTiles(loc, origin, 8, maxRadius: 10);
        var factory = MonsterFactory[_rng.Next(MonsterFactory.Length)];

        foreach (var pos in tiles)
            loc.characters.Add(factory(pos));

        Game1.addHUDMessage(new HUDMessage(
            $"🐛 {triggeredBy} triggered an Infestation! Monsters everywhere!",
            HUDMessage.error_type));
    }
}

// ─── BLINDFOLD — darken screen for 30 seconds ────────────────────────────────

public class BlindfoldSabotage : ISabotage
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

public class ConfusedSabotage : ISabotage
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

public class MashedSabotage : ISabotage
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

// ─── SLIP N SLIDE — ice physics for 60 seconds ───────────────────────────────

public class SlipNSlideSabotage : ISabotage
{
    public string Name         => "Slip n Slide";
    public string BuyCommand   => "slipnslide";
    public string Description  => "ice physics — you can't stop easily for 60 seconds";
    public int Cost            => 250;
    public int CooldownSeconds => 180;

    public static bool IsActive      { get; private set; }
    public static DateTime ExpiresAt { get; private set; }

    // Stored momentum direction
    public static float VelocityX { get; private set; }
    public static float VelocityY { get; private set; }

    private const float Friction    = 0.92f; // how quickly momentum decays (lower = icier)
    private const float Acceleration = 0.8f; // how much input adds to velocity
    private const float MaxSpeed     = 6f;   // cap

    public void Execute(string triggeredBy)
    {
        IsActive  = true;
        ExpiresAt = DateTime.UtcNow.AddSeconds(60);
        VelocityX = 0f;
        VelocityY = 0f;

        Game1.addHUDMessage(new HUDMessage(
            $"🛝 {triggeredBy} activated Slip n Slide! Good luck stopping!",
            HUDMessage.error_type));
    }

    public static void Tick()
    {
        if (!IsActive) return;
        if (DateTime.UtcNow >= ExpiresAt)
        {
            IsActive  = false;
            VelocityX = 0f;
            VelocityY = 0f;
            Game1.player.temporarySpeedBuff = 0f;
            Game1.addHUDMessage(new HUDMessage(
                "🛝 The slide has ended. Your dignity may never recover.",
                HUDMessage.newQuest_type));
            return;
        }

        var dirs = Game1.player.movementDirections;

        // Add acceleration in pressed directions
        if (dirs.Contains(0)) VelocityY -= Acceleration; // up
        if (dirs.Contains(2)) VelocityY += Acceleration; // down
        if (dirs.Contains(3)) VelocityX -= Acceleration; // left
        if (dirs.Contains(1)) VelocityX += Acceleration; // right

        // Clamp to max speed
        VelocityX = Math.Clamp(VelocityX, -MaxSpeed, MaxSpeed);
        VelocityY = Math.Clamp(VelocityY, -MaxSpeed, MaxSpeed);

        // Apply momentum as position offset
        if (Math.Abs(VelocityX) > 0.1f || Math.Abs(VelocityY) > 0.1f)
        {
            Game1.player.position.X += VelocityX * 2f;
            Game1.player.position.Y += VelocityY * 2f;
        }

        // Apply friction — momentum decays slowly
        VelocityX *= Friction;
        VelocityY *= Friction;
    }

    public static int SecsLeft =>
        IsActive ? Math.Max(0, (int)(ExpiresAt - DateTime.UtcNow).TotalSeconds) : 0;
}

// ─── WARP WHISTLE — 5% chance to warp to random building on entry ────────────

public class WarpWhistleSabotage : ISabotage
{
    public string Name         => "Warp Whistle";
    public string BuyCommand   => "warpwhistle";
    public string Description  => "5% chance to warp to a random building every time you enter one (5 min)";
    public int Cost            => 250;
    public int CooldownSeconds => 180;

    public void Execute(string triggeredBy)
    {
        WarpWhistleState.Activate(triggeredBy, DateTime.UtcNow.AddMinutes(5), 0.05);
        Game1.addHUDMessage(new HUDMessage(
            $"🎺 {triggeredBy} used Warp Whistle! Every door is a gamble for 5 minutes!",
            HUDMessage.error_type));
    }
}

public class WarpWhistlePlusSabotage : ISabotage
{
    public string Name         => "Warp Whistle+";
    public string BuyCommand   => "warpwhistleplus";
    public string Description  => "50% chance to warp to a random building every time you enter one (all day)";
    public int Cost            => 600;
    public int CooldownSeconds => 600;

    public void Execute(string triggeredBy)
    {
        WarpWhistleState.Activate(triggeredBy, DateTime.UtcNow.AddHours(20), 0.50);
        Game1.addHUDMessage(new HUDMessage(
            $"🎺 {triggeredBy} used Warp Whistle+! 50% chance on every door for the rest of the day!",
            HUDMessage.error_type));
    }
}

public class WarpWhistleMaxSabotage : ISabotage
{
    public string Name         => "Warp Whistle MAX";
    public string BuyCommand   => "warpwhistlemax";
    public string Description  => "99.5% chance to warp to a random building every time you enter one (15 min)";
    public int Cost            => 2000;
    public int CooldownSeconds => 600;

    public void Execute(string triggeredBy)
    {
        WarpWhistleState.Activate(triggeredBy, DateTime.UtcNow.AddMinutes(15), 0.995);
        Game1.addHUDMessage(new HUDMessage(
            $"🎺 {triggeredBy} used Warp Whistle MAX! Every door is a trap for 15 minutes!",
            HUDMessage.error_type));
    }
}

public class WarpWhistleMaxPlusSabotage : ISabotage
{
    public string Name         => "Warp Whistle MAX+";
    public string BuyCommand   => "warpwhistlemaxplus";
    public string Description  => "99.5% chance to warp to a random building every time you enter one (all day)";
    public int Cost            => 5000;
    public int CooldownSeconds => 900;

    public void Execute(string triggeredBy)
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
        ("Saloon",          11, 20),
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

        // Only trigger when entering a building (not outdoors)
        var loc = Game1.getLocationFromName(newLocation);
        if (loc == null || loc.IsOutdoors) return;

        // Roll based on current chance
        if (_rng.NextDouble() > Chance) return;

        // Pick a random different building
        var options = Buildings.Where(b =>
            !b.Location.Equals(newLocation, StringComparison.OrdinalIgnoreCase)).ToList();
        if (options.Count == 0) return;

        var dest = options[_rng.Next(options.Count)];
        Game1.warpFarmer(dest.Location, dest.X, dest.Y, false);
        Game1.addHUDMessage(new HUDMessage(
            $"🎺 The Warp Whistle activated! You ended up in {dest.Location}!",
            HUDMessage.error_type));
        Game1.playSound("wand");
    }

    public static int SecsLeft =>
        IsActive ? Math.Max(0, (int)(ExpiresAt - DateTime.UtcNow).TotalSeconds) : 0;

    public static void Draw(SpriteBatch sb)
    {
        if (!IsActive) return;
        var chance = (int)(Chance * 100);
        var text = SecsLeft > 3600
            ? $"Warp Whistle ({chance}%): All day"
            : $"Warp Whistle ({chance}%): {SecsLeft}s";
        var font = Game1.smallFont;
        var pos  = new Vector2(16, 112);
        sb.DrawString(font, text, pos + new Vector2(2, 2), Color.Black);
        sb.DrawString(font, text, pos, Color.Gold);
    }
}