using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace SDVChatVsStreamer.Sabotage;

/// <summary>
/// Shared safe-warp logic used by all warp-based sabotages. Validates the
/// destination tile is actually passable before committing to a warp, with
/// a spiral search fallback — this matters because mods (SVE, house upgrades,
/// etc.) can shift map layouts so hardcoded coordinates land on walls/water.
/// </summary>
public static class WarpHelper
{
    /// <summary>
    /// Attempts to warp the farmer to (x, y) in the given location, searching
    /// nearby tiles for a passable spot if the exact coordinates aren't valid.
    /// Returns true if a warp was performed.
    /// </summary>
    public static bool SafeWarp(string locationName, int x, int y, bool flip = false, int maxRadius = 6)
    {
        var loc = Game1.getLocationFromName(locationName);
        if (loc == null)
        {
            ModEntry.Logger?.Log($"[WarpHelper] SafeWarp failed: location '{locationName}' not found.", LogLevel.Warn);
            return false;
        }

        var target = FindNearestPassable(loc, new Vector2(x, y), maxRadius);
        if (target == null)
        {
            ModEntry.Logger?.Log($"[WarpHelper] SafeWarp failed: no passable tile found near ({x},{y}) in '{locationName}' within radius {maxRadius}.", LogLevel.Warn);
            return false;
        }

        // A tile can be locally passable and still sit in a walled-off pocket with no path
        // out — map overhaul mods are especially prone to creating these. Verify it can
        // actually reach one of the location's own exits before committing to the warp.
        if (!IsConnectedToExit(loc, target.Value))
        {
            ModEntry.Logger?.Log($"[WarpHelper] SafeWarp rejected ({target.Value.X},{target.Value.Y}) in '{locationName}' — isolated from any exit.", LogLevel.Warn);
            return false;
        }

        ModEntry.Logger?.Log($"[WarpHelper] Warping to '{locationName}' at ({target.Value.X},{target.Value.Y}) (requested {x},{y}).", LogLevel.Info);
        Game1.warpFarmer(locationName, (int)target.Value.X, (int)target.Value.Y, flip);
        return true;
    }

    private const int MaxConnectivityTiles = 500;
    private static readonly (int Dx, int Dy)[] _cardinalDirs = { (1, 0), (-1, 0), (0, 1), (0, -1) };

    /// <summary>
    /// Bounded flood-fill from a candidate tile to see if it can reach one of the
    /// location's own warp/exit points. Catches the case where a tile is locally
    /// passable but part of an isolated pocket with no way out. Locations with no
    /// registered warps (small closed rooms, etc.) skip the check — nothing to
    /// verify connectivity against.
    /// </summary>
    private static bool IsConnectedToExit(GameLocation loc, Vector2 start)
    {
        if (loc.warps == null || loc.warps.Count == 0) return true;

        var targets = new HashSet<(int X, int Y)>();
        foreach (var w in loc.warps) targets.Add((w.X, w.Y));

        var startKey = ((int)start.X, (int)start.Y);
        if (targets.Contains(startKey)) return true;

        var visited = new HashSet<(int X, int Y)> { startKey };
        var queue   = new Queue<(int X, int Y)>();
        queue.Enqueue(startKey);

        while (queue.Count > 0 && visited.Count <= MaxConnectivityTiles)
        {
            var (cx, cy) = queue.Dequeue();
            foreach (var (dx, dy) in _cardinalDirs)
            {
                var next = (X: cx + dx, Y: cy + dy);
                if (visited.Contains(next)) continue;
                if (targets.Contains(next)) return true;
                if (!IsTilePassable(loc, new Vector2(next.X, next.Y))) continue;

                visited.Add(next);
                queue.Enqueue(next);
            }
        }

        return false;
    }

    private static Vector2? FindNearestPassable(GameLocation loc, Vector2 origin, int maxRadius, bool skipOrigin = false)
    {
        if (!skipOrigin && IsTilePassable(loc, origin)) return origin;

        for (int radius = 1; radius <= maxRadius; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (Math.Abs(dx) != radius && Math.Abs(dy) != radius) continue; // only ring perimeter
                var tile = origin + new Vector2(dx, dy);
                if (IsTilePassable(loc, tile)) return tile;
            }
        }

        return null;
    }

    /// <summary>
    /// Warps to a random spot in the given location, preferring one of the location's
    /// own warp/exit points over a hardcoded coordinate. Exit points are guaranteed to
    /// sit on the connected walkable path network — a bare passability check on a
    /// hardcoded coordinate can still land on an isolated, unreachable tile once a map
    /// overhaul mod (SVE, Ridgeside, etc.) changes what's around it, which is how a
    /// "random location" warp turns into a softlock instead of just an inconvenience.
    /// Falls back to the hardcoded coordinate if the location has no warp points.
    /// </summary>
    public static bool SafeWarpToRandomSpot(string locationName, int fallbackX, int fallbackY, Random rng)
    {
        var loc = Game1.getLocationFromName(locationName);
        if (loc == null)
        {
            ModEntry.Logger?.Log($"[WarpHelper] SafeWarpToRandomSpot failed: location '{locationName}' not found.", LogLevel.Warn);
            return false;
        }

        if (loc.warps != null && loc.warps.Count > 0)
        {
            // Try warp points in random order, landing just off the trigger tile itself —
            // standing exactly on a warp tile can immediately re-fire that same transition
            // (an unwanted double/triple warp) instead of settling safely at the destination.
            var order = Enumerable.Range(0, loc.warps.Count).OrderBy(_ => rng.Next());
            foreach (var i in order)
            {
                var warp   = loc.warps[i];
                var target = FindNearestPassable(loc, new Vector2(warp.X, warp.Y), 3, skipOrigin: true);
                if (target == null) continue;

                ModEntry.Logger?.Log($"[WarpHelper] Warping to '{locationName}' near warp point ({warp.X},{warp.Y}) at ({target.Value.X},{target.Value.Y}).", LogLevel.Info);
                Game1.warpFarmer(locationName, (int)target.Value.X, (int)target.Value.Y, false);
                return true;
            }
        }

        return SafeWarp(locationName, fallbackX, fallbackY);
    }

    public static bool IsTilePassable(GameLocation loc, Vector2 tile)
    {
        try
        {
            if (tile.X < 0 || tile.Y < 0) return false;
            return loc.isTilePassable(new xTile.Dimensions.Location((int)tile.X, (int)tile.Y), Game1.viewport)
                   && !loc.isWaterTile((int)tile.X, (int)tile.Y);
        }
        catch { return false; }
    }
}