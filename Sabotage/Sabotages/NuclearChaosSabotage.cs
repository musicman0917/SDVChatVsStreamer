using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

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
