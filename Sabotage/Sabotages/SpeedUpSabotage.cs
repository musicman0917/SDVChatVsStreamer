using StardewValley;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class SpeedUpSabotage : ISabotage
{
    public string Name         => "SpeedUp";
    public string BuyCommand   => "speedup";
    public string Description  => "fast forwards time by 1 hour";
    public int Cost            => 100;
    public int CooldownSeconds => 120;

    public void Execute(string triggeredBy)
    {
        // Stardew time: 600 = 6am, 100 = 1 hour
        Game1.timeOfDay = Math.Min(Game1.timeOfDay + 100, 2600);

        Game1.addHUDMessage(new HUDMessage(
            $"⏩ {triggeredBy} fast forwarded time by 1 hour!",
            HUDMessage.error_type));
    }
}
