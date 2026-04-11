using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Buildings;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

// ─── TRICK ROOM — jumps clock forward 60 minutes ─────────────────────────────

public class TrickRoomSabotage : ISabotage
{
    public string Name         => "Trick Room";
    public string BuyCommand   => "trickroom";
    public string Description  => "jumps the clock forward 60 minutes";
    public int Cost            => 150;
    public int CooldownSeconds => 120;

    public void Execute(string triggeredBy)
    {
        Game1.timeOfDay = Math.Min(Game1.timeOfDay + 100, 2600);
        Game1.addHUDMessage(new HUDMessage(
            $"⏰ {triggeredBy} used Trick Room! Time jumped forward!",
            HUDMessage.error_type));
    }
}

// ─── METRONOME — shuffles hotbar items ───────────────────────────────────────

public class MetronomeSabotage : ISabotage
{
    public string Name         => "Metronome";
    public string BuyCommand   => "metronome";
    public string Description  => "randomly rearranges your hotbar";
    public int Cost            => 100;
    public int CooldownSeconds => 90;

    private static readonly Random _rng = new();

    public void Execute(string triggeredBy)
    {
        var hotbar = Game1.player.Items.Take(12).ToList();
        var shuffled = hotbar.OrderBy(_ => _rng.Next()).ToList();
        for (int i = 0; i < 12; i++)
            Game1.player.Items[i] = shuffled[i];

        Game1.addHUDMessage(new HUDMessage(
            $"🎵 {triggeredBy} used Metronome! Your hotbar is shuffled!",
            HUDMessage.error_type));
    }
}

// ─── LUCKY CHANT — sets daily luck to maximum ────────────────────────────────

public class LuckyChantBlessing : ISabotage
{
    public string Name         => "Lucky Chant";
    public string BuyCommand   => "luckychant";
    public string Description  => "sets your daily luck to maximum";
    public int Cost            => 150;
    public int CooldownSeconds => 300;

    public void Execute(string triggeredBy)
    {
        Game1.player.team.sharedDailyLuck.Value = 0.115;
        Game1.addHUDMessage(new HUDMessage(
            $"🍀 {triggeredBy} used Lucky Chant! Today is your lucky day!",
            HUDMessage.newQuest_type));
    }
}

// ─── PAY DAY — fills empty slots with useful items ───────────────────────────

public class PayDayBlessing : ISabotage
{
    public string Name         => "Pay Day";
    public string BuyCommand   => "payday";
    public string Description  => "fills empty inventory slots with useful items";
    public int Cost            => 200;
    public int CooldownSeconds => 180;

    private static readonly string[] Pool = {
        "(O)388",  // Wood
        "(O)390",  // Stone
        "(O)709",  // Hardwood
        "(O)378",  // Copper Ore
        "(O)380",  // Iron Ore
        "(O)432",  // Truffle
        "(O)724",  // Maple Syrup
        "(O)340",  // Honey
        "(O)466",  // Speed-Gro
        "(O)685",  // Bait
    };

    private static readonly Random _rng = new();

    public void Execute(string triggeredBy)
    {
        int filled = 0;
        for (int i = 0; i < Game1.player.Items.Count; i++)
        {
            if (Game1.player.Items[i] == null)
            {
                var id = Pool[_rng.Next(Pool.Length)];
                Game1.player.Items[i] = ItemRegistry.Create(id, _rng.Next(5, 20));
                filled++;
            }
        }

        Game1.addHUDMessage(new HUDMessage(
            $"💰 {triggeredBy} used Pay Day! {filled} slots filled with goodies!",
            HUDMessage.newQuest_type));
    }
}

// ─── TRASH DAY — fills empty slots with trash ────────────────────────────────

public class TrashDaySabotage : ISabotage
{
    public string Name         => "Trash Day";
    public string BuyCommand   => "trashday";
    public string Description  => "fills empty inventory slots with trash";
    public int Cost            => 75;
    public int CooldownSeconds => 90;

    private static readonly string[] TrashPool = {
        "(O)168",  // Trash
        "(O)172",  // Soggy Newspaper
        "(O)171",  // Broken CD
        "(O)170",  // Broken Glasses
        "(O)167",  // Joja Cola
        "(O)169",  // Driftwood
    };

    private static readonly Random _rng = new();

    public void Execute(string triggeredBy)
    {
        int filled = 0;
        for (int i = 0; i < Game1.player.Items.Count; i++)
        {
            if (Game1.player.Items[i] == null)
            {
                var id = TrashPool[_rng.Next(TrashPool.Length)];
                Game1.player.Items[i] = ItemRegistry.Create(id, _rng.Next(1, 5));
                filled++;
            }
        }

        Game1.addHUDMessage(new HUDMessage(
            $"🗑️ {triggeredBy} used Trash Day! {filled} slots filled with junk!",
            HUDMessage.error_type));
    }
}

// ─── SOAK — turns off all sprinklers for the day ─────────────────────────────

public class SoakSabotage : ISabotage
{
    public string Name         => "Soak";
    public string BuyCommand   => "soak";
    public string Description  => "turns off all sprinklers — crops won't be watered today";
    public int Cost            => 175;
    public int CooldownSeconds => 180;

    public void Execute(string triggeredBy)
    {
        var farm = Game1.getFarm();
        int count = 0;
        foreach (var obj in farm.objects.Values)
        {
            // Sprinklers have category -19
            if (obj.Category == -19)
            {
                obj.uses.Value = 999; // Mark as already used so it won't water
                count++;
            }
        }

        Game1.addHUDMessage(new HUDMessage(
            $"💧 {triggeredBy} used Soak! {count} sprinklers disabled for today!",
            HUDMessage.error_type));
    }
}

// ─── TELEPORT — warps to a random NPC location ───────────────────────────────

public class TeleportSabotage : ISabotage
{
    public string Name         => "Teleport";
    public string BuyCommand   => "teleport";
    public string Description  => "warps you to a random NPC's location";
    public int Cost            => 125;
    public int CooldownSeconds => 90;

    private static readonly string[] NPCs = {
        "Abigail", "Alex", "Elliott", "Emily", "Haley",
        "Harvey", "Leah", "Maru", "Penny", "Sam",
        "Sebastian", "Shane", "Robin", "Willy", "Linus"
    };

    private static readonly Random _rng = new();

    public void Execute(string triggeredBy)
    {
        var npcName = NPCs[_rng.Next(NPCs.Length)];
        var npc = Game1.getCharacterFromName(npcName);

        if (npc != null)
        {
            Game1.warpFarmer(npc.currentLocation.Name,
                (int)npc.Tile.X, (int)npc.Tile.Y + 1, false);
            Game1.addHUDMessage(new HUDMessage(
                $"✨ {triggeredBy} used Teleport! Warped to {npcName}!",
                HUDMessage.error_type));
        }
        else
        {
            // Fallback to random town location
            var dests = new[] { ("Town", 50, 80), ("Beach", 30, 5), ("Mountain", 30, 20) };
            var dest = dests[_rng.Next(dests.Length)];
            Game1.warpFarmer(dest.Item1, dest.Item2, dest.Item3, false);
            Game1.addHUDMessage(new HUDMessage(
                $"✨ {triggeredBy} used Teleport! You were warped away!",
                HUDMessage.error_type));
        }
    }
}