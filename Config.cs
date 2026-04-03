namespace SDVChatVsStreamer;

public class ModConfig
{
    // ─── Ignored Users ───────────────────────────────────────────────────────
    /// <summary>Comma-separated list of usernames to ignore (bots, etc.)</summary>
    public string IgnoredUsers { get; set; } =
        "bardbouncerbot, streamelements, nightbot, moobot, streamlabs, stay_hydrated_bot, soundalerts, fossabot";

    // ─── Auth ────────────────────────────────────────────────────────────────
    public string AuthConfigDirName { get; set; } = "TwitchAuth";

    // ─── Twitch Channel ──────────────────────────────────────────────────────
    public string ChannelName { get; set; } = "neighborhoodofmusic";
    public string BotUsername { get; set; } = "bardbouncerbot";
    public string BroadcasterUserId { get; set; } = "your_broadcaster_user_id";

    // ─── Feature Toggles ─────────────────────────────────────────────────────
    public bool EnableChatCommands { get; set; } = true;
    public bool EnableChannelPoints { get; set; } = true;
    public bool EnableBitEvents { get; set; } = true;
    public bool EnableFollowBonus { get; set; } = true;
    public bool EnableRaidEvents { get; set; } = true;

    // ─── Passive Points ──────────────────────────────────────────────────────
    public int PassiveTickMinutes { get; set; } = 5;
    public int BasePassivePoints { get; set; } = 10;

    // ─── Chat Bonus ──────────────────────────────────────────────────────────
    public int ChatBonusPoints { get; set; } = 5;
    public int ChatBonusCooldownSeconds { get; set; } = 60;

    // ─── Event Bonuses ───────────────────────────────────────────────────────
    public int FollowBonus { get; set; } = 25;
    public int SubBonus { get; set; } = 100;
    public int GiftSubBonusEach { get; set; } = 50;
    public int RaidLeaderPointsPerViewer { get; set; } = 2;
    public int RaidViewerBonus { get; set; } = 15;
    public int BitsPerPoint { get; set; } = 1;

    // ─── Sub Multipliers ─────────────────────────────────────────────────────
    public float MultiplierNone { get; set; } = 1.00f;
    public float MultiplierPrime { get; set; } = 1.75f;
    public float MultiplierT1 { get; set; } = 1.50f;
    public float MultiplierT2 { get; set; } = 2.00f;
    public float MultiplierT3 { get; set; } = 3.00f;

    // ─── Bit Thresholds ──────────────────────────────────────────────────────
    public int SmallBitThreshold { get; set; } = 100;
    public int MediumBitThreshold { get; set; } = 500;
    public int LargeBitThreshold { get; set; } = 1000;

    // ─── Key Bindings ────────────────────────────────────────────────────────
    public string PasteTokenKey { get; set; } = "F9";
    public string IgnoreListKey { get; set; } = "F8";

    // ─── Channel Point Redemptions ────────────────────────────────────────────
    public bool NotifyOverlayConnected { get; set; } = true;

    // ─── Channel Point Redemptions ────────────────────────────────────────────
    public bool EnableStarterRedemption { get; set; } = true;

    public string StarterRedemptionTitleSmall  { get; set; } = "Get Chaos Points (Small)";
    public int    StarterRedemptionPointsSmall { get; set; } = 50;

    public string StarterRedemptionTitleMedium  { get; set; } = "Get Chaos Points (Medium)";
    public int    StarterRedemptionPointsMedium { get; set; } = 150;

    public string StarterRedemptionTitleLarge  { get; set; } = "Get Chaos Points (Large)";
    public int    StarterRedemptionPointsLarge { get; set; } = 400;

    // ─── Chat Overlay ─────────────────────────────────────────────────────────
    public bool   EnableChatOverlay       { get; set; } = true;
    public string ChatOverlayCorner       { get; set; } = "BottomLeft";
    public int    ChatOverlayMaxMessages  { get; set; } = 6;
    public int    ChatOverlayMessageTTL   { get; set; } = 8; // seconds before messages disappear
    public bool   EnableChatBrowserSource { get; set; } = true;

    // ─── TikTok ───────────────────────────────────────────────────────────────
    public bool EnableTikTok           { get; set; } = false;
    public int  TikTokPort             { get; set; } = 21213;
    public string[] BlockedKeywords    { get; set; } = Array.Empty<string>();
    public int  TikTokChatBonus        { get; set; } = 5;
    public int  TikTokChatCooldown     { get; set; } = 60;
    public int  TikTokFollowBonus      { get; set; } = 25;
    public int  TikTokShareBonus       { get; set; } = 10;
    public int  TikTokSubBonus         { get; set; } = 100;
    public int  TikTokLikeBonus        { get; set; } = 1;
    public int  TikTokPointsPerDiamond { get; set; } = 1;
    public int OverlayPort { get; set; } = 7373;

    // Layout
    public string OverlayMode { get; set; } = "Sidebar"; // Sidebar | Ticker
    public string OverlayPanelOrder { get; set; } = "shop,feed,leaderboard";

    // Sidebar panels
    public bool OverlayShowShop { get; set; } = true;
    public bool OverlayShowFeed { get; set; } = true;
    public bool OverlayShowLeaderboard { get; set; } = true;
    public bool OverlayShowMetaEffects { get; set; } = true;
    public int OverlayMaxShopItems { get; set; } = 50;
    public int OverlayMaxFeedItems { get; set; } = 5;
    public int OverlayMaxLeaderboardItems { get; set; } = 5;
    public int OverlayWidth { get; set; } = 280;
    public int OverlayFontSize { get; set; } = 8;

    // Ticker
    public string OverlayTickerPosition { get; set; } = "Bottom"; // Top | Bottom
    public string OverlayTickerSpeed { get; set; } = "Slow"; // Slow | Medium | Fast

    // Theme
    public string OverlayTheme { get; set; } = "Stardew"; // Stardew | Dark | Light | Custom
    public string OverlayCustomBg { get; set; } = "#1a1a2e";
    public string OverlayCustomAccent { get; set; } = "#9147ff";
    public string OverlayCustomText { get; set; } = "#efeff1";

    // ─── Database ────────────────────────────────────────────────────────────
    public string DatabaseFileName { get; set; } = "ViewerLedger.db";
}