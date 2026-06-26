using StardewValley;
using StardewValley.Characters;
using SDVChatVsStreamer.UI;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

// ─── RENAME ANIMAL — renames a random farm animal ────────────────────────────

public class RenameAnimalSabotage : ISabotage
{
    public string Name         => "Rename Animal";
    public string BuyCommand   => "renameanimal";
    public string Description  => "renames a random farm animal (!buy renameanimal <name>)";
    public int Cost            => 150;
    public int CooldownSeconds => 60;

    public string? Validate(string args = "")
    {
        if (string.IsNullOrWhiteSpace(args))
            return "You need to provide a name! Usage: !buy renameanimal <name>";
        if (args.Length > 20)
            return "Name too long — max 20 characters!";
        if (ContentFilter.IsBlocked(args, Array.Empty<string>()))
            return "That name isn't allowed. Try something else!";

        var farm    = Game1.getFarm();
        var animals = farm.animals.Values.ToList();
        if (animals.Count == 0)
            return "There are no animals on the farm to rename!";

        return null;
    }

    public void Execute(string triggeredBy) => ExecuteWithArgs(triggeredBy, "");

    public void ExecuteWithArgs(string triggeredBy, string args)
    {
        var farm    = Game1.getFarm();
        var animals = farm.animals.Values.ToList();
        var rng     = new Random();
        var animal  = animals[rng.Next(animals.Count)];
        var oldName = animal.Name;

        animal.Name        = args;
        animal.displayName = args;

        Game1.addHUDMessage(new HUDMessage(
            $"🐄 {triggeredBy} renamed your {animal.type.Value} '{oldName}' to '{args}'!",
            HUDMessage.error_type));
    }
}

// ─── RENAME PET — renames the pet ────────────────────────────────────────────

public class RenamePetSabotage : ISabotage
{
    public string Name         => "Rename Pet";
    public string BuyCommand   => "renamepet";
    public string Description  => "renames your pet (!buy renamepet <name>)";
    public int Cost            => 100;
    public int CooldownSeconds => 60;

    public string? Validate(string args = "")
    {
        if (string.IsNullOrWhiteSpace(args))
            return "You need to provide a name! Usage: !buy renamepet <name>";
        if (args.Length > 20)
            return "Name too long — max 20 characters!";
        if (ContentFilter.IsBlocked(args, Array.Empty<string>()))
            return "That name isn't allowed. Try something else!";

        var pet = Game1.getFarm().characters.OfType<Pet>().FirstOrDefault();
        if (pet == null)
            return "No pet found on the farm!";

        return null;
    }

    public void Execute(string triggeredBy) => ExecuteWithArgs(triggeredBy, "");

    public void ExecuteWithArgs(string triggeredBy, string args)
    {
        var pet = Game1.getFarm().characters.OfType<Pet>().FirstOrDefault();
        if (pet == null) return;

        var oldName     = pet.Name;
        pet.Name        = args;
        pet.displayName = args;

        Game1.addHUDMessage(new HUDMessage(
            $"🐾 {triggeredBy} renamed your {pet.petType.Value} '{oldName}' to '{args}'!",
            HUDMessage.newQuest_type));
    }
}