using StardewValley;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class StealSabotage : ISabotage
{
    public string Name         => "Steal";
    public string BuyCommand   => "steal";
    public string Description  => "removes a random item from inventory";
    public int Cost            => 200;
    public int CooldownSeconds => 120;

    private readonly Random _rng = new();

    public void Execute(string triggeredBy)
    {
        var target = MultiplayerTargeting.Resolve(triggeredBy);
        var inventory = target.Items
            .Select((item, idx) => (item, idx))
            .Where(x => x.item != null)
            .ToList();

        if (inventory.Count == 0)
        {
            MultiplayerTargeting.Notify(target,
                $"🦝 {triggeredBy} tried to steal but your inventory is empty!",
                HUDMessage.newQuest_type);
            return;
        }

        var (item, idx) = inventory[_rng.Next(inventory.Count)];
        var itemName    = item.DisplayName;
        target.Items[idx] = null;

        MultiplayerTargeting.Notify(target,
            $"🦝 {triggeredBy} stole your {itemName}!",
            HUDMessage.error_type);
    }
}
