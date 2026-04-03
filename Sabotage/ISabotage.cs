namespace SDVChatVsStreamer.Sabotage;

public interface ISabotage
{
    string Name { get; }
    string BuyCommand { get; }
    string Description { get; }
    int Cost { get; }
    int CooldownSeconds { get; }

    /// <summary>
    /// Optional pre-execution check. Return null if ok, or an error message to
    /// reject the purchase and refund the points.
    /// </summary>
    string? Validate() => null;

    void Execute(string triggeredBy);
}
