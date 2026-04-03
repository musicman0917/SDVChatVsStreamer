using StardewModdingAPI;
using StardewValley;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class RainSabotage : ISabotage
{
    public string Name         => "Rain";
    public string BuyCommand   => "rain";
    public string Description  => "makes it rain tomorrow";
    public int Cost            => 50;
    public int CooldownSeconds => 300;

    public void Execute(string triggeredBy)
    {
        var before = Game1.weatherForTomorrow;
        Game1.weatherForTomorrow = "Rain";
        ModEntry.Logger?.Log($"[WeatherSabotage] Rain: {before} → {Game1.weatherForTomorrow}", LogLevel.Debug);
        Game1.addHUDMessage(new HUDMessage(
            $"☔ {triggeredBy} called the rain! It will rain tomorrow.",
            HUDMessage.error_type));
    }
}
