using StardewValley;
using StardewValley.TerrainFeatures;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class WaterCropsBlessing : ISabotage
{
    public string Name         => "WaterCrops";
    public string BuyCommand   => "watercrops";
    public string Description  => "waters all crops on the farm";
    public int Cost            => 150;
    public int CooldownSeconds => 300;

    public void Execute(string triggeredBy)
    {
        var farm    = Game1.getFarm();
        int watered = 0;

        foreach (var tf in farm.terrainFeatures.Values.OfType<HoeDirt>())
        {
            if (tf.crop != null)
            {
                tf.state.Value = HoeDirt.watered;
                watered++;
            }
        }

        Game1.addHUDMessage(new HUDMessage(
            watered > 0
                ? $"💧 {triggeredBy} watered all {watered} of your crops!"
                : $"💧 {triggeredBy} tried to water your crops but you have none!",
            HUDMessage.newQuest_type));
    }
}
