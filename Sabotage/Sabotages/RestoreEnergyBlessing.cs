using StardewValley;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class RestoreEnergyBlessing : ISabotage
{
    public string Name         => "RestoreEnergy";
    public string BuyCommand   => "restoreenergy";
    public string Description  => "restores full energy";
    public int Cost            => 100;
    public int CooldownSeconds => 120;

    public void Execute(string triggeredBy)
    {
        Game1.player.Stamina = Game1.player.MaxStamina;
        Game1.addHUDMessage(new HUDMessage(
            $"✨ {triggeredBy} restored your energy to full!",
            HUDMessage.newQuest_type));
    }
}
