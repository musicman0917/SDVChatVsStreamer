using StardewModdingAPI;
using StardewValley;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class GreenRainSabotage : ISabotage
{
    public string Name         => "GreenRain";
    public string BuyCommand   => "greenrain";
    public string Description  => "causes mysterious green rain tomorrow";
    public int Cost            => 200;
    public int CooldownSeconds => 600;

    public void Execute(string triggeredBy)
    {
        var before = Game1.weatherForTomorrow;
        Game1.weatherForTomorrow = "GreenRain";
        ModEntry.Logger?.Log($"[WeatherSabotage] GreenRain: {before} → {Game1.weatherForTomorrow}", LogLevel.Debug);
        Game1.addHUDMessage(new HUDMessage(
            $"🟢 {triggeredBy} summoned mysterious green rain for tomorrow!",
            HUDMessage.error_type));
    }
}
