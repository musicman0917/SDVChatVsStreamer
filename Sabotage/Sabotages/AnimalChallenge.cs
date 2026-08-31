using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

/// <summary>
/// Shared state/helpers for the togglable "N animal challenge" (e.g. the 100 Chicken
/// Challenge) — counts matching farm animals against a goal, and backs the
/// help/hurt shop commands that let chat move the counter.
/// </summary>
public static class AnimalChallengeState
{
    private static ModConfig? _config;

    public static void Init(ModConfig config) => _config = config;

    public static bool IsEnabled => _config?.ChallengeModeEnabled ?? false;
    public static int  GoalCount => _config?.ChallengeGoalCount ?? 100;
    public static string FilterLabel => string.IsNullOrWhiteSpace(_config?.ChallengeAnimalFilter)
        ? "Animal" : _config!.ChallengeAnimalFilter.Trim();

    private static bool IsAnyFilter =>
        FilterLabel.Equals("Any", StringComparison.OrdinalIgnoreCase) ||
        FilterLabel.Equals("All", StringComparison.OrdinalIgnoreCase);

    public static bool Matches(FarmAnimal animal) =>
        IsAnyFilter || animal.type.Value.Contains(FilterLabel, StringComparison.OrdinalIgnoreCase);

    public static List<FarmAnimal> GetMatchingAnimals() =>
        Game1.getFarm().animals.Values.Where(Matches).ToList();

    public static int GetCount() => GetMatchingAnimals().Count;

    /// <summary>
    /// Picks a concrete in-game animal type string to spawn. Clones the type off an
    /// existing matching animal when one exists (guaranteed valid), since we can't
    /// spawn a literal "Any". Falls back to "White Chicken" — the one base type we
    /// don't need an existing animal to know is valid — when the challenge is
    /// chicken-flavored (or unfiltered) and nothing exists yet. Returns null if we
    /// can't safely guess a type (e.g. a "Cow" challenge with zero cows so far).
    /// </summary>
    public static string? ResolveSpawnType()
    {
        var existing = GetMatchingAnimals();
        if (existing.Count > 0)
            return existing[new Random().Next(existing.Count)].type.Value;

        if (IsAnyFilter || FilterLabel.Contains("Chicken", StringComparison.OrdinalIgnoreCase))
            return "White Chicken";

        return null;
    }

    private static bool IsCoopDwelling(string animalType) =>
        animalType.Contains("Chicken", StringComparison.OrdinalIgnoreCase) ||
        animalType.Contains("Duck", StringComparison.OrdinalIgnoreCase) ||
        animalType.Contains("Rabbit", StringComparison.OrdinalIgnoreCase) ||
        animalType.Contains("Dinosaur", StringComparison.OrdinalIgnoreCase) ||
        animalType.Contains("Ostrich", StringComparison.OrdinalIgnoreCase);

    private static bool HasRoom(Building building) =>
        building.GetIndoors() is AnimalHouse house && house.animalsThatLiveHere.Count < house.animalLimit.Value;

    /// <summary>Finds a coop/barn (matching the given animal type's family) with free space.</summary>
    public static Building? FindOpenBuilding(string animalType)
    {
        var farm = Game1.getFarm();

        // Prefer a building that already houses this exact type.
        var sibling = farm.animals.Values.FirstOrDefault(a => a.type.Value == animalType)?.home;
        if (sibling != null && HasRoom(sibling))
            return sibling;

        var wantCoop = sibling != null
            ? sibling.buildingType.Value.Contains("Coop", StringComparison.OrdinalIgnoreCase)
            : IsCoopDwelling(animalType);

        foreach (var building in farm.buildings)
        {
            var isCoopBuilding = building.buildingType.Value?.Contains("Coop", StringComparison.OrdinalIgnoreCase) ?? false;
            if (isCoopBuilding != wantCoop) continue;
            if (HasRoom(building)) return building;
        }
        return null;
    }

    /// <summary>
    /// Attempts to spawn a new animal into the given building. Wrapped by the caller
    /// so a bad assumption about the game's animal-housing API fails safely (logs +
    /// refuses) instead of crashing anything.
    /// </summary>
    public static bool TrySpawn(Building building, string animalType, out string? error)
    {
        error = null;
        try
        {
            var farm = Game1.getFarm();

            long id;
            do { id = Game1.random.NextInt64(); } while (farm.animals.ContainsKey(id));

            var animal = new FarmAnimal(animalType, id, Game1.player.UniqueMultiplayerID);

            animal.Position        = new Vector2(building.tileX.Value * 64f, building.tileY.Value * 64f);
            animal.home            = building;
            animal.currentLocation = farm;

            farm.animals.Add(animal.myID.Value, animal);
            if (building.GetIndoors() is AnimalHouse house)
                house.animalsThatLiveHere.Add(animal.myID.Value);

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>Removes an animal from the farm entirely (used by the "hurt" sabotage).</summary>
    public static void Remove(FarmAnimal animal)
    {
        var farm = Game1.getFarm();
        farm.animals.Remove(animal.myID.Value);

        if (animal.home?.GetIndoors() is AnimalHouse house)
            house.animalsThatLiveHere.Remove(animal.myID.Value);
    }
}

// ─── SPOOK ANIMAL — hurts challenge progress by removing a matching animal ────

public class SpookAnimalSabotage : ISabotage
{
    public string Name         => "Spook Animal";
    public string BuyCommand   => "spookanimal";
    public string Description  => "scares a random challenge animal off the farm — permanently";
    public int Cost            => 300;
    public int CooldownSeconds => 240;

    public string? Validate(string args = "")
    {
        if (!AnimalChallengeState.IsEnabled)
            return "The Animal Challenge isn't enabled right now!";
        if (AnimalChallengeState.GetMatchingAnimals().Count == 0)
            return "There are no challenge animals on the farm to lose!";
        return null;
    }

    public void Execute(string triggeredBy)
    {
        var candidates = AnimalChallengeState.GetMatchingAnimals();
        if (candidates.Count == 0) return;

        var animal = candidates[new Random().Next(candidates.Count)];
        var name   = string.IsNullOrWhiteSpace(animal.displayName) ? animal.Name : animal.displayName;
        var type   = animal.type.Value;

        AnimalChallengeState.Remove(animal);

        Game1.addHUDMessage(new HUDMessage(
            $"🐔 {triggeredBy} spooked {name} the {type}! It wandered off and is gone for good. ({AnimalChallengeState.GetCount()}/{AnimalChallengeState.GoalCount})",
            HUDMessage.error_type));
    }
}

// ─── ADD ANIMAL — helps challenge progress by buying chat a free animal ──────

public class AddAnimalBlessing : ISabotage
{
    public string Name         => "Add Animal";
    public string BuyCommand   => "addanimal";
    public string Description  => "buys the farm a free challenge animal, if a coop/barn has room";
    public int Cost            => 400;
    public int CooldownSeconds => 300;
    public SabotageTier Tier   => SabotageTier.Blessing;

    public string? Validate(string args = "")
    {
        if (!AnimalChallengeState.IsEnabled)
            return "The Animal Challenge isn't enabled right now!";

        var type = AnimalChallengeState.ResolveSpawnType();
        if (type == null)
            return $"Buy the farm's first \"{AnimalChallengeState.FilterLabel}\" animal from Marnie's before chat can add more!";

        if (AnimalChallengeState.FindOpenBuilding(type) == null)
            return "No coop or barn has room right now — build or upgrade one first!";

        return null;
    }

    public void Execute(string triggeredBy)
    {
        var type = AnimalChallengeState.ResolveSpawnType();
        if (type == null) return;

        var building = AnimalChallengeState.FindOpenBuilding(type);
        if (building == null) return;

        if (AnimalChallengeState.TrySpawn(building, type, out var error))
        {
            Game1.addHUDMessage(new HUDMessage(
                $"🐣 {triggeredBy} bought the farm a new {type}! ({AnimalChallengeState.GetCount()}/{AnimalChallengeState.GoalCount})",
                HUDMessage.newQuest_type));
        }
        else
        {
            ModEntry.Logger?.Log($"[AddAnimalBlessing] Failed to spawn {type}: {error}", StardewModdingAPI.LogLevel.Warn);
            Game1.addHUDMessage(new HUDMessage(
                $"🐣 {triggeredBy} tried to buy a new animal, but something went wrong. No points were refunded — sorry!",
                HUDMessage.error_type));
        }
    }
}
