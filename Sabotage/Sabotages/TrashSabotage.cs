using StardewValley;
using StardewValley.Objects;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class TrashSabotage : ISabotage
{
    public string Name         => "Trash";
    public string BuyCommand   => "trash";
    public string Description  => "fills a random inventory slot with trash";
    public int Cost            => 75;
    public int CooldownSeconds => 60;

    private readonly Random _rng = new();

    // Trash item IDs in Stardew Valley 1.6 (string format)
    private static readonly string[] TrashIds = { "168", "169", "170", "171", "172", "216" };

    public void Execute(string triggeredBy)
    {
        // Find an empty slot
        var emptySlots = Enumerable.Range(0, Game1.player.Items.Count)
            .Where(i => Game1.player.Items[i] == null)
            .ToList();

        if (emptySlots.Count == 0)
        {
            Game1.addHUDMessage(new HUDMessage(
                $"🗑️ {triggeredBy} tried to fill your inventory with trash but it's already full!",
                HUDMessage.newQuest_type));
            return;
        }

        int slot       = emptySlots[_rng.Next(emptySlots.Count)];
        string trashId = TrashIds[_rng.Next(TrashIds.Length)];

        Game1.player.Items[slot] = new StardewValley.Object(trashId, 1);

        Game1.addHUDMessage(new HUDMessage(
            $"🗑️ {triggeredBy} put trash in your inventory!",
            HUDMessage.error_type));
    }
}