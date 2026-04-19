using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.GameData.Objects;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

// ─── TAX MAN — removes 10-25% of current gold ────────────────────────────────

public class TaxManSabotage : ISabotage
{
    public string Name         => "Tax Man";
    public string BuyCommand   => "taxman";
    public string Description  => "removes 10-25% of your current gold";
    public int Cost            => 200;
    public int CooldownSeconds => 180;

    private static readonly Random _rng = new();

    public void Execute(string triggeredBy)
    {
        if (Game1.player.Money <= 0)
        {
            Game1.addHUDMessage(new HUDMessage(
                $"💸 {triggeredBy} sent the Tax Man but you're already broke!",
                HUDMessage.error_type));
            return;
        }

        int pct    = _rng.Next(10, 26);
        int amount = (int)(Game1.player.Money * (pct / 100.0));
        Game1.player.Money -= amount;

        Game1.addHUDMessage(new HUDMessage(
            $"💸 {triggeredBy} sent the Tax Man! Lost {amount}g ({pct}% tax)!",
            HUDMessage.error_type));
    }
}

// ─── SUGAR RUSH — max energy + speed boost ───────────────────────────────────

public class SugarRushBlessing : ISabotage
{
    public string Name         => "Sugar Rush";
    public string BuyCommand   => "sugarrush";
    public string Description  => "max energy and a speed boost for 30 seconds";
    public int Cost            => 150;
    public int CooldownSeconds => 180;

    public void Execute(string triggeredBy)
    {
        Game1.player.Stamina = Game1.player.MaxStamina;

        Game1.player.buffs.Apply(new Buff(
            id:            "CVS_SugarRush",
            source:        "Chat vs Streamer",
            displaySource: triggeredBy,
            duration:      30000,
            effects:       new StardewValley.Buffs.BuffEffects() { Speed = { 2 } },
            displayName:   "Sugar Rush",
            description:   "Chat gave you a sugar rush!"));

        Game1.addHUDMessage(new HUDMessage(
            $"⚡ {triggeredBy} used Sugar Rush! Max energy + speed boost!",
            HUDMessage.newQuest_type));
    }
}

// ─── GIFTING TREE — random item from Data/Objects ────────────────────────────

public class GiftingTreeBlessing : ISabotage
{
    public string Name         => "Gifting Tree";
    public string BuyCommand   => "giftingtree";
    public string Description  => "spawns a random item into your inventory";
    public int Cost            => 175;
    public int CooldownSeconds => 120;

    private static readonly Random _rng = new();

    // Categories and item IDs to exclude
    private static readonly HashSet<int> ExcludedCategories = new()
    {
        -8,   // Crafting
        -16,  // Building resources (some unobtainable)
        -74,  // Seeds (avoid flooding inventory)
    };

    private static readonly HashSet<string> ExcludedIds = new()
    {
        "79",  // Secret Note
        "454", // Ancient Fruit (seed form causes issues)
        "289", // Ostrich Egg (can only hatch)
        "GoldenEgg",
        "MysteryBox", // Already a separate effect
    };

    public void Execute(string triggeredBy)
    {
        try
        {
            var data = Game1.content.Load<Dictionary<string, ObjectData>>("Data/Objects");

            var eligible = data
                .Where(kvp =>
                    !ExcludedIds.Contains(kvp.Key) &&
                    !ExcludedCategories.Contains(kvp.Value.Category) &&
                    kvp.Value.ExcludeFromShippingCollection == false)
                .ToList();

            if (eligible.Count == 0)
            {
                Game1.addHUDMessage(new HUDMessage("Gifting Tree found nothing to give!", HUDMessage.error_type));
                return;
            }

            var chosen = eligible[_rng.Next(eligible.Count)];
            var item   = ItemRegistry.Create($"(O){chosen.Key}");

            if (Game1.player.addItemToInventory(item) == null)
                Game1.addHUDMessage(new HUDMessage(
                    $"🎁 {triggeredBy} used Gifting Tree! You received: {item.DisplayName}!",
                    HUDMessage.newQuest_type));
            else
                Game1.addHUDMessage(new HUDMessage(
                    $"🎁 {triggeredBy} used Gifting Tree but your inventory is full!",
                    HUDMessage.error_type));
        }
        catch
        {
            Game1.addHUDMessage(new HUDMessage("Gifting Tree failed to find an item!", HUDMessage.error_type));
        }
    }
}