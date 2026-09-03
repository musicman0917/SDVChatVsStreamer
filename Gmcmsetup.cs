using StardewModdingAPI;
using StardewValley;

namespace SDVChatVsStreamer;

public static class GmcmSetup
{
    /// <param name="onMultiplayerConfigChanged">Invoked after every GMCM save so co-op channel
    /// joins/parts can be reconciled live instead of requiring a restart to take effect.</param>
    public static void Register(IModHelper helper, IManifest manifest, ModConfig config, Action? onMultiplayerConfigChanged = null)
    {
        var api = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (api == null)
        {
            // GMCM not installed — silently skip
            return;
        }

        api.Register(
            mod:   manifest,
            reset: () => ResetConfig(config),
            save:  () =>
            {
                helper.WriteConfig(config);
                onMultiplayerConfigChanged?.Invoke();
            }
        );

        // ─── Pages ────────────────────────────────────────────────────────────
        // All page links MUST be added here, before the first AddPage call below —
        // AddPageLink attaches its button to whatever page is currently active, so
        // any link added after an AddPage call ends up buried on that page instead
        // of the root menu.

        api.AddPageLink(manifest, "general",   () => "⚙️  General");
        api.AddPageLink(manifest, "points",    () => "⭐ Points & Economy");
        api.AddPageLink(manifest, "bits",      () => "💰 Bits");
        api.AddPageLink(manifest, "donordrive",() => "💸 DonorDrive");
        api.AddPageLink(manifest, "behavior",  () => "🔧 Sabotage Behavior");
        api.AddPageLink(manifest, "tiktok",    () => "🎵 TikTok (Tikfinity)");
        api.AddPageLink(manifest, "youtube",   () => "📺 YouTube (Streamer.bot) [BETA]");
        api.AddPageLink(manifest, "overlay",   () => "🖥️  OBS Overlay (Shop/Feed)");
        api.AddPageLink(manifest, "chatfeed",  () => "💬 Chat Feed Display");
        api.AddPageLink(manifest, "autoclip",  () => "🎬 Auto-Clipping");
        api.AddPageLink(manifest, "challenge", () => "🐔 Animal Challenge");
        api.AddPageLink(manifest, "multiplayer", () => "👥 Multiplayer Targeting");
        api.AddPageLink(manifest, "ignored",   () => "🚫 Ignored Users");

        // ─── General ──────────────────────────────────────────────────────────

        api.AddPage(manifest, "general", () => "General");

        api.AddSectionTitle(manifest, () => "Twitch Channel");
        api.AddTextOption(manifest,
            getValue: () => config.ChannelName,
            setValue: v => config.ChannelName = v,
            name: () => "Channel Name",
            tooltip: () => "Your Twitch channel name (lowercase)");
        api.AddTextOption(manifest,
            getValue: () => config.BotUsername,
            setValue: v => config.BotUsername = v,
            name: () => "Bot Username",
            tooltip: () => "The bot account that sends chat messages");
        api.AddTextOption(manifest,
            getValue: () => config.BroadcasterUserId,
            setValue: v => config.BroadcasterUserId = v,
            name: () => "Broadcaster User ID",
            tooltip: () => "Your numeric Twitch user ID (find it at streamweasels.com/tools/twitch-user-id-finder)");

        api.AddSectionTitle(manifest, () => "Chaos Shop");
        api.AddTextOption(manifest,
            getValue: () => config.ShopUrl,
            setValue: v => config.ShopUrl = v,
            name: () => "Shop Reference URL",
            tooltip: () => "Link posted in chat by !shop instead of listing every command");

        api.AddSectionTitle(manifest, () => "Key Bindings");
        api.AddTextOption(manifest,
            getValue: () => config.PasteTokenKey,
            setValue: v => config.PasteTokenKey = v,
            name: () => "Paste Token Key",
            tooltip: () => "Key to press to paste the Twitch OAuth token (default: F9)");

        // ─── Points & Economy ─────────────────────────────────────────────────

        api.AddPage(manifest, "points", () => "Points & Economy");

        api.AddSectionTitle(manifest, () => "Passive Points");
        api.AddNumberOption(manifest,
            getValue: () => config.PassiveTickMinutes,
            setValue: v => config.PassiveTickMinutes = v,
            name: () => "Tick Interval (minutes)",
            tooltip: () => "How often passive points are awarded to all viewers",
            min: 1, max: 60);
        api.AddNumberOption(manifest,
            getValue: () => config.BasePassivePoints,
            setValue: v => config.BasePassivePoints = v,
            name: () => "Base Passive Points",
            tooltip: () => "Points awarded per tick before sub multiplier",
            min: 1, max: 100);

        api.AddSectionTitle(manifest, () => "Chat Bonus");
        api.AddNumberOption(manifest,
            getValue: () => config.ChatBonusPoints,
            setValue: v => config.ChatBonusPoints = v,
            name: () => "Chat Bonus Points",
            tooltip: () => "Bonus points awarded for chatting",
            min: 0, max: 50);
        api.AddNumberOption(manifest,
            getValue: () => config.ChatBonusCooldownSeconds,
            setValue: v => config.ChatBonusCooldownSeconds = v,
            name: () => "Chat Bonus Cooldown (seconds)",
            tooltip: () => "How long before the same viewer can earn chat bonus again",
            min: 10, max: 600);

        api.AddSectionTitle(manifest, () => "Event Bonuses");
        api.AddNumberOption(manifest,
            getValue: () => config.FollowBonus,
            setValue: v => config.FollowBonus = v,
            name: () => "Follow Bonus",
            tooltip: () => "Points awarded when someone follows",
            min: 0, max: 500);
        api.AddNumberOption(manifest,
            getValue: () => config.SubBonus,
            setValue: v => config.SubBonus = v,
            name: () => "Sub / Resub Bonus",
            tooltip: () => "Points awarded when someone subscribes or resubscribes",
            min: 0, max: 1000);
        api.AddNumberOption(manifest,
            getValue: () => config.GiftSubBonusEach,
            setValue: v => config.GiftSubBonusEach = v,
            name: () => "Gift Sub Bonus (per gift)",
            tooltip: () => "Points awarded to the gifter per gifted sub",
            min: 0, max: 500);
        api.AddNumberOption(manifest,
            getValue: () => config.RaidLeaderPointsPerViewer,
            setValue: v => config.RaidLeaderPointsPerViewer = v,
            name: () => "Raid Leader Points (per viewer)",
            tooltip: () => "Points awarded to the raid leader per viewer they bring",
            min: 0, max: 20);
        api.AddNumberOption(manifest,
            getValue: () => config.RaidViewerBonus,
            setValue: v => config.RaidViewerBonus = v,
            name: () => "Raider Welcome Bonus",
            tooltip: () => "Points awarded to each viewer who joins via raid",
            min: 0, max: 200);

        api.AddSectionTitle(manifest, () => "Sub Multipliers");
        api.AddParagraph(manifest, () => "Multipliers are applied to passive point ticks. Higher tier = more points per tick.");
        api.AddNumberOption(manifest,
            getValue: () => config.MultiplierNone,
            setValue: v => config.MultiplierNone = v,
            name: () => "Non-Sub Multiplier",
            min: 0.1f, max: 5.0f, interval: 0.05f);
        api.AddNumberOption(manifest,
            getValue: () => config.MultiplierPrime,
            setValue: v => config.MultiplierPrime = v,
            name: () => "Prime Sub Multiplier",
            min: 0.1f, max: 5.0f, interval: 0.05f);
        api.AddNumberOption(manifest,
            getValue: () => config.MultiplierT1,
            setValue: v => config.MultiplierT1 = v,
            name: () => "Tier 1 Sub Multiplier",
            min: 0.1f, max: 5.0f, interval: 0.05f);
        api.AddNumberOption(manifest,
            getValue: () => config.MultiplierT2,
            setValue: v => config.MultiplierT2 = v,
            name: () => "Tier 2 Sub Multiplier",
            min: 0.1f, max: 5.0f, interval: 0.05f);
        api.AddNumberOption(manifest,
            getValue: () => config.MultiplierT3,
            setValue: v => config.MultiplierT3 = v,
            name: () => "Tier 3 Sub Multiplier",
            min: 0.1f, max: 5.0f, interval: 0.05f);

        // ─── Bits ─────────────────────────────────────────────────────────────

        api.AddPage(manifest, "bits", () => "Bits");

        api.AddNumberOption(manifest,
            getValue: () => config.BitsPerPoint,
            setValue: v => config.BitsPerPoint = v,
            name: () => "Points Per Bit",
            tooltip: () => "How many points are awarded per bit cheered",
            min: 0, max: 10);

        api.AddSectionTitle(manifest, () => "Sabotage Thresholds");
        api.AddParagraph(manifest, () => "Bit cheers above these thresholds trigger sabotage events.");
        api.AddNumberOption(manifest,
            getValue: () => config.SmallBitThreshold,
            setValue: v => config.SmallBitThreshold = v,
            name: () => "Small Bit Threshold",
            min: 1, max: 10000);
        api.AddNumberOption(manifest,
            getValue: () => config.MediumBitThreshold,
            setValue: v => config.MediumBitThreshold = v,
            name: () => "Medium Bit Threshold",
            min: 1, max: 10000);
        api.AddNumberOption(manifest,
            getValue: () => config.LargeBitThreshold,
            setValue: v => config.LargeBitThreshold = v,
            name: () => "Large Bit Threshold",
            min: 1, max: 10000);

        // ─── DonorDrive ───────────────────────────────────────────────────────

        api.AddPage(manifest, "donordrive", () => "DonorDrive");

        api.AddSectionTitle(manifest, () => "DonorDrive Settings");
        api.AddParagraph(manifest, () => "Polls a DonorDrive-powered donation page (Extra Life, etc.) and turns each new donation into chaos points plus a random sabotage/blessing scaled by donation size.");
        api.AddBoolOption(manifest,
            getValue: () => config.DonorDriveEnabled,
            setValue: v => config.DonorDriveEnabled = v,
            name: () => "Enabled",
            tooltip: () => "Turn DonorDrive polling on or off");
        api.AddTextOption(manifest,
            getValue: () => config.DonorDriveParticipantId,
            setValue: v => config.DonorDriveParticipantId = v,
            name: () => "Participant ID",
            tooltip: () => "The number (or alias) from your fundraising page's URL, e.g. extra-life.org/participant/123456 → 123456");
        api.AddTextOption(manifest,
            getValue: () => config.DonorDriveApiBaseUrl,
            setValue: v => config.DonorDriveApiBaseUrl = v,
            name: () => "API Base URL",
            tooltip: () => "Base URL of your org's DonorDrive site, e.g. https://www.extra-life.org");
        api.AddNumberOption(manifest,
            getValue: () => config.DonorDrivePollSeconds,
            setValue: v => config.DonorDrivePollSeconds = v,
            name: () => "Poll Interval (seconds)",
            tooltip: () => "How often to check for new donations. DonorDrive asks integrators not to poll more often than every 15 seconds.",
            min: 15, max: 300);
        api.AddNumberOption(manifest,
            getValue: () => config.DonationPointsPerCent,
            setValue: v => config.DonationPointsPerCent = v,
            name: () => "Points Per Cent Donated",
            tooltip: () => "How many points are awarded per cent donated (bits award 1 point per cent by default)",
            min: 0, max: 20);

        api.AddSectionTitle(manifest, () => "Donation Size Tiers ($)");
        api.AddParagraph(manifest, () => "Bigger donations roll from a more dramatic effect pool, same idea as raid size.");
        api.AddNumberOption(manifest,
            getValue: () => config.DonationSmallThreshold,
            setValue: v => config.DonationSmallThreshold = v,
            name: () => "Small Donation Threshold",
            min: 1, max: 100000);
        api.AddNumberOption(manifest,
            getValue: () => config.DonationMediumThreshold,
            setValue: v => config.DonationMediumThreshold = v,
            name: () => "Medium Donation Threshold",
            min: 1, max: 100000);
        api.AddNumberOption(manifest,
            getValue: () => config.DonationLargeThreshold,
            setValue: v => config.DonationLargeThreshold = v,
            name: () => "Large Donation Threshold",
            min: 1, max: 100000);
        api.AddNumberOption(manifest,
            getValue: () => config.DonationMassiveThreshold,
            setValue: v => config.DonationMassiveThreshold = v,
            name: () => "Massive Donation Threshold",
            min: 1, max: 100000);

        // ─── Sabotage Behavior ────────────────────────────────────────────────

        api.AddPage(manifest, "behavior", () => "Sabotage Behavior");

        api.AddSectionTitle(manifest, () => "Chat Commands & Events");
        api.AddBoolOption(manifest,
            getValue: () => config.EnableChatCommands,
            setValue: v => config.EnableChatCommands = v,
            name: () => "Enable Chat Commands",
            tooltip: () => "Allow !buy, !shop, !balance commands in chat");
        api.AddBoolOption(manifest,
            getValue: () => config.EnableChannelPoints,
            setValue: v => config.EnableChannelPoints = v,
            name: () => "Enable Channel Points",
            tooltip: () => "Allow channel point redemptions to trigger sabotages");
        api.AddBoolOption(manifest,
            getValue: () => config.EnableBitEvents,
            setValue: v => config.EnableBitEvents = v,
            name: () => "Enable Bit Events",
            tooltip: () => "Allow bit cheers to trigger sabotages");
        api.AddBoolOption(manifest,
            getValue: () => config.EnableFollowBonus,
            setValue: v => config.EnableFollowBonus = v,
            name: () => "Enable Follow Bonus",
            tooltip: () => "Award points to new followers");
        api.AddBoolOption(manifest,
            getValue: () => config.EnableRaidEvents,
            setValue: v => config.EnableRaidEvents = v,
            name: () => "Enable Raid Events",
            tooltip: () => "Fire a random sabotage when a raid comes in");

        api.AddSectionTitle(manifest, () => "Starter Points Redemptions");
        api.AddParagraph(manifest, () => "Set up to three channel point rewards that award chaos points. Title must match your Twitch reward exactly (case-insensitive).");
        api.AddBoolOption(manifest,
            getValue: () => config.EnableStarterRedemption,
            setValue: v => config.EnableStarterRedemption = v,
            name: () => "Enable CPR Starter Points",
            tooltip: () => "Award chaos points when a viewer redeems a matching channel point reward");
        api.AddTextOption(manifest,
            getValue: () => config.StarterRedemptionTitleSmall,
            setValue: v => config.StarterRedemptionTitleSmall = v,
            name: () => "Small Reward Title",
            tooltip: () => "Must match your Twitch channel point reward title exactly");
        api.AddNumberOption(manifest,
            getValue: () => config.StarterRedemptionPointsSmall,
            setValue: v => config.StarterRedemptionPointsSmall = v,
            name: () => "Small Reward Points",
            min: 1, max: 10000);
        api.AddTextOption(manifest,
            getValue: () => config.StarterRedemptionTitleMedium,
            setValue: v => config.StarterRedemptionTitleMedium = v,
            name: () => "Medium Reward Title",
            tooltip: () => "Must match your Twitch channel point reward title exactly");
        api.AddNumberOption(manifest,
            getValue: () => config.StarterRedemptionPointsMedium,
            setValue: v => config.StarterRedemptionPointsMedium = v,
            name: () => "Medium Reward Points",
            min: 1, max: 10000);
        api.AddTextOption(manifest,
            getValue: () => config.StarterRedemptionTitleLarge,
            setValue: v => config.StarterRedemptionTitleLarge = v,
            name: () => "Large Reward Title",
            tooltip: () => "Must match your Twitch channel point reward title exactly");
        api.AddNumberOption(manifest,
            getValue: () => config.StarterRedemptionPointsLarge,
            setValue: v => config.StarterRedemptionPointsLarge = v,
            name: () => "Large Reward Points",
            min: 1, max: 10000);

        api.AddSectionTitle(manifest, () => "Auto Trigger (Chaos Gods)");
        api.AddBoolOption(manifest,
            getValue: () => config.AutoTriggerEnabled,
            setValue: v => config.AutoTriggerEnabled = v,
            name: () => "Enabled",
            tooltip: () => "Automatically fire a random sabotage if chat goes quiet");
        api.AddNumberOption(manifest,
            getValue: () => config.AutoTriggerMinutes,
            setValue: v => config.AutoTriggerMinutes = v,
            name: () => "Quiet Period (minutes)",
            tooltip: () => "How many minutes of no sabotages before auto-triggering",
            min: 1, max: 30);
        api.AddTextOption(manifest,
            getValue: () => config.AutoTriggerPool,
            setValue: v => config.AutoTriggerPool = v,
            name: () => "Sabotage Pool",
            tooltip: () => "Comma-separated list of !buy commands to pick from");
        api.AddTextOption(manifest,
            getValue: () => config.ForceChaosKey,
            setValue: v => config.ForceChaosKey = v,
            name: () => "Force Chaos Key",
            tooltip: () => "Press this key in-game to immediately fire a random sabotage from the pool above — ignores the Enabled toggle and quiet-period cooldown. Handy for clip farming on demand (default: F7)");

        // ─── TikTok ───────────────────────────────────────────────────────────

        api.AddPage(manifest, "tiktok", () => "TikTok (Tikfinity)");

        api.AddParagraph(manifest, () => "Requires the Tikfinity Desktop App running on this PC. Enable below then restart the game.");
        api.AddBoolOption(manifest,
            getValue: () => config.EnableTikTok,
            setValue: v => config.EnableTikTok = v,
            name: () => "Enable TikTok Integration",
            tooltip: () => "Connect to Tikfinity's local WebSocket to receive TikTok events");
        api.AddNumberOption(manifest,
            getValue: () => config.TikTokPort,
            setValue: v => config.TikTokPort = v,
            name: () => "Tikfinity Port",
            tooltip: () => "Default is 21213 — check Tikfinity's Event API page if this differs",
            min: 1024, max: 65535);

        api.AddSectionTitle(manifest, () => "Point Bonuses");
        api.AddNumberOption(manifest,
            getValue: () => config.TikTokChatBonus,
            setValue: v => config.TikTokChatBonus = v,
            name: () => "Chat Bonus",
            tooltip: () => "Points per chat message (subject to cooldown)",
            min: 0, max: 1000);
        api.AddNumberOption(manifest,
            getValue: () => config.TikTokChatCooldown,
            setValue: v => config.TikTokChatCooldown = v,
            name: () => "Chat Cooldown (seconds)",
            min: 0, max: 600);
        api.AddNumberOption(manifest,
            getValue: () => config.TikTokFollowBonus,
            setValue: v => config.TikTokFollowBonus = v,
            name: () => "Follow Bonus",
            min: 0, max: 1000);
        api.AddNumberOption(manifest,
            getValue: () => config.TikTokShareBonus,
            setValue: v => config.TikTokShareBonus = v,
            name: () => "Share Bonus",
            min: 0, max: 1000);
        api.AddNumberOption(manifest,
            getValue: () => config.TikTokSubBonus,
            setValue: v => config.TikTokSubBonus = v,
            name: () => "Subscribe Bonus",
            min: 0, max: 10000);
        api.AddNumberOption(manifest,
            getValue: () => config.TikTokLikeBonus,
            setValue: v => config.TikTokLikeBonus = v,
            name: () => "Like Bonus (per like)",
            min: 0, max: 100);
        api.AddNumberOption(manifest,
            getValue: () => config.TikTokPointsPerDiamond,
            setValue: v => config.TikTokPointsPerDiamond = v,
            name: () => "Points Per Gift Diamond",
            tooltip: () => "Gifts are valued in TikTok diamonds — how many chaos points each diamond is worth",
            min: 0, max: 100);

        // ─── YouTube ──────────────────────────────────────────────────────────

        api.AddPage(manifest, "youtube", () => "YouTube (via Streamer.bot) [BETA]");

        api.AddSectionTitle(manifest, () => "YouTube Settings (BETA)");
        api.AddParagraph(manifest, () => "⚠️ Experimental and not yet fully tested. Connects YouTube Live Chat via Streamer.bot. Make sure Streamer.bot is running and connected to YouTube before enabling.");
        api.AddBoolOption(manifest,
            getValue: () => config.YouTubeEnabled,
            setValue: v => config.YouTubeEnabled = v,
            name: () => "Enabled",
            tooltip: () => "Enable YouTube Live Chat integration via Streamer.bot");
        api.AddNumberOption(manifest,
            getValue: () => config.StreamerbotPort,
            setValue: v => config.StreamerbotPort = v,
            name: () => "Streamer.bot Port",
            tooltip: () => "Port Streamer.bot WebSocket is running on (default: 8080)",
            min: 1024, max: 65535);

        // ─── OBS Overlay (Shop/Feed) ──────────────────────────────────────────

        api.AddPage(manifest, "overlay", () => "OBS Overlay (Shop/Feed)");

        api.AddSectionTitle(manifest, () => "Connection");
        api.AddNumberOption(manifest,
            getValue: () => config.OverlayPort,
            setValue: v => config.OverlayPort = v,
            name: () => "Port",
            tooltip: () => "OBS browser source port (default: 7373)",
            min: 1024, max: 65535);
        api.AddBoolOption(manifest,
            getValue: () => config.NotifyOverlayConnected,
            setValue: v => config.NotifyOverlayConnected = v,
            name: () => "Notify Overlay Connected",
            tooltip: () => "Show an in-game HUD message when the OBS overlay connects");

        api.AddSectionTitle(manifest, () => "Layout Mode");
        api.AddTextOption(manifest,
            getValue: () => config.OverlayMode,
            setValue: v => config.OverlayMode = v,
            name: () => "Mode",
            tooltip: () => "Sidebar = vertical panels, Ticker = horizontal scrolling bar",
            allowedValues: new[] { "Sidebar", "Ticker" });
        api.AddTextOption(manifest,
            getValue: () => config.OverlayPanelOrder,
            setValue: v => config.OverlayPanelOrder = v,
            name: () => "Panel Order (Sidebar)",
            tooltip: () => "Comma-separated order of panels: shop, feed, leaderboard");

        api.AddSectionTitle(manifest, () => "Ticker Settings");
        api.AddTextOption(manifest,
            getValue: () => config.OverlayTickerPosition,
            setValue: v => config.OverlayTickerPosition = v,
            name: () => "Ticker Position",
            allowedValues: new[] { "Top", "Bottom" });
        api.AddTextOption(manifest,
            getValue: () => config.OverlayTickerSpeed,
            setValue: v => config.OverlayTickerSpeed = v,
            name: () => "Ticker Speed",
            allowedValues: new[] { "Slow", "Medium", "Fast" });

        api.AddSectionTitle(manifest, () => "Panels");
        api.AddBoolOption(manifest,
            getValue: () => config.OverlayShowShop,
            setValue: v => config.OverlayShowShop = v,
            name: () => "Show Shop");
        api.AddBoolOption(manifest,
            getValue: () => config.OverlayShowFeed,
            setValue: v => config.OverlayShowFeed = v,
            name: () => "Show Feed");
        api.AddBoolOption(manifest,
            getValue: () => config.OverlayShowLeaderboard,
            setValue: v => config.OverlayShowLeaderboard = v,
            name: () => "Show Leaderboard");
        api.AddBoolOption(manifest,
            getValue: () => config.OverlayShowMetaEffects,
            setValue: v => config.OverlayShowMetaEffects = v,
            name: () => "Show Meta Effects",
            tooltip: () => "Show active raid meta effects (double points, halved costs)");
        api.AddNumberOption(manifest,
            getValue: () => config.OverlayMaxShopItems,
            setValue: v => config.OverlayMaxShopItems = v,
            name: () => "Max Shop Items",
            min: 1, max: 30);
        api.AddNumberOption(manifest,
            getValue: () => config.OverlayMaxFeedItems,
            setValue: v => config.OverlayMaxFeedItems = v,
            name: () => "Max Feed Items",
            min: 1, max: 20);
        api.AddNumberOption(manifest,
            getValue: () => config.OverlayMaxLeaderboardItems,
            setValue: v => config.OverlayMaxLeaderboardItems = v,
            name: () => "Max Leaderboard Items",
            min: 1, max: 10);

        api.AddSectionTitle(manifest, () => "Embedded Chat & Alerts");
        api.AddParagraph(manifest, () => "Fold the chat overlay and alert popup into this same browser source, so you only need to add one OBS source instead of three. Off by default so upgrading doesn't duplicate anything you've already added separately.");
        api.AddBoolOption(manifest,
            getValue: () => config.OverlayIncludeChat,
            setValue: v => config.OverlayIncludeChat = v,
            name: () => "Include Chat",
            tooltip: () => "Show the Twitch/TikTok chat feed inside this overlay instead of (or in addition to) the separate /chat browser source");
        api.AddTextOption(manifest,
            getValue: () => config.OverlayChatCorner,
            setValue: v => config.OverlayChatCorner = v,
            name: () => "Chat Corner",
            allowedValues: new[] { "TopLeft", "TopRight", "BottomLeft", "BottomRight" });
        api.AddBoolOption(manifest,
            getValue: () => config.OverlayIncludeAlerts,
            setValue: v => config.OverlayIncludeAlerts = v,
            name: () => "Include Alert Popups",
            tooltip: () => "Show the transient alert popup inside this overlay instead of (or in addition to) the separate /alert browser source");

        api.AddSectionTitle(manifest, () => "Appearance");
        api.AddNumberOption(manifest,
            getValue: () => config.OverlayWidth,
            setValue: v => config.OverlayWidth = v,
            name: () => "Width (Sidebar)",
            tooltip: () => "Width of the sidebar overlay in pixels",
            min: 200, max: 500);
        api.AddNumberOption(manifest,
            getValue: () => config.OverlayFontSize,
            setValue: v => config.OverlayFontSize = v,
            name: () => "Font Size",
            min: 6, max: 16);
        api.AddTextOption(manifest,
            getValue: () => config.OverlayTheme,
            setValue: v => config.OverlayTheme = v,
            name: () => "Theme",
            allowedValues: new[] { "Stardew", "Dark", "Light", "Custom" });

        api.AddSectionTitle(manifest, () => "Custom Theme Colors");
        api.AddTextOption(manifest,
            getValue: () => config.OverlayCustomBg,
            setValue: v => config.OverlayCustomBg = v,
            name: () => "Background Color",
            tooltip: () => "Hex color e.g. #1a1a2e");
        api.AddTextOption(manifest,
            getValue: () => config.OverlayCustomAccent,
            setValue: v => config.OverlayCustomAccent = v,
            name: () => "Accent Color",
            tooltip: () => "Hex color e.g. #9147ff");
        api.AddTextOption(manifest,
            getValue: () => config.OverlayCustomText,
            setValue: v => config.OverlayCustomText = v,
            name: () => "Text Color",
            tooltip: () => "Hex color e.g. #efeff1");

        // ─── Chat Feed Display ────────────────────────────────────────────────
        // The chat message feed (as opposed to the shop/feed/leaderboard overlay
        // above) — shown both as an in-game HUD and as a separate OBS browser source.

        api.AddPage(manifest, "chatfeed", () => "Chat Feed Display");

        api.AddSectionTitle(manifest, () => "In-Game HUD");
        api.AddBoolOption(manifest,
            getValue: () => config.EnableChatOverlay,
            setValue: v => config.EnableChatOverlay = v,
            name: () => "Enable In-Game Chat Overlay",
            tooltip: () => "Show a chat feed in the corner of the screen while playing");
        api.AddTextOption(manifest,
            getValue: () => config.ChatOverlayCorner,
            setValue: v => config.ChatOverlayCorner = v,
            name: () => "Chat Overlay Corner",
            tooltip: () => "Which corner to display the chat feed",
            allowedValues: new[] { "TopLeft", "TopRight", "BottomLeft", "BottomRight" });
        api.AddNumberOption(manifest,
            getValue: () => config.ChatOverlayMaxMessages,
            setValue: v => config.ChatOverlayMaxMessages = v,
            name: () => "Max Messages",
            tooltip: () => "How many messages to show at once",
            min: 1, max: 20);
        api.AddNumberOption(manifest,
            getValue: () => config.ChatOverlayMessageTTL,
            setValue: v => config.ChatOverlayMessageTTL = v,
            name: () => "Message Display Time (seconds)",
            tooltip: () => "How long messages stay visible before disappearing",
            min: 3, max: 60);

        api.AddSectionTitle(manifest, () => "OBS Browser Source");
        api.AddBoolOption(manifest,
            getValue: () => config.EnableChatBrowserSource,
            setValue: v => config.EnableChatBrowserSource = v,
            name: () => "Enable Chat Browser Source",
            tooltip: () => "Serve a chat overlay at http://localhost:7373/chat for OBS");

        // ─── Auto-Clipping ────────────────────────────────────────────────────

        api.AddPage(manifest, "autoclip", () => "Auto-Clipping");

        api.AddParagraph(manifest, () => "Automatically creates a Twitch clip a few seconds after a sabotage or blessing fires, so you never miss capturing chaos for later. Requires clips:edit scope on your Twitch token.");
        api.AddBoolOption(manifest,
            getValue: () => config.AutoClipEnabled,
            setValue: v => config.AutoClipEnabled = v,
            name: () => "Enabled",
            tooltip: () => "Master switch for automatic clip creation");
        api.AddNumberOption(manifest,
            getValue: () => config.ClipDelaySeconds,
            setValue: v => config.ClipDelaySeconds = v,
            name: () => "Clip Delay (seconds)",
            tooltip: () => "How long to wait after a trigger before creating the clip, so the effect is actually visible on screen",
            min: 0, max: 30);

        api.AddSectionTitle(manifest, () => "Clip By Tier");
        api.AddParagraph(manifest, () => "Which sabotage tiers get auto-clipped. Applies to !buy purchases, bits, donations, channel points, TikTok, and Chaos Gods auto-triggers alike.");
        api.AddBoolOption(manifest,
            getValue: () => config.ClipNuisance,
            setValue: v => config.ClipNuisance = v,
            name: () => "Clip Nuisance Tier");
        api.AddNumberOption(manifest,
            getValue: () => config.ClipNuisanceCooldownSeconds,
            setValue: v => config.ClipNuisanceCooldownSeconds = v,
            name: () => "Nuisance Cooldown (seconds)",
            min: 0, max: 1800);
        api.AddBoolOption(manifest,
            getValue: () => config.ClipDisruptive,
            setValue: v => config.ClipDisruptive = v,
            name: () => "Clip Disruptive Tier");
        api.AddNumberOption(manifest,
            getValue: () => config.ClipDisruptiveCooldownSeconds,
            setValue: v => config.ClipDisruptiveCooldownSeconds = v,
            name: () => "Disruptive Cooldown (seconds)",
            min: 0, max: 1800);
        api.AddBoolOption(manifest,
            getValue: () => config.ClipPainful,
            setValue: v => config.ClipPainful = v,
            name: () => "Clip Painful Tier");
        api.AddNumberOption(manifest,
            getValue: () => config.ClipPainfulCooldownSeconds,
            setValue: v => config.ClipPainfulCooldownSeconds = v,
            name: () => "Painful Cooldown (seconds)",
            min: 0, max: 1800);
        api.AddBoolOption(manifest,
            getValue: () => config.ClipDevastating,
            setValue: v => config.ClipDevastating = v,
            name: () => "Clip Devastating Tier");
        api.AddNumberOption(manifest,
            getValue: () => config.ClipDevastatingCooldownSeconds,
            setValue: v => config.ClipDevastatingCooldownSeconds = v,
            name: () => "Devastating Cooldown (seconds)",
            min: 0, max: 1800);
        api.AddBoolOption(manifest,
            getValue: () => config.ClipBlessings,
            setValue: v => config.ClipBlessings = v,
            name: () => "Clip Blessings");

        api.AddSectionTitle(manifest, () => "Raids");
        api.AddBoolOption(manifest,
            getValue: () => config.ClipRaids,
            setValue: v => config.ClipRaids = v,
            name: () => "Clip Raid Events",
            tooltip: () => "Also clip the Chaos/Blessing/Meta event rolled when a raid comes in");
        api.AddNumberOption(manifest,
            getValue: () => config.ClipRaidsCooldownSeconds,
            setValue: v => config.ClipRaidsCooldownSeconds = v,
            name: () => "Raid Clip Cooldown (seconds)",
            min: 0, max: 1800);

        // ─── Animal Challenge ─────────────────────────────────────────────────

        api.AddPage(manifest, "challenge", () => "Animal Challenge");

        api.AddParagraph(manifest, () => "A togglable \"100 Chicken Challenge\"-style goal: try to reach a target number of a given animal type on the farm. Chat can buy !buy addanimal to help or !buy spookanimal to hurt your progress.");
        api.AddBoolOption(manifest,
            getValue: () => config.ChallengeModeEnabled,
            setValue: v => config.ChallengeModeEnabled = v,
            name: () => "Enabled",
            tooltip: () => "Turns on the challenge counter and the addanimal/spookanimal shop commands");
        api.AddTextOption(manifest,
            getValue: () => config.ChallengeAnimalFilter,
            setValue: v => config.ChallengeAnimalFilter = v,
            name: () => "Animal Type",
            tooltip: () => "Which animals count toward the goal — matches any animal type containing this text (e.g. \"Chicken\" matches White/Brown/Void/Golden Chicken). Use \"Any\" to count every farm animal.");
        api.AddNumberOption(manifest,
            getValue: () => config.ChallengeGoalCount,
            setValue: v => config.ChallengeGoalCount = v,
            name: () => "Goal Count",
            tooltip: () => "How many matching animals counts as reaching the challenge",
            min: 1, max: 999);

        // ─── Multiplayer Targeting ────────────────────────────────────────────

        api.AddPage(manifest, "multiplayer", () => "Multiplayer Targeting");

        api.AddParagraph(manifest, () => "Playing co-op with other streamers? Each co-op player is their own broadcaster with their own Twitch channel. Enable a slot below, type in their channel name, and this mod joins that channel too — any command typed there (by them or their own viewers) lands on THEIR farmhand instead of you, no mod install needed on their end. Their in-game character name must match their channel name EXACTLY (case-insensitive) or targeting falls back to you.");
        api.AddParagraph(manifest, () => "Only sabotages that change save-file state (money, health/stamina, buffs, inventory, nearby monster/explosion spawns) can be redirected this way. Effects that read your keyboard or draw straight to a screen — Confused, jump scares, bans, warps — can only ever affect whoever's game is actually running this mod, so those always land on you regardless of this setting.");
        api.AddBoolOption(manifest,
            getValue: () => config.MultiplayerTargetingEnabled,
            setValue: v => config.MultiplayerTargetingEnabled = v,
            name: () => "Enabled",
            tooltip: () => "Master switch — turns co-op channel joining and farmhand targeting on or off");

        api.AddSectionTitle(manifest, () => "Player 2");
        api.AddTextOption(manifest,
            getValue: () => config.MultiplayerPlayer2Channel,
            setValue: v => config.MultiplayerPlayer2Channel = v,
            name: () => "Twitch Channel",
            tooltip: () => "Their Twitch channel name (lowercase) — must match their in-game character name exactly");
        api.AddBoolOption(manifest,
            getValue: () => config.MultiplayerPlayer2Enabled,
            setValue: v => config.MultiplayerPlayer2Enabled = v,
            name: () => "Connect",
            tooltip: () => "Join this channel and start routing commands typed there to their farmhand. Takes effect as soon as you back out of this menu.");

        api.AddSectionTitle(manifest, () => "Player 3");
        api.AddTextOption(manifest,
            getValue: () => config.MultiplayerPlayer3Channel,
            setValue: v => config.MultiplayerPlayer3Channel = v,
            name: () => "Twitch Channel",
            tooltip: () => "Their Twitch channel name (lowercase) — must match their in-game character name exactly");
        api.AddBoolOption(manifest,
            getValue: () => config.MultiplayerPlayer3Enabled,
            setValue: v => config.MultiplayerPlayer3Enabled = v,
            name: () => "Connect",
            tooltip: () => "Join this channel and start routing commands typed there to their farmhand. Takes effect as soon as you back out of this menu.");

        api.AddSectionTitle(manifest, () => "Player 4");
        api.AddTextOption(manifest,
            getValue: () => config.MultiplayerPlayer4Channel,
            setValue: v => config.MultiplayerPlayer4Channel = v,
            name: () => "Twitch Channel",
            tooltip: () => "Their Twitch channel name (lowercase) — must match their in-game character name exactly");
        api.AddBoolOption(manifest,
            getValue: () => config.MultiplayerPlayer4Enabled,
            setValue: v => config.MultiplayerPlayer4Enabled = v,
            name: () => "Connect",
            tooltip: () => "Join this channel and start routing commands typed there to their farmhand. Takes effect as soon as you back out of this menu.");

        // ─── Ignored Users ────────────────────────────────────────────────────

        api.AddPage(manifest, "ignored", () => "Ignored Users");

        api.AddParagraph(manifest, () => $"Press {config.IgnoreListKey} in-game to open the Ignored Users manager.");
        api.AddTextOption(manifest,
            getValue: () => config.IgnoreListKey,
            setValue: v => config.IgnoreListKey = v,
            name: () => "Open Menu Key",
            tooltip: () => "Key to press in-game to open the Ignored Users manager (default: F8)");
    }

    private static void ResetConfig(ModConfig config)
    {
        var defaults = new ModConfig();
        config.IgnoredUsers              = defaults.IgnoredUsers;
        config.ChannelName               = defaults.ChannelName;
        config.BotUsername               = defaults.BotUsername;
        config.BroadcasterUserId         = defaults.BroadcasterUserId;
        config.ShopUrl                   = defaults.ShopUrl;
        config.PassiveTickMinutes        = defaults.PassiveTickMinutes;
        config.BasePassivePoints         = defaults.BasePassivePoints;
        config.ChatBonusPoints           = defaults.ChatBonusPoints;
        config.ChatBonusCooldownSeconds  = defaults.ChatBonusCooldownSeconds;
        config.MultiplierNone            = defaults.MultiplierNone;
        config.MultiplierPrime           = defaults.MultiplierPrime;
        config.MultiplierT1              = defaults.MultiplierT1;
        config.MultiplierT2              = defaults.MultiplierT2;
        config.MultiplierT3              = defaults.MultiplierT3;
        config.FollowBonus               = defaults.FollowBonus;
        config.SubBonus                  = defaults.SubBonus;
        config.GiftSubBonusEach          = defaults.GiftSubBonusEach;
        config.RaidLeaderPointsPerViewer = defaults.RaidLeaderPointsPerViewer;
        config.RaidViewerBonus           = defaults.RaidViewerBonus;
        config.BitsPerPoint              = defaults.BitsPerPoint;
        config.SmallBitThreshold         = defaults.SmallBitThreshold;
        config.MediumBitThreshold        = defaults.MediumBitThreshold;
        config.LargeBitThreshold         = defaults.LargeBitThreshold;
        config.DonorDriveEnabled         = defaults.DonorDriveEnabled;
        config.DonorDriveApiBaseUrl      = defaults.DonorDriveApiBaseUrl;
        config.DonorDriveParticipantId   = defaults.DonorDriveParticipantId;
        config.DonorDrivePollSeconds     = defaults.DonorDrivePollSeconds;
        config.DonationPointsPerCent     = defaults.DonationPointsPerCent;
        config.DonationSmallThreshold    = defaults.DonationSmallThreshold;
        config.DonationMediumThreshold   = defaults.DonationMediumThreshold;
        config.DonationLargeThreshold    = defaults.DonationLargeThreshold;
        config.DonationMassiveThreshold  = defaults.DonationMassiveThreshold;
        config.EnableChatCommands        = defaults.EnableChatCommands;
        config.EnableChannelPoints       = defaults.EnableChannelPoints;
        config.EnableBitEvents           = defaults.EnableBitEvents;
        config.EnableFollowBonus         = defaults.EnableFollowBonus;
        config.EnableRaidEvents          = defaults.EnableRaidEvents;
        config.NotifyOverlayConnected    = defaults.NotifyOverlayConnected;
        config.EnableChatOverlay         = defaults.EnableChatOverlay;
        config.ChatOverlayCorner         = defaults.ChatOverlayCorner;
        config.ChatOverlayMaxMessages    = defaults.ChatOverlayMaxMessages;
        config.ChatOverlayMessageTTL     = defaults.ChatOverlayMessageTTL;
        config.EnableChatBrowserSource   = defaults.EnableChatBrowserSource;
        config.EnableTikTok              = defaults.EnableTikTok;
        config.TikTokPort                = defaults.TikTokPort;
        config.TikTokChatBonus           = defaults.TikTokChatBonus;
        config.TikTokChatCooldown        = defaults.TikTokChatCooldown;
        config.TikTokFollowBonus         = defaults.TikTokFollowBonus;
        config.TikTokShareBonus          = defaults.TikTokShareBonus;
        config.TikTokSubBonus            = defaults.TikTokSubBonus;
        config.TikTokLikeBonus           = defaults.TikTokLikeBonus;
        config.TikTokPointsPerDiamond    = defaults.TikTokPointsPerDiamond;
        config.EnableStarterRedemption        = defaults.EnableStarterRedemption;
        config.StarterRedemptionTitleSmall    = defaults.StarterRedemptionTitleSmall;
        config.StarterRedemptionPointsSmall   = defaults.StarterRedemptionPointsSmall;
        config.StarterRedemptionTitleMedium   = defaults.StarterRedemptionTitleMedium;
        config.StarterRedemptionPointsMedium  = defaults.StarterRedemptionPointsMedium;
        config.StarterRedemptionTitleLarge    = defaults.StarterRedemptionTitleLarge;
        config.StarterRedemptionPointsLarge   = defaults.StarterRedemptionPointsLarge;
        config.OverlayPort               = defaults.OverlayPort;
        config.PasteTokenKey             = defaults.PasteTokenKey;
        config.OverlayMode               = defaults.OverlayMode;
        config.OverlayPanelOrder         = defaults.OverlayPanelOrder;
        config.OverlayShowShop           = defaults.OverlayShowShop;
        config.OverlayShowFeed           = defaults.OverlayShowFeed;
        config.OverlayShowLeaderboard    = defaults.OverlayShowLeaderboard;
        config.OverlayShowMetaEffects    = defaults.OverlayShowMetaEffects;
        config.OverlayMaxShopItems       = defaults.OverlayMaxShopItems;
        config.OverlayMaxFeedItems       = defaults.OverlayMaxFeedItems;
        config.OverlayMaxLeaderboardItems = defaults.OverlayMaxLeaderboardItems;
        config.OverlayIncludeChat        = defaults.OverlayIncludeChat;
        config.OverlayIncludeAlerts      = defaults.OverlayIncludeAlerts;
        config.OverlayChatCorner         = defaults.OverlayChatCorner;
        config.OverlayWidth              = defaults.OverlayWidth;
        config.OverlayFontSize           = defaults.OverlayFontSize;
        config.OverlayTheme              = defaults.OverlayTheme;
        config.OverlayTickerPosition     = defaults.OverlayTickerPosition;
        config.OverlayTickerSpeed        = defaults.OverlayTickerSpeed;
        config.OverlayCustomBg           = defaults.OverlayCustomBg;
        config.OverlayCustomAccent       = defaults.OverlayCustomAccent;
        config.OverlayCustomText         = defaults.OverlayCustomText;
        config.AutoTriggerEnabled        = defaults.AutoTriggerEnabled;
        config.AutoTriggerMinutes        = defaults.AutoTriggerMinutes;
        config.AutoTriggerPool           = defaults.AutoTriggerPool;
        config.ForceChaosKey             = defaults.ForceChaosKey;
        config.AutoClipEnabled                = defaults.AutoClipEnabled;
        config.ClipNuisance                   = defaults.ClipNuisance;
        config.ClipDisruptive                 = defaults.ClipDisruptive;
        config.ClipPainful                    = defaults.ClipPainful;
        config.ClipDevastating                = defaults.ClipDevastating;
        config.ClipBlessings                  = defaults.ClipBlessings;
        config.ClipDelaySeconds               = defaults.ClipDelaySeconds;
        config.ClipNuisanceCooldownSeconds    = defaults.ClipNuisanceCooldownSeconds;
        config.ClipDisruptiveCooldownSeconds  = defaults.ClipDisruptiveCooldownSeconds;
        config.ClipPainfulCooldownSeconds     = defaults.ClipPainfulCooldownSeconds;
        config.ClipDevastatingCooldownSeconds = defaults.ClipDevastatingCooldownSeconds;
        config.ClipRaids                      = defaults.ClipRaids;
        config.ClipRaidsCooldownSeconds       = defaults.ClipRaidsCooldownSeconds;
        config.ChallengeModeEnabled           = defaults.ChallengeModeEnabled;
        config.ChallengeAnimalFilter          = defaults.ChallengeAnimalFilter;
        config.ChallengeGoalCount             = defaults.ChallengeGoalCount;
        config.MultiplayerTargetingEnabled    = defaults.MultiplayerTargetingEnabled;
        config.MultiplayerPlayer2Enabled      = defaults.MultiplayerPlayer2Enabled;
        config.MultiplayerPlayer2Channel      = defaults.MultiplayerPlayer2Channel;
        config.MultiplayerPlayer3Enabled      = defaults.MultiplayerPlayer3Enabled;
        config.MultiplayerPlayer3Channel      = defaults.MultiplayerPlayer3Channel;
        config.MultiplayerPlayer4Enabled      = defaults.MultiplayerPlayer4Enabled;
        config.MultiplayerPlayer4Channel      = defaults.MultiplayerPlayer4Channel;
    }
}
