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