namespace SDVChatVsStreamer.Sabotage;

public interface ISabotage
{
    string Name { get; }
    string BuyCommand { get; }
    string Description { get; }
    int Cost { get; }
    int CooldownSeconds { get; }

    /// <summary>Tier used for auto-clipping decisions.</summary>
    SabotageTier Tier => Cost switch
    {
        <= 100  => SabotageTier.Nuisance,
        <= 200  => SabotageTier.Disruptive,
        <= 350  => SabotageTier.Painful,
        <= 1000 => SabotageTier.Devastating,
        _       => SabotageTier.Blessing
    };

    /// <summary>
    /// Optional pre-execution check. Return null if ok, or an error message to
    /// reject the purchase and refund the points.
    /// </summary>
    string? Validate() => null;

    void Execute(string triggeredBy);
}

public enum SabotageTier { Nuisance, Disruptive, Painful, Devastating, Blessing }