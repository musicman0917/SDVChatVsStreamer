using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

/// <summary>
/// Shared state for the jump scare — fires a full-screen flash + sudden sound
/// after a random hidden delay, with no HUD message before or after, so there's
/// nothing on screen to tip the streamer off before it hits.
/// </summary>
public static class JumpScareState
{
    private static IMonitor? _monitor;
    private static Texture2D? _face;

    private static readonly Random _rng = new();
    private static readonly string[] ScarySounds = { "fuse", "batScreech", "serpentHit" };

    private static DateTime _fireAt      = DateTime.MaxValue;
    private static DateTime _flashUntil  = DateTime.MinValue;

    public static void Init(IModHelper helper, IMonitor monitor)
    {
        _monitor = monitor;
        try
        {
            _face = helper.ModContent.Load<Texture2D>("assets/MrQi.png");
        }
        catch
        {
            _monitor.Log("[JumpScare] MrQi.png not found — flash will be screen-only, no face.", LogLevel.Debug);
        }
    }

    /// <summary>Schedules the scare 8-40 seconds out. No HUD message — that's the whole point.</summary>
    public static void Schedule()
    {
        _fireAt = DateTime.UtcNow.AddSeconds(_rng.Next(8, 40));
    }

    public static void Tick()
    {
        if (DateTime.UtcNow < _fireAt) return;
        _fireAt = DateTime.MaxValue; // consume — don't refire

        _flashUntil = DateTime.UtcNow.AddMilliseconds(450);

        try
        {
            var sound = ScarySounds[_rng.Next(ScarySounds.Length)];
            Game1.playSound(sound);
        }
        catch
        {
            _monitor?.Log("[JumpScare] Sound cue failed to play.", LogLevel.Debug);
        }
    }

    /// <summary>Full-screen flash + face. Draw this every frame; it no-ops outside its brief window.</summary>
    public static void Draw(SpriteBatch sb)
    {
        if (DateTime.UtcNow >= _flashUntil) return;

        var viewport = Game1.graphics.GraphicsDevice.Viewport;
        var screen   = new Rectangle(0, 0, viewport.Width, viewport.Height);
        sb.Draw(Game1.fadeToBlackRect, screen, Color.Black * 0.75f);

        if (_face == null) return;

        var scale  = Math.Min(viewport.Width / (float)_face.Width, viewport.Height / (float)_face.Height) * 0.9f;
        var center = new Vector2(viewport.Width / 2f, viewport.Height / 2f);
        var origin = new Vector2(_face.Width / 2f, _face.Height / 2f);
        sb.Draw(_face, center, null, Color.White, 0f, origin, scale, SpriteEffects.None, 0f);
    }
}

// ─── JUMP SCARE — silent purchase, hidden delay, full-screen flash ───────────

public class JumpScareSabotage : ISabotage
{
    public string Name         => "Jump Scare";
    public string BuyCommand   => "jumpscare";
    public string Description  => "flashes a startling image with a sudden sound, at a random moment in the next ~40 seconds — no warning when you buy it or when it hits";
    public int Cost            => 300;
    public int CooldownSeconds => 300;

    public void Execute(string triggeredBy)
    {
        JumpScareState.Schedule();
        // Intentionally no HUD message here, and none when it fires either —
        // same "no warning" philosophy as Paranoia.
    }
}
