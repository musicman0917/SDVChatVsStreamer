using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

/// <summary>
/// The flash, the boom(s), and Lewis's reaction. Kept separate from the actual
/// destruction logic so a problem here can never affect whether the farm itself
/// gets wiped.
/// </summary>
public static class NuclearChaosState
{
    private static readonly Random _rng = new();

    private static readonly string[] WizardLines =
    {
        "I sense... a GIANT FIREBALL, hurtling toward your farm as we speak!",
        "The arcane winds warn me: a sphere of pure flame descends upon you!",
        "By my beard! A fireball of unspeakable size approaches your land!",
        "The stars foretold this. A great fire falls from the heavens, mortal.",
        "Flee if you must — a colossal fireball is already in the air!",
        "My crystal ball shows only fire. SO MUCH FIRE.",
        "This was not in my calculations. A meteor of flame descends!",
        "I have seen many omens, farmer. None so dire as this blazing sphere.",
        "The ley lines tremble! Something enormous and burning approaches!",
        "Quickly, gather your belongings — the sky itself is about to ignite!",
        "Junimo, hide! A fireball vast enough to swallow the valley draws near!",
        "I warned the Guild about this. They did not listen. NOBODY listens.",
        "This magic is beyond even me. Brace yourself, mortal.",
        "The tower's windows rattle — a fireball of ancient power nears your farm!",
        "I felt the disturbance from my tower. A great flame comes for your crops.",
        "Do not look directly at it! ...too late. Well, good luck.",
        "The prophecy spoke of this day. I did not think it would be a Tuesday.",
        "Rasmodius warns you: incoming fireball, of truly cosmic proportions!",
        "My familiar just fainted. That's never a good sign. FIREBALL INBOUND!",
        "Hold onto something. The heavens are hurling fire at your farm!",
    };

    private static DateTime _flashUntil = DateTime.MinValue;
    private static DateTime _hazeUntil  = DateTime.MinValue;
    private static readonly List<DateTime> _pendingAftershocks = new();

    public static void Trigger()
    {
        var now = DateTime.UtcNow;
        _flashUntil = now.AddMilliseconds(600);
        _hazeUntil  = now.AddSeconds(6);

        // The real blast — reuses the exact same GameLocation.explode(...) call
        // BombSabotage already uses, for a proven-correct sound/VFX/damage boom.
        try
        {
            var loc = Game1.player.currentLocation;
            var pos = new Vector2(Game1.player.TilePoint.X, Game1.player.TilePoint.Y);
            loc.explode(pos, 5, Game1.player);
        }
        catch { /* explosion is a nice-to-have, never worth crashing over */ }

        // Staggered aftershocks nearby — the "alarms going off" cascade
        _pendingAftershocks.Clear();
        _pendingAftershocks.Add(now.AddMilliseconds(500));
        _pendingAftershocks.Add(now.AddMilliseconds(1100));
        _pendingAftershocks.Add(now.AddMilliseconds(1900));

        // The Wizard senses doom. Always shown as a HUD line; also tried as a
        // speech bubble above his actual head if he happens to be nearby.
        var line = WizardLines[_rng.Next(WizardLines.Length)];
        try
        {
            var wizard = Game1.getCharacterFromName("Wizard");
            wizard?.showTextAboveHead(line);
        }
        catch { }
        Game1.addHUDMessage(new HUDMessage($"🧙 Wizard: \"{line}\"", HUDMessage.error_type));
    }

    public static void Tick()
    {
        if (_pendingAftershocks.Count == 0 || !StardewModdingAPI.Context.IsWorldReady) return;

        var now = DateTime.UtcNow;
        for (int i = _pendingAftershocks.Count - 1; i >= 0; i--)
        {
            if (now < _pendingAftershocks[i]) continue;
            _pendingAftershocks.RemoveAt(i);

            try
            {
                var loc    = Game1.player.currentLocation;
                var offset = new Vector2(_rng.Next(-3, 4), _rng.Next(-3, 4));
                var pos    = new Vector2(Game1.player.TilePoint.X, Game1.player.TilePoint.Y) + offset;
                loc.explode(pos, 2, Game1.player);
            }
            catch { }
        }
    }

    /// <summary>Bright white flash, fading into a lingering orange haze. Draw every frame; no-ops outside its window.</summary>
    public static void Draw(SpriteBatch sb)
    {
        var now = DateTime.UtcNow;
        if (now >= _hazeUntil) return;

        var viewport = Game1.graphics.GraphicsDevice.Viewport;
        var screen   = new Rectangle(0, 0, viewport.Width, viewport.Height);

        if (now < _flashUntil)
        {
            var frac = (_flashUntil - now).TotalMilliseconds / 600.0;
            sb.Draw(Game1.fadeToBlackRect, screen, Color.White * (float)Math.Clamp(frac, 0, 1));
        }
        else
        {
            sb.Draw(Game1.fadeToBlackRect, screen, Color.OrangeRed * 0.22f);
        }
    }
}

// ─── NUCLEAR CHAOS — wipes the farm down to the house, cabins, greenhouse, and shipping bin ──

public class NuclearChaosSabotage : ISabotage
{
    public string Name         => "Nuclear Chaos";
    public string BuyCommand   => "nuclearchaos";
    public string Description  => "the ultimate sabotage — demolishes every non-housing building (animals inside included), kills every crop, fells every tree, and clears every rock and fence on the farm";
    public int Cost            => 50000;
    public int CooldownSeconds => 7200;

    // Farmhouse isn't a Building at all, so it's safe by construction. These are
    // the only Building types spared alongside it.
    private static readonly string[] KeepBuildingTypes = { "Cabin", "Greenhouse", "Shipping" };

    public void Execute(string triggeredBy)
    {
        NuclearChaosState.Trigger();

        var farm = Game1.getFarm();
        int buildingsDestroyed = 0, animalsLost = 0, cropsKilled = 0, treesFelled = 0, rocksCleared = 0, fencesDestroyed = 0;

        // ── Buildings (and any animals living in them) ──
        try
        {
            foreach (var building in farm.buildings.ToList())
            {
                var type = building.buildingType.Value ?? "";
                if (KeepBuildingTypes.Any(k => type.Contains(k, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var homed = farm.animals.Values.Where(a => a.home == building).ToList();
                foreach (var animal in homed)
                {
                    farm.animals.Remove(animal.myID.Value);
                    animalsLost++;
                }

                farm.buildings.Remove(building);
                buildingsDestroyed++;
            }
        }
        catch (Exception ex)
        {
            ModEntry.Logger?.Log($"[NuclearChaos] Building demolition error: {ex.Message}", StardewModdingAPI.LogLevel.Warn);
        }

        // ── Crops ──
        try
        {
            foreach (var dirt in farm.terrainFeatures.Values.OfType<HoeDirt>())
            {
                if (dirt.crop == null || dirt.crop.dead.Value) continue;
                dirt.crop.Kill();
                cropsKilled++;
            }
        }
        catch (Exception ex)
        {
            ModEntry.Logger?.Log($"[NuclearChaos] Crop kill error: {ex.Message}", StardewModdingAPI.LogLevel.Warn);
        }

        // ── Trees & fruit trees ──
        try
        {
            var treeKeys = farm.terrainFeatures.Pairs
                .Where(kv => kv.Value is Tree or FruitTree)
                .Select(kv => kv.Key)
                .ToList();
            foreach (var key in treeKeys)
            {
                farm.terrainFeatures.Remove(key);
                treesFelled++;
            }
        }
        catch (Exception ex)
        {
            ModEntry.Logger?.Log($"[NuclearChaos] Tree removal error: {ex.Message}", StardewModdingAPI.LogLevel.Warn);
        }

        // ── Rocks — resource clumps (boulders, stumps, meteorites) ──
        try
        {
            foreach (var clump in farm.resourceClumps.ToList())
            {
                farm.resourceClumps.Remove(clump);
                rocksCleared++;
            }
        }
        catch (Exception ex)
        {
            ModEntry.Logger?.Log($"[NuclearChaos] Resource clump removal error: {ex.Message}", StardewModdingAPI.LogLevel.Warn);
        }

        // ── Rocks — smaller mineable stones/ore nodes, and fences (both live in .objects) ──
        try
        {
            var toRemove = farm.objects.Pairs
                .Where(kv =>
                    kv.Value is Fence ||
                    (kv.Value.Name != null &&
                     (kv.Value.Name.Contains("Stone", StringComparison.OrdinalIgnoreCase) ||
                      kv.Value.Name.Contains("Node", StringComparison.OrdinalIgnoreCase))))
                .ToList();

            foreach (var kv in toRemove)
            {
                farm.objects.Remove(kv.Key);
                if (kv.Value is Fence) fencesDestroyed++;
                else rocksCleared++;
            }
        }
        catch (Exception ex)
        {
            ModEntry.Logger?.Log($"[NuclearChaos] Rock/fence removal error: {ex.Message}", StardewModdingAPI.LogLevel.Warn);
        }

        Game1.addHUDMessage(new HUDMessage(
            $"☢️ {triggeredBy} unleashed NUCLEAR CHAOS! {buildingsDestroyed} buildings demolished, {animalsLost} animals lost, {cropsKilled} crops killed, {treesFelled} trees felled, {rocksCleared} rocks cleared, {fencesDestroyed} fences destroyed. Only the house stands.",
            HUDMessage.error_type));
    }
}
