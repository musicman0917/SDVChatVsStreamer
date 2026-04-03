using StardewValley;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class RestoreHealthBlessing : ISabotage
{
    public string Name         => "RestoreHealth";
    public string BuyCommand   => "restorehealth";
    public string Description  => "restores full health";
    public int Cost            => 100;
    public int CooldownSeconds => 120;

    public void Execute(string triggeredBy)
    {
        Game1.player.health = Game1.player.maxHealth;
        Game1.addHUDMessage(new HUDMessage(
            $"💚 {triggeredBy} restored your health to full!",
            HUDMessage.newQuest_type));
    }
}
