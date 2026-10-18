using StardewModdingAPI;
using StardewValley;

namespace SDVChatVsStreamer.Sabotage;

/// <summary>
/// A sabotage that can be executed purely locally, with no state-mutating side effects of
/// its own beyond calling into static Tick()/Draw()-driven state (buffs, timers, screen
/// overlays, input suppression). Implemented by effects that today only ever land on
/// Game1.player regardless of who they were "aimed" at -- Confused, bans, jump scares,
/// warps, and similar -- so PeerRelay can ask a farmhand's own copy of this mod to run
/// ExecuteLocal() on their machine, where Game1.player correctly means them.
/// </summary>
public interface ILocallyRelayable
{
    void ExecuteLocal(string triggeredBy);
}

/// <summary>
/// Relays "run this local-only effect" to a specific connected farmhand's own copy of this
/// mod, over SMAPI's built-in multiplayer messaging. Needed because effects like Confused,
/// jump scares, bans, and warps work by reading local input or writing directly to
/// Game1.player/the screen -- state that never syncs across the network on its own, so the
/// only way to make one land on a farmhand is to ask their own mod instance (if they have
/// one) to run it locally on their own machine.
///
/// Everything here is best-effort: a farmhand without the mod, a dropped connection, or a
/// malformed payload all just mean the relay silently fails and the caller falls back to
/// its existing host-only behavior -- never a crash, and never a reason to deny the host
/// their own purchase.
/// </summary>
public static class PeerRelay
{
    public const string EffectMessageType = "ExecuteLocalEffect";
    public const string RefundMessageType = "RequestRefund";

    private static IModHelper? _helper;
    private static IManifest? _manifest;
    private static IMonitor? _monitor;

    public static void Init(IModHelper helper, IManifest manifest, IMonitor monitor)
    {
        _helper   = helper;
        _manifest = manifest;
        _monitor  = monitor;
    }

    /// <summary>
    /// True if the given farmer is a connected farmhand (not the local player) running a
    /// copy of this same mod, reachable over SMAPI's multiplayer messaging.
    /// </summary>
    public static bool HasModInstalled(Farmer target)
    {
        if (_helper == null || _manifest == null) return false;
        if (target == Game1.player) return false; // local player never needs relaying

        try
        {
            var peer = _helper.Multiplayer.GetConnectedPlayer(target.UniqueMultiplayerID);
            return peer?.GetMod(_manifest.UniqueID) != null;
        }
        catch (Exception ex)
        {
            _monitor?.Log($"[PeerRelay] Peer lookup for {target.Name} failed: {ex.Message}", LogLevel.Debug);
            return false;
        }
    }

    /// <summary>
    /// Asks the given farmhand's own mod instance to run the named effect (its BuyCommand)
    /// locally on their machine. Returns false if they're the local player or don't have
    /// the mod -- callers should fall back to their existing host-only ExecuteLocal in that
    /// case, exactly as if this feature didn't exist.
    /// </summary>
    public static bool TryRelay(Farmer target, string effectId, string triggeredBy)
    {
        if (!HasModInstalled(target)) return false;

        try
        {
            _helper!.Multiplayer.SendMessage(
                message:     new RelayPayload(effectId, triggeredBy),
                messageType: EffectMessageType,
                modIDs:      new[] { _manifest!.UniqueID },
                playerIDs:   new[] { target.UniqueMultiplayerID });
            _monitor?.Log($"[PeerRelay] Relayed '{effectId}' (bought by {triggeredBy}) to {target.Name}.", LogLevel.Debug);
            return true;
        }
        catch (Exception ex)
        {
            _monitor?.Log($"[PeerRelay] Failed to relay '{effectId}' to {target.Name}: {ex.Message}", LogLevel.Warn);
            return false;
        }
    }

    /// <summary>
    /// Asks the host to refund points to a viewer. Only needed when ExecuteLocal() ends up
    /// running on a farmhand's machine (because the host relayed it there) and itself fails
    /// in a way that would normally refund the purchase -- the farmhand's own ViewerLedger
    /// is a separate, un-synced database, so a local refund there would silently vanish
    /// instead of returning points to the shared economy the viewer actually spent them in.
    /// Call this instead of ModEntry.Ledger directly whenever !Game1.IsMasterGame.
    /// </summary>
    public static void RequestRefund(string username, int amount, string reason)
    {
        if (_helper == null || _manifest == null || amount <= 0) return;

        try
        {
            _helper.Multiplayer.SendMessage(
                message:     new RefundPayload(username, amount, reason),
                messageType: RefundMessageType,
                modIDs:      new[] { _manifest.UniqueID },
                playerIDs:   new[] { Game1.MasterPlayer.UniqueMultiplayerID });
            _monitor?.Log($"[PeerRelay] Requested {amount}pt refund for {username} ({reason}) from host.", LogLevel.Debug);
        }
        catch (Exception ex)
        {
            _monitor?.Log($"[PeerRelay] Failed to request refund for {username}: {ex.Message}", LogLevel.Warn);
        }
    }

    public record RelayPayload(string EffectId, string TriggeredBy);
    public record RefundPayload(string Username, int Amount, string Reason);
}
