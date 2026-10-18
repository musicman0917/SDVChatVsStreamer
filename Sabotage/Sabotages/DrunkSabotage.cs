using StardewValley;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class DrunkSabotage : ISabotage, ILocallyRelayable
{
    public string Name         => "Drunk";
    public string BuyCommand   => "drunk";
    public string Description  => "maximum tipsy effect for 60 seconds";
    public int Cost            => 200;
    public int CooldownSeconds => 180;

    public void Execute(string triggeredBy)
    {
        var target = MultiplayerTargeting.Resolve(triggeredBy);

        // Heavy tipsy — speed -3 for 60 seconds. This is host-authoritative, network-synced
        // Farmer state, so it already correctly lands on a farmhand target without any
        // relaying -- applying it again from ExecuteLocal on the receiving end would double it.
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

        // The screen glow only renders on whichever client calls it, so it's relayed to a
        // farmhand's own mod instance separately from the buff above -- if they don't have
        // the mod installed, they just don't get the glow, same as before this feature.
        if (target == Game1.player)
            ExecuteLocal(triggeredBy);
        else
            PeerRelay.TryRelay(target, BuyCommand, triggeredBy);

        MultiplayerTargeting.Notify(target,
            $"🍺 {triggeredBy} got you absolutely wasted! Speed -3 for 60 seconds.",
            HUDMessage.error_type);
    }

    /// <summary>Just the local screen-glow flourish -- the buff itself is applied directly
    /// in Execute() above via host-authoritative Farmer state, never through this.</summary>
    public void ExecuteLocal(string triggeredBy)
    {
        Game1.player.temporarilyInvincible = false;
        Game1.screenGlowOnce(Microsoft.Xna.Framework.Color.DarkBlue * 0.5f, false);
    }
}
