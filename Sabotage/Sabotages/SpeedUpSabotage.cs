using StardewValley;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class SpeedUpSabotage : ISabotage
{
    public string Name         => "SpeedUp";
    public string BuyCommand   => "speedup";
    public string Description  => "increases your movement speed for 5 minutes";
    public int Cost            => 100;
    public int CooldownSeconds => 120;

    public void Execute(string triggeredBy)
    {
        var target = MultiplayerTargeting.Resolve(triggeredBy);

        target.buffs.Apply(new Buff(
            id:            "CVS_SpeedUp",
            source:        "Chat vs Streamer",
            displaySource: triggeredBy,
            duration:      300000, // 5 minutes
            effects:       new StardewValley.Buffs.BuffEffects() { Speed = { 2 } },
            displayName:   "Speed Up",
            description:   $"{triggeredBy} sped you up!"));

        MultiplayerTargeting.Notify(target,
            $"⏩ {triggeredBy} sped you up! +2 movement speed for 5 minutes.",
            HUDMessage.error_type);
    }
}
