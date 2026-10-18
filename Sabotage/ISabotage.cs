namespace SDVChatVsStreamer.Sabotage;

public interface ISabotage
{
    string Name { get; }
    string BuyCommand { get; }
    string Description { get; }
    int Cost { get; }
    int CooldownSeconds { get; }

    // Default tier is inferred from cost for sabotages. Blessings are never inferred —
    // every Blessing class overrides this explicitly, so anything expensive that
    // doesn't override falls into Devastating rather than being mistaken for a blessing.
    SabotageTier Tier => Cost switch
    {
        <= 100  => SabotageTier.Nuisance,
        <= 200  => SabotageTier.Disruptive,
        <= 350  => SabotageTier.Painful,
        _       => SabotageTier.Devastating
    };

    string? Validate(string args = "") => null;

    void Execute(string triggeredBy);

    void ExecuteWithArgs(string triggeredBy, string args) => Execute(triggeredBy);
}

public enum SabotageTier { Nuisance, Disruptive, Painful, Devastating, Blessing }