using StardewModdingAPI;
using StardewValley;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class StormSabotage : ISabotage
{
    public string Name         => "Storm";
    public string BuyCommand   => "storm";
    public string Description  => "makes it storm tomorrow";
    public int Cost            => 75;
    public int CooldownSeconds => 300;

    public void Execute(string triggeredBy)
    {
        var before = Game1.weatherForTomorrow;
        Game1.weatherForTomorrow = "Storm";
        ModEntry.Logger?.Log($"[WeatherSabotage] Storm: {before} → {Game1.weatherForTomorrow}", LogLevel.Debug);
        Game1.addHUDMessage(new HUDMessage(
            $"⛈️ {triggeredBy} called a lightning storm! Better stay inside tomorrow!",
            HUDMessage.error_type));
    }
}
