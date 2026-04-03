namespace SDVChatVsStreamer.Sabotage;

public class SabotageDefinition
{
    public ISabotage Sabotage { get; init; } = null!;
    public DateTime LastFired { get; set; } = DateTime.MinValue;

    public string Name        => Sabotage.Name;
    public string BuyCommand  => Sabotage.BuyCommand;
    public string Description => Sabotage.Description;
    public int Cost           => Sabotage.Cost;

    public bool IsOnCooldown =>
        (DateTime.UtcNow - LastFired).TotalSeconds < Sabotage.CooldownSeconds;

    public int CooldownRemaining =>
        Math.Max(0, Sabotage.CooldownSeconds - (int)(DateTime.UtcNow - LastFired).TotalSeconds);

    public void Fire(string triggeredBy)
    {
        Sabotage.Execute(triggeredBy);
        LastFired = DateTime.UtcNow;
    }
}
