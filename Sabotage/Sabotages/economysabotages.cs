using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Menus;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

// ─── NARCOLEPSY — random sleep emote every 30 seconds ────────────────────────

public class NarcolepsySabotage : ISabotage
{
    public string Name         => "Narcolepsy";
    public string BuyCommand   => "narcolepsy";
    public string Description  => "randomly forces a sleep emote every 30 seconds for 3 minutes";
    public int Cost            => 200;
    public int CooldownSeconds => 180;

    public static bool IsActive      { get; private set; }
    public static DateTime ExpiresAt { get; private set; }
    private static DateTime _nextSleep = DateTime.UtcNow;

    public void Execute(string triggeredBy)
    {
        IsActive  = true;
        ExpiresAt = DateTime.UtcNow.AddMinutes(3);
        _nextSleep = DateTime.UtcNow.AddSeconds(30);

        Game1.addHUDMessage(new HUDMessage(
            $"😴 {triggeredBy} gave you narcolepsy! Try not to fall asleep...",
            HUDMessage.error_type));
    }

    public static void Tick()
    {
        if (!IsActive) return;
        if (DateTime.UtcNow >= ExpiresAt) { IsActive = false; return; }
        if (DateTime.UtcNow < _nextSleep) return;

        _nextSleep = DateTime.UtcNow.AddSeconds(30);

        // Force sleep emote for 3 seconds
        Game1.player.jump();
        Game1.player.FarmerSprite.animateOnce(new FarmerSprite.AnimationFrame[] {
            new(24, 3000, false, false)
        });
        Game1.player.freezePause = 3000;
        Game1.playSound("yawn");

        Game1.addHUDMessage(new HUDMessage(
            "😴 zzz...",
            HUDMessage.error_type));
    }

    public static int SecsLeft =>
        IsActive ? Math.Max(0, (int)(ExpiresAt - DateTime.UtcNow).TotalSeconds) : 0;
}

// ─── STICKY — prevents item switching and dropping for 60 seconds ─────────────

public class StickySabotage : ISabotage
{
    public string Name         => "Sticky";
    public string BuyCommand   => "sticky";
    public string Description  => "prevents item switching and dropping for 60 seconds";
    public int Cost            => 175;
    public int CooldownSeconds => 120;

    public static bool IsActive      { get; private set; }
    public static DateTime ExpiresAt { get; private set; }

    public void Execute(string triggeredBy)
    {
        IsActive  = true;
        ExpiresAt = DateTime.UtcNow.AddSeconds(60);

        Game1.addHUDMessage(new HUDMessage(
            $"🍯 {triggeredBy} made your hands sticky! No switching items for 60 seconds!",
            HUDMessage.error_type));
    }

    public static void Tick()
    {
        if (IsActive && DateTime.UtcNow >= ExpiresAt)
        {
            IsActive = false;
            Game1.addHUDMessage(new HUDMessage(
                "🍯 Your hands are no longer sticky.",
                HUDMessage.newQuest_type));
        }
    }

    public static int SecsLeft =>
        IsActive ? Math.Max(0, (int)(ExpiresAt - DateTime.UtcNow).TotalSeconds) : 0;
}

// ─── INFLATION — random price multiplier on in-game shops ────────────────────

public class InflationSabotage : ISabotage
{
    public string Name         => "Inflation";
    public string BuyCommand   => "inflation";
    public string Description  => "randomly multiplies shop prices for the rest of the day";
    public int Cost            => 250;
    public int CooldownSeconds => 600;

    private static readonly Random _rng = new();

    public static float Multiplier { get; private set; } = 1f;
    public static bool IsActive    { get; private set; }

    public void Execute(string triggeredBy)
    {
        // Random multiplier between 1.5x and 4x
        Multiplier = 1.5f + (float)(_rng.NextDouble() * 2.5f);
        IsActive   = true;

        int pct = (int)((Multiplier - 1f) * 100);
        Game1.addHUDMessage(new HUDMessage(
            $"📈 {triggeredBy} caused inflation! Shop prices are {pct}% more expensive today!",
            HUDMessage.error_type));
    }

    public static void OnDayStarted() { IsActive = false; Multiplier = 1f; }
}

// ─── DEBT COLLECTOR — steals a random non-tool, non-quest item ───────────────

public class DebtCollectorSabotage : ISabotage
{
    public string Name         => "Debt Collector";
    public string BuyCommand   => "debtcollector";
    public string Description  => "steals a random item from your inventory";
    public int Cost            => 300;
    public int CooldownSeconds => 120;

    // Categories to protect (seeds, crops, forage, etc.)
    private static readonly HashSet<int> SafeCategories = new()
    {
        -20,  // Junk
        -8,   // Crafting
        -99,  // Mixed Seeds
        -74,  // Seeds
        -75,  // Vegetables
        -79,  // Fruit
        -80,  // Flower
        -81,  // Greens
        -28,  // Monster Loot
    };

    private static readonly Random _rng = new();

    public void Execute(string triggeredBy)
    {
        var eligible = new List<(StardewValley.Item item, int idx)>();
        for (int i = 0; i < Game1.player.Items.Count; i++)
        {
            var item = Game1.player.Items[i];
            if (item == null) continue;

            // Skip tools — this catches Axe, Pickaxe, Hoe, WateringCan, FishingRod, Scythe, etc.
            if (item is StardewValley.Tool) continue;

            // Skip quest items
            if (item is StardewValley.Object obj && obj.questItem.Value) continue;

            // Skip by negative category (seeds, crops, etc.)
            if (SafeCategories.Contains(item.Category)) continue;

            eligible.Add((item, i));
        }

        if (eligible.Count == 0)
        {
            Game1.addHUDMessage(new HUDMessage(
                $"💼 {triggeredBy} sent the Debt Collector but your inventory is already bare!",
                HUDMessage.error_type));
            return;
        }

        var chosen = eligible[_rng.Next(eligible.Count)];
        var name   = chosen.item.DisplayName;
        Game1.player.Items[chosen.idx] = null;

        Game1.addHUDMessage(new HUDMessage(
            $"💼 {triggeredBy} sent the Debt Collector! They took your {name}!",
            HUDMessage.error_type));
    }
}