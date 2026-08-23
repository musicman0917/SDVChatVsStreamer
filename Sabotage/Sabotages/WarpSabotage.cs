using StardewValley;
using SDVChatVsStreamer.Sabotage;
using StardewModdingAPI;
using System.Linq;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class WarpSabotage : ISabotage
{
    public string Name         => "Warp";
    public string BuyCommand   => "warp";
    public string Description  => "warps you to a random location";
    public int Cost            => 400;
    public int CooldownSeconds => 300;

    private readonly Random _rng = new();

    private static readonly (string Location, int X, int Y)[] _destinations =
    {
        ("Town",     50, 80),
        ("Mountain", 30, 20),
        ("Beach",    30,  5),
        ("Forest",   60, 40),
        ("BusStop",  12, 10),
        ("Mine",      8,  8),
    };

    public void Execute(string triggeredBy)
    {
        ModEntry.Logger?.Log($"[WarpSabotage] Execute called by {triggeredBy}. Current location: {Game1.player.currentLocation?.Name ?? "null"}", LogLevel.Info);

        // Try each destination in random order until one actually warps successfully
        var shuffled = _destinations.OrderBy(_ => _rng.Next()).ToList();
        foreach (var dest in shuffled)
        {
            ModEntry.Logger?.Log($"[WarpSabotage] Trying destination: {dest.Location} ({dest.X},{dest.Y})", LogLevel.Info);
            if (WarpHelper.SafeWarp(dest.Location, dest.X, dest.Y))
            {
                ModEntry.Logger?.Log($"[WarpSabotage] Successfully warped to {dest.Location}.", LogLevel.Info);
                Game1.addHUDMessage(new HUDMessage(
                    $"🌀 {triggeredBy} warped you to {dest.Location}!",
                    HUDMessage.error_type));
                return;
            }
        }

        ModEntry.Logger?.Log("[WarpSabotage] All destinations failed SafeWarp.", LogLevel.Warn);
        Game1.addHUDMessage(new HUDMessage(
            $"🌀 {triggeredBy} tried to warp you, but nowhere safe was found!",
            HUDMessage.error_type));
    }
}