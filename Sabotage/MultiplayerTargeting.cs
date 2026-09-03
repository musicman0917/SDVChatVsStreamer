using StardewValley;

namespace SDVChatVsStreamer.Sabotage;

/// <summary>
/// Resolves which Farmer a sabotage should actually act on. Every sabotage in the library
/// was originally written against Game1.player (the local player of whichever machine runs
/// the mod). In multiplayer, only clients with the mod installed run its code at all — so
/// this can only ever redirect effects that mutate host-authoritative, network-synced state
/// (Farmer stats/buffs/money/items, or GameLocation contents like monsters and explosions).
/// Effects that read local input or draw directly to a screen (Confused, jump scares, bans,
/// warps) have no way to reach a farmhand who doesn't run the mod, and intentionally keep
/// using Game1.player untouched.
/// </summary>
public static class MultiplayerTargeting
{
    private static ModConfig? _config;

    public static void Init(ModConfig config) => _config = config;

    /// <summary>
    /// Resolves the Farmer a triggering chatter's effect should land on. If multiplayer
    /// targeting is off, the chatter isn't on the configured player list, or no currently
    /// connected farmhand's character name matches their Twitch name exactly, falls back
    /// to Game1.player (the host) — the same behavior as before this feature existed.
    /// </summary>
    public static Farmer Resolve(string triggeredBy)
    {
        if (_config == null || !_config.MultiplayerTargetingEnabled || string.IsNullOrWhiteSpace(triggeredBy))
            return Game1.player;

        var listed = _config.MultiplayerPlayers
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!listed.Contains(triggeredBy, StringComparer.OrdinalIgnoreCase))
            return Game1.player;

        try
        {
            foreach (var farmer in Game1.getOnlineFarmers())
            {
                if (farmer.Name == triggeredBy)
                    return farmer;
            }
        }
        catch (Exception ex)
        {
            ModEntry.Logger?.Log($"[MultiplayerTargeting] Farmhand lookup failed: {ex.Message}", StardewModdingAPI.LogLevel.Warn);
        }

        return Game1.player;
    }

    /// <summary>
    /// Shows a HUD message on the local (host) screen — the only screen this code is running
    /// on. When the effect actually landed on a farmhand rather than the host, tags who it
    /// hit so the message isn't misread as having happened to the host.
    /// </summary>
    public static void Notify(Farmer target, string message, int hudType)
    {
        if (target != Game1.player)
            message += $" (hit {target.Name})";
        Game1.addHUDMessage(new HUDMessage(message, hudType));
    }
}
