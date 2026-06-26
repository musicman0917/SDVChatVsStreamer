using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class FloorIsLavaSabotage : ISabotage
{
    public string Name         => "Floor is Lava";
    public string BuyCommand   => "floorislava";
    public string Description  => "natural ground deals damage on your farm — only placed paths/floors are safe!";
    public int Cost            => 400;
    public int CooldownSeconds => 240;

    public static bool IsActive      { get; private set; }
    public static DateTime ExpiresAt { get; private set; }
    private static int _damageTick   = 0;
    private static string? _triggeredBy;

    public string? Validate(string args = "")
    {
        var loc = Game1.player.currentLocation;

        if (loc is not StardewValley.Farm)
        {
            return "Floor is Lava only works on your farm — points refunded!";
        }

        return null;
    }

    public void Execute(string triggeredBy)
    {
        _triggeredBy = triggeredBy;

        MrQiDialogue.Show(new[] {
            $"The spirits are very displeased...$pause",
            $"...and also very hot.$pause",
            $"{triggeredBy} has spoken! THE FLOOR IS LAVA!$pause",
            $"Grass, dirt, and crops will burn you. Only placed paths and floors are safe.$pause",
            $"If you haven't built any... well. Heh heh heh."
        }, onDismissed: () =>
        {
            IsActive    = true;
            ExpiresAt   = DateTime.UtcNow.AddMinutes(2);
            _damageTick = 0;
            Game1.playSound("fireball");
            Game1.addHUDMessage(new HUDMessage(
                $"🔥 {triggeredBy} activated Floor is Lava! Only placed paths/floors are safe — everything else burns!",
                HUDMessage.error_type));
        });
    }

    public static void Tick()
    {
        if (!IsActive) return;

        if (DateTime.UtcNow >= ExpiresAt)
        {
            IsActive = false;
            Game1.addHUDMessage(new HUDMessage(
                "🔥 The lava has cooled. You survived!",
                HUDMessage.newQuest_type));
            return;
        }

        // Damage every 2 seconds (120 ticks at 60fps)
        _damageTick++;
        if (_damageTick < 120) return;
        _damageTick = 0;

        if (!IsOnLava()) return;

        // Burn down to 1 HP — never kill
        int damage = Math.Min(15, Game1.player.health - 1);
        if (damage <= 0) return;

        Game1.player.health -= damage;
        Game1.player.currentLocation.playSound("ow");

        // Apply burnt debuff — slows the player
        Game1.player.buffs.Apply(new Buff(
            id:            "CVS_Burnt",
            source:        "Chat vs Streamer",
            displaySource: _triggeredBy ?? "Chat",
            duration:      2500,
            effects:       new StardewValley.Buffs.BuffEffects() { Speed = { -1 } },
            displayName:   "Burnt",
            description:   "The lava is getting to you!"));
    }

    private static bool IsOnLava()
    {
        var loc  = Game1.player.currentLocation;
        var tile = Game1.player.TilePoint;
        var tileVec = new Vector2(tile.X, tile.Y);

        // Safety net — if the player somehow left the farm after triggering
        // (warp whistle, etc.), don't damage them in unrelated locations
        if (loc is not StardewValley.Farm) return false;
        if (!loc.IsOutdoors) return false;

        // Player-placed paths/floors are TerrainFeatures, not Back-layer tiles —
        // this is the only reliable cross-tilesheet way to detect "safe ground"
        if (loc.terrainFeatures.TryGetValue(tileVec, out var feature) &&
            feature is StardewValley.TerrainFeatures.Flooring)
        {
            return false;
        }

        // Anything else on the farm (grass, dirt, HoeDirt, crops, decorative
        // map tiles) counts as natural ground = lava
        return true;
    }

    public static int SecsLeft =>
        IsActive ? Math.Max(0, (int)(ExpiresAt - DateTime.UtcNow).TotalSeconds) : 0;

    // Draw orange haze overlay + countdown
    public static void Draw(SpriteBatch sb)
    {
        if (!IsActive) return;

        var viewport = Game1.graphics.GraphicsDevice.Viewport;
        var rect     = new Rectangle(0, 0, viewport.Width, viewport.Height);

        // Orange haze — more intense when on lava
        float alpha = IsOnLava() ? 0.35f : 0.12f;
        sb.Draw(Game1.fadeToBlackRect, rect, Color.OrangeRed * alpha);

        // Countdown
        var text = $"Floor is Lava: {SecsLeft}s";
        var font = Game1.smallFont;
        var pos  = new Vector2(16, 80);
        sb.DrawString(font, text, pos + new Vector2(2, 2), Color.Black);
        sb.DrawString(font, text, pos, Color.OrangeRed);
    }
}