using StardewModdingAPI;
using StardewValley;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class SunnyWeatherBlessing : ISabotage
{
    public string Name         => "SunnyWeather";
    public string BuyCommand   => "sunny";
    public string Description  => "makes it sunny tomorrow";
    public int Cost            => 75;
    public int CooldownSeconds => 300;

    public void Execute(string triggeredBy)
    {
        var before = Game1.weatherForTomorrow;
        Game1.weatherForTomorrow = "Sun";
        ModEntry.Logger?.Log($"[WeatherSabotage] Sunny: {before} → {Game1.weatherForTomorrow}", LogLevel.Debug);
        Game1.addHUDMessage(new HUDMessage(
            $"☀️ {triggeredBy} cleared the skies! It will be sunny tomorrow!",
            HUDMessage.newQuest_type));
    }
}
