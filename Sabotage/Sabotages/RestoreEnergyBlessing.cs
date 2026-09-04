using StardewValley;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class RestoreEnergyBlessing : ISabotage
{
    public string Name         => "RestoreEnergy";
    public string BuyCommand   => "restoreenergy";
    public string Description  => "restores full energy";
    public int Cost            => 100;
    public int CooldownSeconds => 120;
    public SabotageTier Tier   => SabotageTier.Blessing;

    public void Execute(string triggeredBy)
    {
        var target = MultiplayerTargeting.Resolve(triggeredBy);
        target.Stamina = target.MaxStamina;
        MultiplayerTargeting.Notify(target,
            $"✨ {triggeredBy} restored your energy to full!",
            HUDMessage.newQuest_type);
    }
}
