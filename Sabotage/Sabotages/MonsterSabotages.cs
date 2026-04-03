using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Monsters;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

internal static class MonsterSpawnHelper
{
    /// <summary>
    /// Finds the nearest passable tile to the player within maxRadius.
    /// Returns world-space pixel position (tile * 64).
    /// Falls back to player position if nothing found.
    /// </summary>
    public static Vector2 FindSpawnTile(GameLocation loc, Vector2 playerTile, int preferredOffsetX = 2, int preferredOffsetY = 0, int maxRadius = 6)
    {
        // Try preferred offset first
        var preferred = playerTile + new Vector2(preferredOffsetX, preferredOffsetY);
        if (IsTilePassable(loc, preferred)) return preferred * 64f;

        // Spiral outward from the player to find nearest passable tile
        for (int radius = 1; radius <= maxRadius; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (Math.Abs(dx) != radius && Math.Abs(dy) != radius) continue;
                var tile = playerTile + new Vector2(dx, dy);
                if (IsTilePassable(loc, tile)) return tile * 64f;
            }
        }

        // Absolute fallback — just use player tile
        return playerTile * 64f;
    }

    /// <summary>
    /// Finds multiple distinct passable tiles near the player.
    /// </summary>
    public static List<Vector2> FindSpawnTiles(GameLocation loc, Vector2 playerTile, int count, int maxRadius = 8)
    {
        var results = new List<Vector2>();
        var used    = new HashSet<(int, int)>();

        for (int radius = 1; radius <= maxRadius && results.Count < count; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (results.Count >= count) break;
                if (Math.Abs(dx) != radius && Math.Abs(dy) != radius) continue;
                var tile = playerTile + new Vector2(dx, dy);
                var key  = ((int)tile.X, (int)tile.Y);
                if (used.Contains(key)) continue;
                if (!IsTilePassable(loc, tile)) continue;
                used.Add(key);
                results.Add(tile * 64f);
            }
        }

        // Fill remaining with player tile if we couldn't find enough
        while (results.Count < count)
            results.Add(playerTile * 64f);

        return results;
    }

    private static bool IsTilePassable(GameLocation loc, Vector2 tile)
    {
        try
        {
            if (tile.X < 0 || tile.Y < 0) return false;
            var rect = new Microsoft.Xna.Framework.Rectangle((int)tile.X * 64, (int)tile.Y * 64, 64, 64);
            return loc.isTilePassable(new xTile.Dimensions.Location((int)tile.X, (int)tile.Y),
                       Game1.viewport) &&
                   !loc.isWaterTile((int)tile.X, (int)tile.Y);
        }
        catch { return false; }
    }
}

// ─── LOW TIER ────────────────────────────────────────────────────────────────

public class SpawnBugSabotage : ISabotage
{
    public string Name         => "SpawnBugs";
    public string BuyCommand   => "bugs";
    public string Description  => "spawns a swarm of bugs near you";
    public int Cost            => 75;
    public int CooldownSeconds => 60;

    public void Execute(string triggeredBy)
    {
        var loc    = Game1.player.currentLocation;
        var origin = new Vector2(Game1.player.TilePoint.X, Game1.player.TilePoint.Y);
        var tiles  = MonsterSpawnHelper.FindSpawnTiles(loc, origin, 5);
        foreach (var pos in tiles)
            loc.characters.Add(new Bug(pos, 0));

        Game1.addHUDMessage(new HUDMessage(
            $"🐛 {triggeredBy} sent a bug swarm after you!",
            HUDMessage.error_type));
    }
}

public class SpawnGrubSabotage : ISabotage
{
    public string Name         => "SpawnGrubs";
    public string BuyCommand   => "grubs";
    public string Description  => "spawns grubs near you";
    public int Cost            => 75;
    public int CooldownSeconds => 60;

    public void Execute(string triggeredBy)
    {
        var loc    = Game1.player.currentLocation;
        var origin = new Vector2(Game1.player.TilePoint.X, Game1.player.TilePoint.Y);
        var tiles  = MonsterSpawnHelper.FindSpawnTiles(loc, origin, 4);
        foreach (var pos in tiles)
            loc.characters.Add(new Grub(pos, false));

        Game1.addHUDMessage(new HUDMessage(
            $"🐛 {triggeredBy} sent grubs crawling at you!",
            HUDMessage.error_type));
    }
}

public class SpawnGolemSabotage : ISabotage
{
    public string Name         => "SpawnGolem";
    public string BuyCommand   => "golem";
    public string Description  => "spawns a rock golem — slow but high defense";
    public int Cost            => 100;
    public int CooldownSeconds => 90;

    public void Execute(string triggeredBy)
    {
        var loc = Game1.player.currentLocation;
        var origin = new Vector2(Game1.player.TilePoint.X, Game1.player.TilePoint.Y);
        var pos = MonsterSpawnHelper.FindSpawnTile(loc, origin, 2, 0);
        loc.characters.Add(new RockGolem(pos, Game1.player.CombatLevel));
        Game1.addHUDMessage(new HUDMessage(
            $"🪨 {triggeredBy} dropped a rock golem in your path!",
            HUDMessage.error_type));
    }
}

// ─── MID TIER ────────────────────────────────────────────────────────────────

public class SpawnFrostBatSabotage : ISabotage
{
    public string Name         => "SpawnFrostBat";
    public string BuyCommand   => "frostbat";
    public string Description  => "spawns a frost bat near you";
    public int Cost            => 150;
    public int CooldownSeconds => 90;

    public void Execute(string triggeredBy)
    {
        var loc = Game1.player.currentLocation;
        var origin = new Vector2(Game1.player.TilePoint.X, Game1.player.TilePoint.Y);
        var pos = MonsterSpawnHelper.FindSpawnTile(loc, origin, 2, -2);
        loc.characters.Add(new Bat(pos, -555));
        Game1.addHUDMessage(new HUDMessage(
            $"🦇 {triggeredBy} released a frost bat!",
            HUDMessage.error_type));
    }
}

public class SpawnDustSpriteSabotage : ISabotage
{
    public string Name         => "SpawnDustSprites";
    public string BuyCommand   => "dust";
    public string Description  => "spawns a pack of dust sprites — they come in groups";
    public int Cost            => 150;
    public int CooldownSeconds => 90;

    public void Execute(string triggeredBy)
    {
        var loc    = Game1.player.currentLocation;
        var origin = new Vector2(Game1.player.TilePoint.X, Game1.player.TilePoint.Y);
        var tiles  = MonsterSpawnHelper.FindSpawnTiles(loc, origin, 5);
        foreach (var pos in tiles)
            loc.characters.Add(new DustSpirit(pos, false));

        Game1.addHUDMessage(new HUDMessage(
            $"💨 {triggeredBy} summoned a dust sprite pack!",
            HUDMessage.error_type));
    }
}

public class SpawnGhostSabotage : ISabotage
{
    public string Name         => "SpawnGhost";
    public string BuyCommand   => "ghost";
    public string Description  => "spawns a ghost — it teleports and pushes you back";
    public int Cost            => 200;
    public int CooldownSeconds => 120;

    public void Execute(string triggeredBy)
    {
        var loc = Game1.player.currentLocation;
        var origin = new Vector2(Game1.player.TilePoint.X, Game1.player.TilePoint.Y);
        var pos = MonsterSpawnHelper.FindSpawnTile(loc, origin, 3, 0);
        loc.characters.Add(new Ghost(pos));
        Game1.addHUDMessage(new HUDMessage(
            $"👻 {triggeredBy} sent a ghost to haunt you!",
            HUDMessage.error_type));
    }
}

// ─── HIGH TIER ────────────────────────────────────────────────────────────────

public class SpawnSerpentSabotage : ISabotage
{
    public string Name         => "SpawnSerpent";
    public string BuyCommand   => "serpent";
    public string Description  => "spawns a serpent — fast, large hitbox, high damage";
    public int Cost            => 350;
    public int CooldownSeconds => 150;

    public void Execute(string triggeredBy)
    {
        var loc = Game1.player.currentLocation;
        var origin = new Vector2(Game1.player.TilePoint.X, Game1.player.TilePoint.Y);
        var pos = MonsterSpawnHelper.FindSpawnTile(loc, origin, 3, 0);
        loc.characters.Add(new Serpent(pos));
        Game1.addHUDMessage(new HUDMessage(
            $"🐍 {triggeredBy} unleashed a serpent!",
            HUDMessage.error_type));
    }
}

public class SpawnShadowBruteSabotage : ISabotage
{
    public string Name         => "SpawnShadowBrute";
    public string BuyCommand   => "shadowbrute";
    public string Description  => "spawns a shadow brute — heavy hitter";
    public int Cost            => 400;
    public int CooldownSeconds => 180;

    public void Execute(string triggeredBy)
    {
        var loc = Game1.player.currentLocation;
        var origin = new Vector2(Game1.player.TilePoint.X, Game1.player.TilePoint.Y);
        var pos = MonsterSpawnHelper.FindSpawnTile(loc, origin, 2, 0);
        loc.characters.Add(new ShadowBrute(pos));
        Game1.addHUDMessage(new HUDMessage(
            $"👹 {triggeredBy} summoned a shadow brute!",
            HUDMessage.error_type));
    }
}

public class SpawnShadowShamanSabotage : ISabotage
{
    public string Name         => "SpawnShadowShaman";
    public string BuyCommand   => "shaman";
    public string Description  => "spawns a shadow shaman — heals enemies and fires debuff projectiles";
    public int Cost            => 400;
    public int CooldownSeconds => 180;

    public void Execute(string triggeredBy)
    {
        var loc = Game1.player.currentLocation;
        var origin = new Vector2(Game1.player.TilePoint.X, Game1.player.TilePoint.Y);
        var pos = MonsterSpawnHelper.FindSpawnTile(loc, origin, 3, -1);
        loc.characters.Add(new ShadowShaman(pos));
        Game1.addHUDMessage(new HUDMessage(
            $"🔮 {triggeredBy} summoned a shadow shaman!",
            HUDMessage.error_type));
    }
}

public class SpawnIridiumGolemSabotage : ISabotage
{
    public string Name         => "SpawnIridiumGolem";
    public string BuyCommand   => "iridiumgolem";
    public string Description  => "spawns an iridium golem — extremely tanky late game threat";
    public int Cost            => 500;
    public int CooldownSeconds => 240;

    public void Execute(string triggeredBy)
    {
        var loc = Game1.player.currentLocation;
        var origin = new Vector2(Game1.player.TilePoint.X, Game1.player.TilePoint.Y);
        var pos = MonsterSpawnHelper.FindSpawnTile(loc, origin, 2, 0);
        loc.characters.Add(new RockGolem(pos, 10));
        Game1.addHUDMessage(new HUDMessage(
            $"💎 {triggeredBy} dropped an iridium golem on you!",
            HUDMessage.error_type));
    }
}

public class SpawnSquidKidSabotage : ISabotage
{
    public string Name         => "SpawnSquidKids";
    public string BuyCommand   => "squidkids";
    public string Description  => "spawns squid kids — 1HP but they fireball you from range";
    public int Cost            => 300;
    public int CooldownSeconds => 120;

    public void Execute(string triggeredBy)
    {
        var loc    = Game1.player.currentLocation;
        var origin = new Vector2(Game1.player.TilePoint.X, Game1.player.TilePoint.Y);
        var tiles  = MonsterSpawnHelper.FindSpawnTiles(loc, origin, 4);
        foreach (var pos in tiles)
            loc.characters.Add(new SquidKid(pos));

        Game1.addHUDMessage(new HUDMessage(
            $"🔥 {triggeredBy} surrounded you with squid kids!",
            HUDMessage.error_type));
    }
}