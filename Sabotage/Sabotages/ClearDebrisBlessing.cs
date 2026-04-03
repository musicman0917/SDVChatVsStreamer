using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class ClearDebrisBlessing : ISabotage
{
    public string Name         => "ClearDebris";
    public string BuyCommand   => "cleardebris";
    public string Description  => "clears all weeds and debris from the farm";
    public int Cost            => 200;
    public int CooldownSeconds => 300;

    public void Execute(string triggeredBy)
    {
        var farm    = Game1.getFarm();
        int cleared = 0;

        // Remove weeds (Bush with size 4) from terrain features
        var weedKeys = farm.terrainFeatures.Pairs
            .Where(kv => kv.Value is Bush b && b.size.Value == 4)
            .Select(kv => kv.Key)
            .ToList();
        foreach (var key in weedKeys)
        {
            farm.terrainFeatures.Remove(key);
            cleared++;
        }

        // Remove debris objects (twigs, stones, weeds as objects)
        var debrisKeys = farm.objects.Pairs
            .Where(kv => kv.Value.Name is "Twig" or "Stone" or "Weeds")
            .Select(kv => kv.Key)
            .ToList();
        foreach (var key in debrisKeys)
        {
            farm.objects.Remove(key);
            cleared++;
        }

        Game1.addHUDMessage(new HUDMessage(
            cleared > 0
                ? $"🧹 {triggeredBy} cleared {cleared} pieces of debris from your farm!"
                : $"🧹 {triggeredBy} tried to clear debris but your farm is already clean!",
            HUDMessage.newQuest_type));
    }
}