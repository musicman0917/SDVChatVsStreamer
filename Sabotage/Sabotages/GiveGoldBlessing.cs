using StardewValley;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class GiveGoldBlessing : ISabotage
{
    public string Name         => "GiveGold";
    public string BuyCommand   => "givegold";
    public string Description  => "gives 500g";
    public int Cost            => 150;
    public int CooldownSeconds => 120;

    public void Execute(string triggeredBy)
    {
        Game1.player.Money += 500;
        Game1.addHUDMessage(new HUDMessage(
            $"💰 {triggeredBy} gave you 500g!",
            HUDMessage.newQuest_type));
    }
}
