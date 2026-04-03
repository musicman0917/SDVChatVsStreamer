using StardewValley;
using StardewValley.TerrainFeatures;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class KillFarmSabotage : ISabotage
{
    public string Name         => "KillFarm";
    public string BuyCommand   => "killfarm";
    public string Description  => "kills ALL crops on the farm";
    public int Cost            => 500;
    public int CooldownSeconds => 600;

    public void Execute(string triggeredBy)
    {
        var farm  = Game1.getFarm();
        var crops = farm.terrainFeatures.Values
            .OfType<HoeDirt>()
            .Where(d => d.crop != null && !d.crop.dead.Value)
            .ToList();

        int killed = 0;
        foreach (var dirt in crops)
        {
            dirt.crop.Kill();
            killed++;
        }

        Game1.addHUDMessage(new HUDMessage(
            killed > 0
                ? $"💀 {triggeredBy} killed ALL {killed} crops on your farm!"
                : $"💀 {triggeredBy} tried to kill your crops but you have none!",
            HUDMessage.error_type));
    }
}
