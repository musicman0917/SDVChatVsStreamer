using StardewValley;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class BrokeSabotage : ISabotage
{
    public string Name         => "Broke";
    public string BuyCommand   => "broke";
    public string Description  => "removes a random amount of gold";
    public int Cost            => 150;
    public int CooldownSeconds => 120;

    private readonly Random _rng = new();

    public void Execute(string triggeredBy)
    {
        // Steal between 100-500g, but not more than they have
        int maxSteal = Math.Min(Game1.player.Money, 500);
        if (maxSteal <= 0)
        {
            Game1.addHUDMessage(new HUDMessage(
                $"💸 {triggeredBy} tried to take your gold but you're already broke!",
                HUDMessage.newQuest_type));
            return;
        }

        int stolen       = _rng.Next(100, maxSteal + 1);
        Game1.player.Money -= stolen;

        Game1.addHUDMessage(new HUDMessage(
            $"💸 {triggeredBy} stole {stolen}g from you!",
            HUDMessage.error_type));
    }
}
