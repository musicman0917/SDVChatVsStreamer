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

// ─── BANKRUPTCY — sets gold to 1g ────────────────────────────────────────────

public class BankruptcySabotage : ISabotage
{
    public string Name         => "Bankruptcy";
    public string BuyCommand   => "bankruptcy";
    public string Description  => "sets your gold to 1g. Just 1.";
    public int Cost            => 5000;
    public int CooldownSeconds => 600;

    private static readonly string[] Messages = {
        "have filed you for bankruptcy. You have 1g to your name. Spend it wisely.",
        "have consulted with your financial advisor. The news is not good. You have 1g.",
        "have reviewed your portfolio. It's giving 1g.",
        "have declared you officially broke. Pierre won't even look at you. You have 1g.",
        "have liquidated your assets. All of them. You have 1g remaining.",
        "have spoken with your accountant. You're down to 1g. Maybe sell a parsnip.",
        "have restructured your finances. The new balance is 1g. Good luck out there.",
        "have notified the Ferngill Revenue Service. They have taken everything. You have 1g.",
        "have filed a report with the FRS. An audit has been completed. Balance: 1g.",
        "have contacted the FRS. They say this is actually an improvement. You have 1g.",
        "have alerted the Ferngill Revenue Service. They said to tell you: 'You're welcome.' You have 1g.",
        "have submitted your tax return to the FRS. The refund is 0g. You owe 1g. Net balance: 1g.",
    };

    private static readonly Random _rng = new();

    public void Execute(string triggeredBy)
    {
        Game1.player.Money = 1;
        var msg = Messages[_rng.Next(Messages.Length)];
        Game1.addHUDMessage(new HUDMessage(
            $"💸 {triggeredBy} {msg}",
            HUDMessage.error_type));
    }
}

// ─── POLTERGEIST — shifts all furniture one tile in a random direction ────────

public class PoltergeistSabotage : ISabotage
{
    public string Name         => "Poltergeist";
    public string BuyCommand   => "poltergeist";
    public string Description  => "shifts all furniture in the farmhouse one tile randomly";
    public int Cost            => 175;
    public int CooldownSeconds => 120;

    private static readonly Random _rng = new();
    private static readonly Vector2[] Directions = {
        new(-1, 0), new(1, 0), new(0, -1), new(0, 1)
    };

    public void Execute(string triggeredBy)
    {
        var house = Game1.getLocationFromName("FarmHouse");
        var furniture = house.furniture.ToList();
        var occupied  = new HashSet<Vector2>(furniture.Select(f => f.TileLocation));
        int moved     = 0;

        // Shuffle order so we don't always move the same piece first
        furniture = furniture.OrderBy(_ => _rng.Next()).ToList();

        foreach (var f in furniture)
        {
            var dirs = Directions.OrderBy(_ => _rng.Next()).ToList();
            foreach (var dir in dirs)
            {
                var newPos = f.TileLocation + dir;

                // Check bounds and no overlap
                if (newPos.X < 0 || newPos.Y < 0) continue;
                if (occupied.Contains(newPos)) continue;

                occupied.Remove(f.TileLocation);
                f.TileLocation = newPos;
                f.boundingBox.X = (int)newPos.X * 64;
                f.boundingBox.Y = (int)newPos.Y * 64;
                occupied.Add(newPos);
                moved++;
                break;
            }
        }

        Game1.addHUDMessage(new HUDMessage(
            $"👻 {triggeredBy} sent a poltergeist! {moved} pieces of furniture moved!",
            HUDMessage.error_type));
    }
}