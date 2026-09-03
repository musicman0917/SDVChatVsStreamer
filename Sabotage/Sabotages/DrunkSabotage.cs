using StardewValley;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class DrunkSabotage : ISabotage
{
    public string Name         => "Drunk";
    public string BuyCommand   => "drunk";
    public string Description  => "maximum tipsy effect for 60 seconds";
    public int Cost            => 200;
    public int CooldownSeconds => 180;

    public void Execute(string triggeredBy)
    {
        var target = MultiplayerTargeting.Resolve(triggeredBy);

        // Heavy tipsy — speed -3 for 60 seconds
        target.buffs.Apply(new Buff(
            id:          "CVS_Drunk",
            source:      "Chat vs Streamer",
            displaySource: triggeredBy,
            duration:    60000,
            effects:     new StardewValley.Buffs.BuffEffects()
            {
                Speed = { -3 }
            },
            displayName: "Wasted",
            description: $"{triggeredBy} got you absolutely hammered!"
        ));

        // Screen glow only renders on whichever client calls it — meaningless to fire
        // for a farmhand target since this code only runs on the host's screen.
        if (target == Game1.player)
        {
            Game1.player.temporarilyInvincible = false;
            Game1.screenGlowOnce(Microsoft.Xna.Framework.Color.DarkBlue * 0.5f, false);
        }

        MultiplayerTargeting.Notify(target,
            $"🍺 {triggeredBy} got you absolutely wasted! Speed -3 for 60 seconds.",
            HUDMessage.error_type);
    }
}
