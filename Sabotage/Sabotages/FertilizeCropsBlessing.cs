using StardewValley;
using StardewValley.TerrainFeatures;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class FertilizeCropsBlessing : ISabotage
{
    public string Name         => "FertilizeCrops";
    public string BuyCommand   => "fertilize";
    public string Description  => "applies basic fertilizer to all crops";
    public int Cost            => 200;
    public int CooldownSeconds => 600;

    public void Execute(string triggeredBy)
    {
        var farm        = Game1.getFarm();
        int fertilized  = 0;

        foreach (var tf in farm.terrainFeatures.Values.OfType<HoeDirt>())
        {
            if (tf.crop != null && string.IsNullOrEmpty(tf.fertilizer.Value))
            {
                tf.fertilizer.Value = "(O)465"; // Speed-Gro item ID in 1.6
                fertilized++;
            }
        }

        Game1.addHUDMessage(new HUDMessage(
            fertilized > 0
                ? $"🌱 {triggeredBy} fertilized {fertilized} of your crops with Speed-Gro!"
                : $"🌱 {triggeredBy} tried to fertilize your crops but there's nothing to fertilize!",
            HUDMessage.newQuest_type));
    }
}
