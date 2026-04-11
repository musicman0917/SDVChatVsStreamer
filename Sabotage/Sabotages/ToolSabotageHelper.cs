using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public enum ToolOperation { Downgrade, Upgrade, SetCopper, SetIridium }

public static class ToolSabotageHelper
{
    private static readonly Random _rng = new();
    private static IMonitor? _monitor;

    public static void SetMonitor(IMonitor monitor) => _monitor = monitor;

    // Thread-safe queue for inventory changes — must run on main game thread
    private static readonly Queue<Action> _pendingActions = new();
    private static readonly object _actionLock = new();

    public static void ProcessPendingActions()
    {
        lock (_actionLock)
        {
            while (_pendingActions.Count > 0)
                _pendingActions.Dequeue().Invoke();
        }
    }

    private static void QueueAction(Action action)
    {
        lock (_actionLock)
            _pendingActions.Enqueue(action);
    }

    // Tool name aliases chat can use
    private static readonly Dictionary<string, string> _aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        { "wateringcan", "Watering Can" }, { "can",  "Watering Can" },
        { "hoe",         "Hoe"          },
        { "pickaxe",     "Pickaxe"      }, { "pick", "Pickaxe"      },
        { "axe",         "Axe"          },
        { "fishingrod",  "Fishing Rod"  }, { "rod",  "Fishing Rod"  },
    };

    private static string GetBaseName(Tool t) => t switch
    {
        WateringCan  => "Watering Can",
        Hoe          => "Hoe",
        Pickaxe      => "Pickaxe",
        Axe          => "Axe",
        FishingRod   => "Fishing Rod",
        _            => t.Name
    };

    /// <summary>Try to parse a tool alias to a canonical name. Returns null if not found.</summary>
    public static string? ParseToolName(string alias)
    {
        _aliases.TryGetValue(alias.Trim(), out var name);
        return name;
    }

    /// <summary>Execute a tool operation on a specific or random tool. Returns a result message.</summary>
    public static string Execute(string triggeredBy, ToolOperation op, string? targetToolName = null)
    {
        // Collect eligible tools from inventory by type
        var tools = Game1.player.Items
            .OfType<Tool>()
            .Where(t => t is WateringCan || t is Hoe || t is Pickaxe || t is Axe || t is FishingRod)
            .ToList();

        if (tools.Count == 0)
            return $"{triggeredBy} tried to mess with your tools but you have none!";

        // Filter to specific tool if requested
        if (targetToolName != null)
        {
            tools = tools.Where(t => GetBaseName(t).Equals(targetToolName, StringComparison.OrdinalIgnoreCase)).ToList();
            if (tools.Count == 0)
                return $"{triggeredBy} tried to {OpVerb(op)} your {targetToolName} but you don't have one!";
        }

        var tool     = tools[_rng.Next(tools.Count)];
        var oldLevel = tool.UpgradeLevel;
        var toolName = GetBaseName(tool);
        var toolSlot = Game1.player.Items.IndexOf(tool);

        int newLevel = oldLevel;

        switch (op)
        {
            case ToolOperation.Downgrade:
                if (oldLevel <= 0)
                    return $"{triggeredBy} tried to downgrade your {toolName} but it's already at base level!";
                newLevel = oldLevel - 1;
                break;

            case ToolOperation.Upgrade:
                int maxLevel = tool is FishingRod ? 2 : 4;
                if (oldLevel >= maxLevel)
                    return $"{triggeredBy} tried to upgrade your {toolName} but it's already maxed!";
                newLevel = oldLevel + 1;
                break;

            case ToolOperation.SetCopper:
                if (oldLevel == 1)
                    return $"{triggeredBy} tried to set your {toolName} to copper but it's already copper!";
                newLevel = 1;
                break;

            case ToolOperation.SetIridium:
                int iridiumLevel = tool is FishingRod ? 2 : 4;
                if (oldLevel == iridiumLevel)
                    return $"{triggeredBy} tried to upgrade your {toolName} to iridium but it's already iridium!";
                newLevel = iridiumLevel;
                break;
        }

        // Map tool type + level to the correct 1.6 item ID
        static string? GetToolId(Tool t, int level) => (t, level) switch
        {
            (Axe,         0) => "(T)Axe",
            (Axe,         1) => "(T)CopperAxe",
            (Axe,         2) => "(T)SteelAxe",
            (Axe,         3) => "(T)GoldAxe",
            (Axe,         4) => "(T)IridiumAxe",
            (Pickaxe,     0) => "(T)Pickaxe",
            (Pickaxe,     1) => "(T)CopperPickaxe",
            (Pickaxe,     2) => "(T)SteelPickaxe",
            (Pickaxe,     3) => "(T)GoldPickaxe",
            (Pickaxe,     4) => "(T)IridiumPickaxe",
            (Hoe,         0) => "(T)Hoe",
            (Hoe,         1) => "(T)CopperHoe",
            (Hoe,         2) => "(T)SteelHoe",
            (Hoe,         3) => "(T)GoldHoe",
            (Hoe,         4) => "(T)IridiumHoe",
            (WateringCan, 0) => "(T)WateringCan",
            (WateringCan, 1) => "(T)CopperWateringCan",
            (WateringCan, 2) => "(T)SteelWateringCan",
            (WateringCan, 3) => "(T)GoldWateringCan",
            (WateringCan, 4) => "(T)IridiumWateringCan",
            (FishingRod,  0) => "(T)BambooRod",
            (FishingRod,  1) => "(T)TrainingRod",
            (FishingRod,  2) => "(T)FiberglassRod",
            _               => null
        };

        string? newToolId = GetToolId(tool, newLevel);
        if (newToolId == null)
            return $"{triggeredBy} tried to mess with an unknown tool!";

        _monitor?.Log($"[ToolHelper] Looking for {toolName} at level {oldLevel} → {newLevel}", LogLevel.Debug);
        _monitor?.Log($"[ToolHelper] Creating via ItemRegistry: {newToolId}", LogLevel.Debug);

        var newTool = (Tool)ItemRegistry.Create(newToolId);

        _monitor?.Log($"[ToolHelper] New tool created: {newTool.Name}, UpgradeLevel={newTool.UpgradeLevel}", LogLevel.Debug);

        _monitor?.Log($"[ToolHelper] Queueing swap for slot containing {toolName} level {oldLevel} → {newLevel}", LogLevel.Debug);

        QueueAction(() =>
        {
            if (toolSlot >= 0 && toolSlot < Game1.player.Items.Count)
            {
                _monitor?.Log($"[ToolHelper] Main thread swap: slot {toolSlot}", LogLevel.Debug);
                Game1.player.Items[toolSlot] = newTool;
            }
        });

        bool swapped = true;

        _monitor?.Log($"[ToolHelper] Swap result: {swapped}", LogLevel.Debug);

        return $"{OpEmoji(op)} {triggeredBy} {OpPast(op)} your {toolName}! ({LevelName(oldLevel)} → {LevelName(newLevel)})";
    }

    private static string OpVerb(ToolOperation op) => op switch
    {
        ToolOperation.Downgrade  => "downgrade",
        ToolOperation.Upgrade    => "upgrade",
        ToolOperation.SetCopper  => "set to copper",
        ToolOperation.SetIridium => "set to iridium",
        _ => "modify"
    };

    private static string OpPast(ToolOperation op) => op switch
    {
        ToolOperation.Downgrade  => "downgraded",
        ToolOperation.Upgrade    => "upgraded",
        ToolOperation.SetCopper  => "set to copper",
        ToolOperation.SetIridium => "set to iridium",
        _ => "modified"
    };

    private static string OpEmoji(ToolOperation op) => op switch
    {
        ToolOperation.Downgrade  => "📉",
        ToolOperation.Upgrade    => "📈",
        ToolOperation.SetCopper  => "🔽",
        ToolOperation.SetIridium => "🔼",
        _ => "🔧"
    };

    public static string LevelName(int level) => level switch
    {
        0 => "Basic",
        1 => "Copper",
        2 => "Steel",
        3 => "Gold",
        4 => "Iridium",
        _ => level.ToString()
    };
}