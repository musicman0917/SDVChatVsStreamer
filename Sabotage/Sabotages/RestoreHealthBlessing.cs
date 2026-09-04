using StardewValley;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class RestoreHealthBlessing : ISabotage
{
    public string Name         => "RestoreHealth";
    public string BuyCommand   => "restorehealth";
    public string Description  => "restores full health";
    public int Cost            => 100;
    public int CooldownSeconds => 120;
    public SabotageTier Tier   => SabotageTier.Blessing;

    public void Execute(string triggeredBy)
    {
        var target = MultiplayerTargeting.Resolve(triggeredBy);
        target.health = target.maxHealth;
        MultiplayerTargeting.Notify(target,
            $"💚 {triggeredBy} restored your health to full!",
            HUDMessage.newQuest_type);
    }
}
