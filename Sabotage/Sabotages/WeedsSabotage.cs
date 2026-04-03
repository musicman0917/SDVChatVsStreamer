using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class WeedsSabotage : ISabotage
{
    public string Name         => "Weeds";
    public string BuyCommand   => "weeds";
    public string Description  => "spawns weeds on the farm";
    public int Cost            => 250;
    public int CooldownSeconds => 120;

    private readonly Random _rng = new();

    public void Execute(string triggeredBy)
    {
        var farm    = Game1.getFarm();
        int spawned = 0;

        // Try to spawn 5 weeds on random open tiles
        for (int attempt = 0; attempt < 50 && spawned < 5; attempt++)
        {
            int x = _rng.Next(1, farm.Map.Layers[0].LayerWidth  - 1);
            int y = _rng.Next(1, farm.Map.Layers[0].LayerHeight - 1);
            var tile = new Vector2(x, y);

            if (farm.isTileLocationOpen(new xTile.Dimensions.Location(x, y)) &&
                !farm.terrainFeatures.ContainsKey(tile) &&
                !farm.objects.ContainsKey(tile))
            {
                farm.terrainFeatures.Add(tile, new Bush(tile, 4, farm));
                spawned++;
            }
        }

        Game1.addHUDMessage(new HUDMessage(
            $"🌿 {triggeredBy} scattered weeds all over your farm!",
            HUDMessage.error_type));
    }
}