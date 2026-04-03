using StardewValley;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class SleepSabotage : ISabotage
{
    public string Name         => "Sleep";
    public string BuyCommand   => "sleep";
    public string Description  => "fast forwards time to 2am";
    public int Cost            => 350;
    public int CooldownSeconds => 600;

    public void Execute(string triggeredBy)
    {
        Game1.timeOfDay = 2600;
        Game1.addHUDMessage(new HUDMessage(
            $"😴 {triggeredBy} fast forwarded to 2am! Better get to bed!",
            HUDMessage.error_type));
    }
}
