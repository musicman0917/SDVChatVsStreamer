using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using xTile.Layers;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class FloorIsLavaSabotage : ISabotage
{
    public string Name         => "Floor is Lava";
    public string BuyCommand   => "floorislava";
    public string Description  => "natural tiles deal damage — stay on paths to survive!";
    public int Cost            => 400;
    public int CooldownSeconds => 240;

    public static bool IsActive      { get; private set; }
    public static DateTime ExpiresAt { get; private set; }
    private static int _damageTick   = 0;
    private static string? _triggeredBy;

    // Tile indexes that count as safe flooring on the Back layer
    // Wood path, stone path, gravel path, brick floor, wood floor, straw floor,
    // crystal path, cobblestone, stepping stone
    private static readonly HashSet<int> SafeTileIndexes = new()
    {
        328, 329, 330,   // Wood path
        331, 332, 333,   // Stone path
        334, 335, 336,   // Gravel path
        401, 402, 403,   // Brick floor
        404, 405, 406,   // Wood floor
        407, 408, 409,   // Straw floor
        411, 412, 413,   // Crystal path
        415, 416, 417,   // Cobblestone
        419, 420, 421,   // Stepping stone
        // Indoor floors — always safe
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
    };

    // Locations where Floor is Lava makes sense — player can build paths here
    private static readonly HashSet<string> AllowedLocations = new(StringComparer.OrdinalIgnoreCase)
    {
        "Farm", "FarmHouse", "Greenhouse",
        "Town", "Beach", "Mountain", "Forest",
        "BusStop", "Desert", "Railroad",
        "Woods", "AnimalShop", "SeedShop",
        "Saloon", "Hospital", "Blacksmith",
        "FishShop", "WizardHouse", "ManorHouse",
        "ArchaeologyHouse", "LeahHouse", "SamHouse",
        "HaleyHouse", "SebastianRoom", "JoshHouse",
        "ElliottHouse", "MarnieRanch", "HarveyRoom",
        "ScienceHouse", "Trailer", "AdventureGuild",
        "Bathhouse_Entry", "LeoTreeHouse"
    };

    public string? Validate()
    {
        var locName = Game1.player.currentLocation?.Name ?? "";
        if (!AllowedLocations.Contains(locName))
            return "Floor is Lava only works on the farm and in town — no cheating in the mines!";
        return null;
    }

    public void Execute(string triggeredBy)
    {
        _triggeredBy = triggeredBy;

        MrQiDialogue.Show(new[] {
            $"The spirits are very displeased...$pause",
            $"...and also very hot.$pause",
            $"{triggeredBy} has spoken! THE FLOOR IS LAVA!$pause",
            $"Get to the paths, farmer... if you can. Heh heh heh."
        }, onDismissed: () =>
        {
            IsActive    = true;
            ExpiresAt   = DateTime.UtcNow.AddMinutes(2);
            _damageTick = 0;
            Game1.playSound("fireball");
            Game1.addHUDMessage(new HUDMessage(
                $"🔥 {triggeredBy} activated Floor is Lava! Stay on the paths!",
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
        var loc    = Game1.player.currentLocation;
        var tile   = Game1.player.TilePoint;

        // Indoors is always safe
        if (!loc.IsOutdoors) return false;

        try
        {
            var backLayer = loc.Map.GetLayer("Back");
            if (backLayer == null) return false;

            var tileObj = backLayer.Tiles[tile.X, tile.Y];
            if (tileObj == null) return true; // No tile = natural ground = lava

            return !SafeTileIndexes.Contains(tileObj.TileIndex);
        }
        catch { return false; }
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