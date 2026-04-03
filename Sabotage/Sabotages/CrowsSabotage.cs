using StardewValley;
using StardewValley.TerrainFeatures;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class CrowsSabotage : ISabotage
{
    public string Name         => "Crows";
    public string BuyCommand   => "crows";
    public string Description  => "crows eat a random crop";
    public int Cost            => 150;
    public int CooldownSeconds => 120;

    private readonly Random _rng = new();

    public void Execute(string triggeredBy)
    {
        var farm  = Game1.getFarm();
        var crops = farm.terrainFeatures.Values
            .OfType<HoeDirt>()
            .Where(d => d.crop != null && !d.crop.dead.Value)
            .ToList();

        if (crops.Count == 0)
        {
            Game1.addHUDMessage(new HUDMessage(
                $"🐦 {triggeredBy} sent crows but there are no crops to eat!",
                HUDMessage.newQuest_type));
            return;
        }

        var target = crops[_rng.Next(crops.Count)];
        target.crop.Kill();

        Game1.addHUDMessage(new HUDMessage(
            $"🐦 {triggeredBy} sent crows! A random crop was eaten!",
            HUDMessage.error_type));
    }
}
