using StardewValley;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class DizzySabotage : ISabotage
{
    public string Name         => "Dizzy";
    public string BuyCommand   => "dizzy";
    public string Description  => "applies the tipsy debuff";
    public int Cost            => 100;
    public int CooldownSeconds => 90;

    public void Execute(string triggeredBy)
    {
        // Tipsy debuff — speed -1 for 30 seconds (30000ms)
        Game1.player.buffs.Apply(new Buff(
            id:          "CVS_Tipsy",
            source:      "Chat vs Streamer",
            displaySource: triggeredBy,
            duration:    30000,
            effects:     new StardewValley.Buffs.BuffEffects()
            {
                Speed = { -1 }
            },
            displayName: "Tipsy",
            description: $"{triggeredBy} made you dizzy!"
        ));

        Game1.addHUDMessage(new HUDMessage(
            $"🥴 {triggeredBy} made you dizzy! Speed -1 for 30 seconds.",
            HUDMessage.error_type));
    }
}
