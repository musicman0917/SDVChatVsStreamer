using StardewValley;
using SDVChatVsStreamer.Sabotage;
using StardewModdingAPI;
using System.Linq;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class WarpSabotage : ISabotage, ILocallyRelayable
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
        var target = MultiplayerTargeting.Resolve(triggeredBy);
        if (target != Game1.player && PeerRelay.TryRelay(target, BuyCommand, triggeredBy))
        {
            MultiplayerTargeting.Notify(target, $"🌀 {triggeredBy} warped you away!", HUDMessage.error_type);
            return;
        }
        ExecuteLocal(triggeredBy);
    }

    public void ExecuteLocal(string triggeredBy)
    {
        ModEntry.Logger?.Log($"[WarpSabotage] Execute called by {triggeredBy}. Current location: {Game1.player.currentLocation?.Name ?? "null"}", LogLevel.Info);

        // Try each destination in random order until one actually warps successfully
        var shuffled = _destinations.OrderBy(_ => _rng.Next()).ToList();
        foreach (var dest in shuffled)
        {
            ModEntry.Logger?.Log($"[WarpSabotage] Trying destination: {dest.Location} ({dest.X},{dest.Y})", LogLevel.Info);
            if (WarpHelper.SafeWarpToRandomSpot(dest.Location, dest.X, dest.Y, _rng))
            {
                ModEntry.Logger?.Log($"[WarpSabotage] Successfully warped to {dest.Location}.", LogLevel.Info);
                Game1.addHUDMessage(new HUDMessage(
                    $"🌀 {triggeredBy} warped you to {dest.Location}!",
                    HUDMessage.error_type));
                return;
            }
        }

        ModEntry.Logger?.Log("[WarpSabotage] All destinations failed SafeWarp — refunding.", LogLevel.Warn);
        // Refund what was actually paid, not the static base Cost -- if "sabotage costs
        // halved" was active at purchase time, the player paid less than Cost and this
        // would otherwise refund more than they spent. (If this is running on a relayed
        // farmhand, their own RaidEventSystem's cost-modifier state is independent of the
        // host's and could theoretically be stale at this exact moment -- a narrow,
        // low-stakes imprecision versus the alternative of threading the host's effective
        // cost through the relay protocol for every effect just for this one case.)
        int refund = ModEntry.RaidEvents?.ApplyCostModifier(Cost) ?? Cost;

        // The shared ledger only exists on the host -- a farmhand's own ViewerLedger is a
        // separate, un-synced database, so a direct AddPoints here would vanish instead of
        // returning points to the economy the viewer actually spent them in.
        if (Game1.IsMasterGame)
            ModEntry.Ledger?.AddPoints(triggeredBy, refund);
        else
            PeerRelay.RequestRefund(triggeredBy, refund, BuyCommand);

        Game1.addHUDMessage(new HUDMessage(
            $"🌀 {triggeredBy} tried to warp you, but nowhere safe was found! Refunded {refund}pts.",
            HUDMessage.error_type));
    }
}