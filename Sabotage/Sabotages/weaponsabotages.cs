using StardewValley;
using StardewValley.GameData.Weapons;
using StardewValley.Tools;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

internal static class WeaponPool
{
    private static readonly Random _rng = new();

    public static List<string> GetAllWeaponIds()
    {
        var ids = new List<string>();
        try
        {
            var data = Game1.content.Load<Dictionary<string, WeaponData>>("Data/Weapons");
            foreach (var key in data.Keys)
            {
                // Exclude slingshots and scythes — not real combat weapons
                var item = ItemRegistry.Create($"(W){key}");
                if (item is MeleeWeapon) ids.Add($"(W){key}");
            }
        }
        catch { }
        return ids;
    }

    public static MeleeWeapon? PickFrom(IEnumerable<string> pool)
    {
        var list = pool.ToList();
        if (list.Count == 0) return null;
        for (int attempts = 0; attempts < 10; attempts++)
        {
            var weapon = ItemRegistry.Create(list[_rng.Next(list.Count)]) as MeleeWeapon;
            if (weapon != null) return weapon;
        }
        return null;
    }

    public static void GiveWeapon(MeleeWeapon? weapon, string triggeredBy, string emoji)
    {
        if (weapon == null)
        {
            Game1.addHUDMessage(new HUDMessage(
                $"{emoji} {triggeredBy} tried to give you a weapon but something went wrong!",
                HUDMessage.error_type));
            return;
        }

        if (Game1.player.addItemToInventory(weapon) == null)
            Game1.addHUDMessage(new HUDMessage(
                $"{emoji} {triggeredBy} gave you a {weapon.DisplayName}!",
                HUDMessage.newQuest_type));
        else
            Game1.addHUDMessage(new HUDMessage(
                $"{emoji} {triggeredBy} tried to give you a {weapon.DisplayName} but your inventory is full!",
                HUDMessage.error_type));
    }
}

// ─── Normal ─────────────────────────────────────────────────── early game ───

public class WeaponSabotageNormal : ISabotage
{
    public string Name         => "RandomWeaponNormal";
    public string BuyCommand   => "weaponnormal";
    public string Description  => "gives you a random normal-tier weapon";
    public int Cost            => 125;
    public int CooldownSeconds => 120;

    // Rusty, Wooden, Wood Club, Wood Mallet, Silver Saber, Iron Dirk,
    // Iron Edge, Elf Blade, Pirate's Sword, Cutlass, Bone Sword, Femur, Kudgel
    private static readonly string[] Pool = {
        "(W)0",  // Rusty Sword
        "(W)12", // Wooden Blade
        "(W)24", // Wood Club
        "(W)27", // Wood Mallet
        "(W)1",  // Silver Saber
        "(W)17", // Iron Dirk
        "(W)6",  // Iron Edge
        "(W)20", // Elf Blade
        "(W)43", // Pirate's Sword
        "(W)44", // Cutlass
        "(W)5",  // Bone Sword
        "(W)31", // Femur
        "(W)46", // Kudgel
    };

    public void Execute(string triggeredBy) =>
        WeaponPool.GiveWeapon(WeaponPool.PickFrom(Pool), triggeredBy, "⚔️");
}

// ─── Random (ALL melee weapons including mods) ────────────────────────────────

public class WeaponSabotageRandom : ISabotage
{
    public string Name         => "RandomWeapon";
    public string BuyCommand   => "weaponrandom";
    public string Description  => "gives you a truly random weapon — every weapon in the game including mods";
    public int Cost            => 175;
    public int CooldownSeconds => 120;

    public void Execute(string triggeredBy)
    {
        var all = WeaponPool.GetAllWeaponIds();
        WeaponPool.GiveWeapon(WeaponPool.PickFrom(all), triggeredBy, "🎲");
    }
}

// ─── Better ─────────────────────────────────────────────────── mid game ─────

public class WeaponSabotageBetter : ISabotage
{
    public string Name         => "RandomWeaponBetter";
    public string BuyCommand   => "weaponbetter";
    public string Description  => "gives you a random mid-tier weapon";
    public int Cost            => 250;
    public int CooldownSeconds => 120;

    // Steel Smallsword, Claymore, Dark Sword, Forest Sword, Shadow Dagger,
    // Crystal Dagger, Wind Spire, Templar's Blade, Steel Falchion,
    // Tempered Broadsword, Rapier, Yeti Tooth, Neptune's Glaive, Insect Head
    private static readonly string[] Pool = {
        "(W)11", // Steel Smallsword
        "(W)10", // Claymore
        "(W)2",  // Dark Sword
        "(W)15", // Forest Sword
        "(W)19", // Shadow Dagger
        "(W)21", // Crystal Dagger
        "(W)22", // Wind Spire
        "(W)7",  // Templar's Blade
        "(W)50", // Steel Falchion
        "(W)52", // Tempered Broadsword
        "(W)49", // Rapier
        "(W)48", // Yeti Tooth
        "(W)14", // Neptune's Glaive
        "(W)13", // Insect Head
    };

    public void Execute(string triggeredBy) =>
        WeaponPool.GiveWeapon(WeaponPool.PickFrom(Pool), triggeredBy, "⚔️");
}

// ─── Epic ────────────────────────────────────────────────────── late game ────

public class WeaponSabotageEpic : ISabotage
{
    public string Name         => "RandomWeaponEpic";
    public string BuyCommand   => "weaponepic";
    public string Description  => "gives you a random epic-tier weapon";
    public int Cost            => 450;
    public int CooldownSeconds => 180;

    // Obsidian Edge, Lava Katana, Holy Blade, Ossified Blade,
    // Dragontooth Cutlass, Dragontooth Club, Dragontooth Shiv,
    // Dwarf Sword, Dwarf Hammer, Dwarf Dagger, Broken Trident,
    // Galaxy Sword, Galaxy Dagger, Galaxy Hammer, Iridium Needle
    private static readonly string[] Pool = {
        "(W)8",  // Obsidian Edge
        "(W)9",  // Lava Katana
        "(W)3",  // Holy Blade
        "(W)60", // Ossified Blade
        "(W)57", // Dragontooth Cutlass
        "(W)58", // Dragontooth Club
        "(W)59", // Dragontooth Shiv
        "(W)54", // Dwarf Sword
        "(W)55", // Dwarf Hammer
        "(W)56", // Dwarf Dagger
        "(W)51", // Broken Trident
        "(W)4",  // Galaxy Sword
        "(W)23", // Galaxy Dagger
        "(W)29", // Galaxy Hammer
        "(W)61", // Iridium Needle
    };

    public void Execute(string triggeredBy) =>
        WeaponPool.GiveWeapon(WeaponPool.PickFrom(Pool), triggeredBy, "✨");
}

// ─── Legendary ───────────────────────────────────────────────── end game ─────

public class WeaponSabotageLegendary : ISabotage
{
    public string Name         => "RandomWeaponLegendary";
    public string BuyCommand   => "weaponlegendary";
    public string Description  => "gives you a legendary weapon — Infinity or Meowmere";
    public int Cost            => 600;
    public int CooldownSeconds => 300;

    // Infinity Blade, Infinity Gavel, Infinity Dagger, Meowmere
    private static readonly string[] Pool = {
        "(W)62", // Infinity Blade
        "(W)63", // Infinity Gavel
        "(W)64", // Infinity Dagger
        "(W)65", // Meowmere
    };

    public void Execute(string triggeredBy) =>
        WeaponPool.GiveWeapon(WeaponPool.PickFrom(Pool), triggeredBy, "🌟");
}