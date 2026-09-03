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
///
/// Targeting is routed by CHANNEL, not by chatter username: each co-op player is their own
/// streamer with their own Twitch channel, and TwitchManager joins those channels alongside
/// the host's own. Any command typed in a co-op player's channel — by them or one of their
/// own viewers — targets THAT player's farmhand, the same way any command in the host's own
/// channel already targets the host. The channel-to-farmhand link is an exact (case-insensitive)
/// match between the channel name and a connected farmhand's character name.
/// </summary>
public static class MultiplayerTargeting
{
    private static ModConfig? _config;

    public static void Init(ModConfig config) => _config = config;

    /// <summary>
    /// Set by TwitchManager immediately before dispatching a chat-triggered command, naming the
    /// Twitch channel the message came from, and cleared right after in a finally block. Safe as
    /// an ambient static because the whole chat-command dispatch chain (OnMessageReceived →
    /// SabotageEngine.TryBuy → ISabotage.Execute) runs synchronously within a single TwitchLib
    /// event callback — no other message can interleave between the set and the clear.
    /// </summary>
    public static string? CurrentChannel { get; set; }

    /// <summary>The host's own channel plus every enabled co-op player channel from config.</summary>
    public static IEnumerable<(string Channel, bool Enabled)> ConfiguredCoopChannels()
    {
        if (_config == null) yield break;
        yield return (_config.MultiplayerPlayer2Channel, _config.MultiplayerPlayer2Enabled);
        yield return (_config.MultiplayerPlayer3Channel, _config.MultiplayerPlayer3Enabled);
        yield return (_config.MultiplayerPlayer4Channel, _config.MultiplayerPlayer4Enabled);
    }

    /// <summary>
    /// Resolves the Farmer a chat-triggered command should land on. If multiplayer targeting is
    /// off, the message didn't come from a configured co-op channel, or no currently connected
    /// farmhand's character name matches that channel exactly, falls back to Game1.player (the
    /// host) — the same behavior as before this feature existed. Commands from the host's own
    /// channel always resolve to Game1.player.
    /// </summary>
    public static Farmer Resolve(string triggeredBy)
    {
        if (_config == null || !_config.MultiplayerTargetingEnabled) return Game1.player;

        var channel = CurrentChannel;
        if (string.IsNullOrWhiteSpace(channel)) return Game1.player;
        if (channel.Equals(_config.ChannelName, StringComparison.OrdinalIgnoreCase)) return Game1.player;

        bool isConfiguredCoopChannel = ConfiguredCoopChannels().Any(c =>
            c.Enabled && !string.IsNullOrWhiteSpace(c.Channel) &&
            c.Channel.Trim().Equals(channel, StringComparison.OrdinalIgnoreCase));
        if (!isConfiguredCoopChannel) return Game1.player;

        try
        {
            foreach (var farmer in Game1.getOnlineFarmers())
            {
                if (farmer.Name.Equals(channel, StringComparison.OrdinalIgnoreCase))
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
