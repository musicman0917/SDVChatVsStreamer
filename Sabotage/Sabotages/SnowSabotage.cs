using StardewModdingAPI;
using StardewValley;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class SnowSabotage : ISabotage
{
    public string Name         => "Snow";
    public string BuyCommand   => "snow";
    public string Description  => "makes it snow tomorrow";
    public int Cost            => 75;
    public int CooldownSeconds => 300;

    public string? Validate() =>
        Game1.currentSeason != "winter"
            ? $"snow only works in winter! (current season: {Game1.currentSeason})"
            : null;

    public void Execute(string triggeredBy)
    {
        var before = Game1.weatherForTomorrow;
        Game1.weatherForTomorrow = "Snow";
        ModEntry.Logger?.Log($"[WeatherSabotage] Snow: {before} → {Game1.weatherForTomorrow}", LogLevel.Debug);
        Game1.addHUDMessage(new HUDMessage(
            $"❄️ {triggeredBy} called a snowstorm! It will snow tomorrow!",
            HUDMessage.error_type));
    }
}
