using StardewValley;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

/// <summary>A single tool sabotage — one operation on one specific (or random) tool.</summary>
public class ToolSabotage : ISabotage
{
    private readonly ToolOperation _op;
    private readonly string? _toolName; // null = random

    public string Name { get; }
    public string BuyCommand { get; }
    public string Description { get; }
    public int Cost { get; }
    public int CooldownSeconds { get; }

    public ToolSabotage(ToolOperation op, string? toolName, string buyCommand, string description, int cost, int cooldown)
    {
        _op          = op;
        _toolName    = toolName;
        Name         = buyCommand;
        BuyCommand   = buyCommand;
        Description  = description;
        Cost         = cost;
        CooldownSeconds = cooldown;
    }

    public void Execute(string triggeredBy)
    {
        var msg = ToolSabotageHelper.Execute(triggeredBy, _op, _toolName);
        var type = _op == ToolOperation.Upgrade || _op == ToolOperation.SetIridium
            ? HUDMessage.newQuest_type
            : HUDMessage.error_type;
        Game1.addHUDMessage(new HUDMessage(msg, type));
    }

    /// <summary>Build all 24 tool sabotage instances.</summary>
    public static IEnumerable<ToolSabotage> BuildAll()
    {
        var tools = new (string? Name, string Suffix)[]
        {
            (null,           "random"),
            ("Axe",          "axe"),
            ("Pickaxe",      "pickaxe"),
            ("Hoe",          "hoe"),
            ("Watering Can", "wateringcan"),
            ("Fishing Rod",  "rod"),
        };

        foreach (var (toolName, suffix) in tools)
        {
            var isRandom = toolName == null;
            var toolLabel = isRandom ? "a random tool" : $"your {toolName!.ToLower()}";

            yield return new ToolSabotage(
                op:          ToolOperation.Downgrade,
                toolName:    toolName,
                buyCommand:  $"downgrade{suffix}",
                description: $"downgrades {toolLabel} one level",
                cost:        isRandom ? 175 : 200,
                cooldown:    120);

            yield return new ToolSabotage(
                op:          ToolOperation.Upgrade,
                toolName:    toolName,
                buyCommand:  $"upgrade{suffix}",
                description: $"upgrades {toolLabel} one level",
                cost:        isRandom ? 125 : 150,
                cooldown:    120);

            yield return new ToolSabotage(
                op:          ToolOperation.SetCopper,
                toolName:    toolName,
                buyCommand:  $"copper{suffix}",
                description: $"sets {toolLabel} to copper",
                cost:        isRandom ? 250 : 300,
                cooldown:    180);

            yield return new ToolSabotage(
                op:          ToolOperation.SetIridium,
                toolName:    toolName,
                buyCommand:  $"iridium{suffix}",
                description: $"sets {toolLabel} to iridium",
                cost:        isRandom ? 250 : 300,
                cooldown:    180);
        }
    }
}

