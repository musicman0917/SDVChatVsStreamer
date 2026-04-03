using StardewModdingAPI;
using StardewValley;

namespace SDVChatVsStreamer;

public static class GmcmSetup
{
    public static void Register(IModHelper helper, IManifest manifest, ModConfig config)
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
            save:  () => helper.WriteConfig(config)
        );

        // ─── Pages ────────────────────────────────────────────────────────────

        api.AddPageLink(manifest, "general",      () => "⚙️  General");
        api.AddPageLink(manifest, "points",       () => "⭐ Points & Economy");
        api.AddPageLink(manifest, "multipliers",  () => "🎖️  Sub Multipliers");
        api.AddPageLink(manifest, "bonuses",      () => "🎁 Event Bonuses");
        api.AddPageLink(manifest, "bits",         () => "💰 Bit Thresholds");
        api.AddPageLink(manifest, "features",     () => "🔧 Feature Toggles");
        api.AddPageLink(manifest, "tiktok",       () => "🎵 TikTok (Tikfinity)");
        api.AddPageLink(manifest, "overlay",      () => "🖥️  Overlay");
        api.AddPageLink(manifest, "ignored",      () => "🚫 Ignored Users");

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

        api.AddSectionTitle(manifest, () => "Overlay");
        api.AddNumberOption(manifest,
            getValue: () => config.OverlayPort,
            setValue: v => config.OverlayPort = v,
            name: () => "Overlay Port",
            tooltip: () => "Port for the OBS browser source overlay (default: 7373)",
            min: 1024, max: 65535);

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

        // ─── Sub Multipliers ──────────────────────────────────────────────────

        api.AddPage(manifest, "multipliers", () => "Sub Multipliers");

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

        // ─── Event Bonuses ────────────────────────────────────────────────────

        api.AddPage(manifest, "bonuses", () => "Event Bonuses");

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
        api.AddNumberOption(manifest,
            getValue: () => config.BitsPerPoint,
            setValue: v => config.BitsPerPoint = v,
            name: () => "Points Per Bit",
            tooltip: () => "How many points are awarded per bit cheered",
            min: 0, max: 10);

        // ─── Bit Thresholds ───────────────────────────────────────────────────

        api.AddPage(manifest, "bits", () => "Bit Thresholds");

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

        // ─── Feature Toggles ──────────────────────────────────────────────────

        api.AddPage(manifest, "features", () => "Feature Toggles");

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
        api.AddBoolOption(manifest,
            getValue: () => config.NotifyOverlayConnected,
            setValue: v => config.NotifyOverlayConnected = v,
            name: () => "Notify Overlay Connected",
            tooltip: () => "Show an in-game HUD message when the OBS overlay connects");
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
            name: () => "Chat Overlay Max Messages",
            tooltip: () => "How many messages to show in the in-game overlay",
            min: 1, max: 20);
        api.AddNumberOption(manifest,
            getValue: () => config.ChatOverlayMessageTTL,
            setValue: v => config.ChatOverlayMessageTTL = v,
            name: () => "Message Display Time (seconds)",
            tooltip: () => "How long messages stay visible before disappearing",
            min: 3, max: 60);
        api.AddBoolOption(manifest,
            getValue: () => config.EnableChatBrowserSource,
            setValue: v => config.EnableChatBrowserSource = v,
            name: () => "Enable Chat Browser Source",
            tooltip: () => "Serve a chat overlay at http://localhost:7373/chat for OBS");

        api.AddSectionTitle(manifest, () => "Starter Points Redemptions");
        api.AddParagraph(manifest, () => "Set up to three channel point rewards that award chaos points. Title must match your Twitch reward exactly (case-insensitive).");
        api.AddBoolOption(manifest,
            getValue: () => config.EnableStarterRedemption,
            setValue: v => config.EnableStarterRedemption = v,
            name: () => "Enable CPR Starter Points",
            tooltip: () => "Award chaos points when a viewer redeems a matching channel point reward");

        api.AddSectionTitle(manifest, () => "Small Reward");
        api.AddTextOption(manifest,
            getValue: () => config.StarterRedemptionTitleSmall,
            setValue: v => config.StarterRedemptionTitleSmall = v,
            name: () => "Redemption Title",
            tooltip: () => "Must match your Twitch channel point reward title exactly");
        api.AddNumberOption(manifest,
            getValue: () => config.StarterRedemptionPointsSmall,
            setValue: v => config.StarterRedemptionPointsSmall = v,
            name: () => "Points Awarded",
            min: 1, max: 10000);

        api.AddSectionTitle(manifest, () => "Medium Reward");
        api.AddTextOption(manifest,
            getValue: () => config.StarterRedemptionTitleMedium,
            setValue: v => config.StarterRedemptionTitleMedium = v,
            name: () => "Redemption Title",
            tooltip: () => "Must match your Twitch channel point reward title exactly");
        api.AddNumberOption(manifest,
            getValue: () => config.StarterRedemptionPointsMedium,
            setValue: v => config.StarterRedemptionPointsMedium = v,
            name: () => "Points Awarded",
            min: 1, max: 10000);

        api.AddSectionTitle(manifest, () => "Large Reward");
        api.AddTextOption(manifest,
            getValue: () => config.StarterRedemptionTitleLarge,
            setValue: v => config.StarterRedemptionTitleLarge = v,
            name: () => "Redemption Title",
            tooltip: () => "Must match your Twitch channel point reward title exactly");
        api.AddNumberOption(manifest,
            getValue: () => config.StarterRedemptionPointsLarge,
            setValue: v => config.StarterRedemptionPointsLarge = v,
            name: () => "Points Awarded",
            min: 1, max: 10000);

        // ─── TikTok ───────────────────────────────────────────────────────────────

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

        // ─── Overlay ──────────────────────────────────────────────────────────

        api.AddPage(manifest, "overlay", () => "Overlay");

        api.AddSectionTitle(manifest, () => "Connection");
        api.AddNumberOption(manifest,
            getValue: () => config.OverlayPort,
            setValue: v => config.OverlayPort = v,
            name: () => "Port",
            tooltip: () => "OBS browser source port (default: 7373)",
            min: 1024, max: 65535);

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
        config.OverlayWidth              = defaults.OverlayWidth;
        config.OverlayFontSize           = defaults.OverlayFontSize;
        config.OverlayTheme              = defaults.OverlayTheme;
        config.OverlayTickerPosition     = defaults.OverlayTickerPosition;
        config.OverlayTickerSpeed        = defaults.OverlayTickerSpeed;
        config.OverlayCustomBg           = defaults.OverlayCustomBg;
        config.OverlayCustomAccent       = defaults.OverlayCustomAccent;
        config.OverlayCustomText         = defaults.OverlayCustomText;
    }
}