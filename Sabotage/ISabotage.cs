namespace SDVChatVsStreamer.Sabotage;

public interface ISabotage
{
    string Name { get; }
    string BuyCommand { get; }
    string Description { get; }
    int Cost { get; }
    int CooldownSeconds { get; }

    SabotageTier Tier => Cost switch
    {
        <= 100  => SabotageTier.Nuisance,
        <= 200  => SabotageTier.Disruptive,
        <= 350  => SabotageTier.Painful,
        <= 1000 => SabotageTier.Devastating,
        _       => SabotageTier.Blessing
    };

    string? Validate(string args = "") => null;

    void Execute(string triggeredBy);

    void ExecuteWithArgs(string triggeredBy, string args) => Execute(triggeredBy);
}

public enum SabotageTier { Nuisance, Disruptive, Painful, Devastating, Blessing }