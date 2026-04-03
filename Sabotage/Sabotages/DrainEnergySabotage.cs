using StardewValley;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class DrainEnergySabotage : ISabotage
{
    public string Name         => "DrainEnergy";
    public string BuyCommand   => "drain";
    public string Description  => "drains 50 stamina";
    public int Cost            => 100;
    public int CooldownSeconds => 60;

    public void Execute(string triggeredBy)
    {
        Game1.player.Stamina = Math.Max(0, Game1.player.Stamina - 50f);
        Game1.addHUDMessage(new HUDMessage(
            $"⚡ {triggeredBy} drained your energy! -50 stamina.",
            HUDMessage.error_type));
    }
}
