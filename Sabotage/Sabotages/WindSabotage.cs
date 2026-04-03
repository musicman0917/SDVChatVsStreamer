using StardewModdingAPI;
using StardewValley;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class WindSabotage : ISabotage
{
    public string Name         => "Wind";
    public string BuyCommand   => "wind";
    public string Description  => "makes it windy tomorrow";
    public int Cost            => 50;
    public int CooldownSeconds => 300;

    public void Execute(string triggeredBy)
    {
        var before = Game1.weatherForTomorrow;
        Game1.weatherForTomorrow = "Wind";
        ModEntry.Logger?.Log($"[WeatherSabotage] Wind: {before} → {Game1.weatherForTomorrow}", LogLevel.Debug);
        Game1.addHUDMessage(new HUDMessage(
            $"💨 {triggeredBy} called the wind! It will be windy tomorrow!",
            HUDMessage.error_type));
    }
}
