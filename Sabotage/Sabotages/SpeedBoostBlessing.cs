using StardewValley;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class SpeedBoostBlessing : ISabotage
{
    public string Name         => "SpeedBoost";
    public string BuyCommand   => "speedboost";
    public string Description  => "speed +2 for 60 seconds";
    public int Cost            => 100;
    public int CooldownSeconds => 180;
    public SabotageTier Tier   => SabotageTier.Blessing;

    public void Execute(string triggeredBy)
    {
        var target = MultiplayerTargeting.Resolve(triggeredBy);
        target.buffs.Apply(new Buff(
            id:            "CVS_SpeedBoost",
            source:        "Chat vs Streamer",
            displaySource: triggeredBy,
            duration:      60000,
            effects:       new StardewValley.Buffs.BuffEffects() { Speed = { 2 } },
            displayName:   "Speed Boost",
            description:   $"{triggeredBy} gave you a speed boost!"));

        MultiplayerTargeting.Notify(target,
            $"⚡ {triggeredBy} gave you a speed boost! Speed +2 for 60 seconds.",
            HUDMessage.newQuest_type);
    }
}
