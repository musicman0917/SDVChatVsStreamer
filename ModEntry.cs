using SDVChatVsStreamer.Economy;
using SDVChatVsStreamer.Overlay;
using SDVChatVsStreamer.Sabotage;
using SDVChatVsStreamer.Sabotage.Sabotages;
using SDVChatVsStreamer.Twitch;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Runtime.InteropServices;

namespace SDVChatVsStreamer;

public class ModEntry : Mod
{
    public static IMonitor? Logger { get; private set; }
    public static Queue<Action> PendingActions { get; } = new();
    public static Action? PendingDismissalAction { get; set; }
    private bool _dismissalWaitTick = false;
    private ModConfig _config = null!;
    private ViewerLedger _ledger = null!;
    private PointsEngine _points = null!;
    private SabotageEngine _sabotage = null!;
    private TwitchManager _twitch = null!;
    private OverlayServer _overlay = null!;

    private DateTime _lastLeaderboardPush = DateTime.MinValue;
    private readonly TimeSpan _leaderboardInterval = TimeSpan.FromSeconds(60);
    private TikTok.TikTokManager?  _tiktok;
    private UI.ChatFeed            _chatFeed = null!;
    private UI.ChatHud             _chatHud  = null!;

    // ─── Entry Point ──────────────────────────────────────────────────────────

    public override void Entry(IModHelper helper)
    {
        Logger  = Monitor;
        _config = helper.ReadConfig<ModConfig>();

        // Auth
        var authDir = Path.Combine(helper.DirectoryPath, _config.AuthConfigDirName);
        TwitchAuth.Init(authDir, Monitor);

        // Economy
        var dbPath = Path.Combine(helper.DirectoryPath, _config.DatabaseFileName);
        _ledger  = new ViewerLedger(dbPath);
        _points  = new PointsEngine(_ledger, Monitor, _config);

        // Sabotage
        _sabotage = new SabotageEngine(_ledger, Monitor);
        SDVChatVsStreamer.Sabotage.Sabotages.ToolSabotageHelper.SetMonitor(Monitor);
        RegisterSabotages();

        // Wire raid event system into points engine for double points
        _points.SetRaidEventSystem(_sabotage.RaidEvents);

        // Chat feed
        _chatFeed = new UI.ChatFeed();
        _chatHud  = new UI.ChatHud(_config, _chatFeed, Monitor, helper);
        helper.Events.Display.RenderedHud += _chatHud.OnRenderedHud;

        // Mr. Qi portrait
        MrQiDialogue.Init(helper);
        _overlay = new OverlayServer(_config, _sabotage, _ledger, Monitor);
        _sabotage.SetOverlay(_overlay);

        // Twitch
        _twitch = new TwitchManager(_config, _points, _sabotage, _ledger, Monitor, _chatFeed, _overlay);

        // SMAPI events
        helper.Events.GameLoop.GameLaunched    += OnGameLaunched;
        helper.Events.GameLoop.UpdateTicked    += OnUpdateTicked;
        helper.Events.GameLoop.SaveLoaded      += OnSaveLoaded;
        helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
        helper.Events.Input.ButtonPressed      += OnButtonPressed;

        Monitor.Log("[ChatVsStreamer] Mod loaded.", LogLevel.Info);
    }

    // ─── Sabotage Registration ────────────────────────────────────────────────

    private void RegisterSabotages()
    {
        // ── Buyable shop ──
        _sabotage.Register(new RainSabotage());
        _sabotage.Register(new DrainEnergySabotage());
        _sabotage.Register(new SpawnMonsterSabotage());
        _sabotage.Register(new SleepSabotage());
        _sabotage.Register(new CrowsSabotage());
        _sabotage.Register(new WarpSabotage());
        _sabotage.Register(new KillFarmSabotage());
        _sabotage.Register(new WeedsSabotage());
        _sabotage.Register(new StealSabotage());
        _sabotage.Register(new StormSabotage());
        _sabotage.Register(new SnowSabotage());
        _sabotage.Register(new WindSabotage());
        _sabotage.Register(new GreenRainSabotage());
        _sabotage.Register(new SpeedUpSabotage());
        _sabotage.Register(new SwarmSabotage());
        _sabotage.Register(new BatSabotage());
        _sabotage.Register(new BombSabotage());
        _sabotage.Register(new BrokeSabotage());
        _sabotage.Register(new TrashSabotage());
        _sabotage.Register(new DizzySabotage());
        _sabotage.Register(new DrunkSabotage());

        // ── Low tier monsters ──
        _sabotage.Register(new SpawnBugSabotage());
        _sabotage.Register(new SpawnGrubSabotage());
        _sabotage.Register(new SpawnGolemSabotage());

        // ── Mid tier monsters ──
        _sabotage.Register(new SpawnFrostBatSabotage());
        _sabotage.Register(new SpawnDustSpriteSabotage());
        _sabotage.Register(new SpawnGhostSabotage());

        // ── High tier monsters ──
        _sabotage.Register(new SpawnSerpentSabotage());
        _sabotage.Register(new SpawnShadowBruteSabotage());
        _sabotage.Register(new SpawnShadowShamanSabotage());
        _sabotage.Register(new SpawnIridiumGolemSabotage());
        _sabotage.Register(new SpawnSquidKidSabotage());

        // ── Weapon sabotages ──
        _sabotage.Register(new WeaponSabotageNormal());
        _sabotage.Register(new WeaponSabotageRandom());
        _sabotage.Register(new WeaponSabotageBetter());
        _sabotage.Register(new WeaponSabotageEpic());
        _sabotage.Register(new WeaponSabotageLegendary());

        // ── Blessings ──
        _sabotage.Register(new RestoreEnergyBlessing());
        _sabotage.Register(new RestoreHealthBlessing());
        _sabotage.Register(new GiveGoldBlessing());
        _sabotage.Register(new WaterCropsBlessing());
        _sabotage.Register(new SpeedBoostBlessing());
        _sabotage.Register(new FertilizeCropsBlessing());
        _sabotage.Register(new ClearDebrisBlessing());
        _sabotage.Register(new SunnyWeatherBlessing());

        // ── Tool sabotages (24 commands) ──
        foreach (var toolSabotage in ToolSabotage.BuildAll())
            _sabotage.Register(toolSabotage);

        // ── Raid pool ──
        _sabotage.RegisterRaidEvent(new RainSabotage());
        _sabotage.RegisterRaidEvent(new StormSabotage());
        _sabotage.RegisterRaidEvent(new SnowSabotage());
        _sabotage.RegisterRaidEvent(new GreenRainSabotage());
        _sabotage.RegisterRaidEvent(new SpawnMonsterSabotage());
        _sabotage.RegisterRaidEvent(new SpawnBugSabotage());
        _sabotage.RegisterRaidEvent(new SpawnFrostBatSabotage());
        _sabotage.RegisterRaidEvent(new SpawnGhostSabotage());
        _sabotage.RegisterRaidEvent(new SpawnSerpentSabotage());
        _sabotage.RegisterRaidEvent(new SpawnShadowBruteSabotage());
        _sabotage.RegisterRaidEvent(new WarpSabotage());
        _sabotage.RegisterRaidEvent(new DrainEnergySabotage());
        _sabotage.RegisterRaidEvent(new SwarmSabotage());
        _sabotage.RegisterRaidEvent(new BombSabotage());
        _sabotage.RegisterRaidEvent(new DrunkSabotage());

        // ── Bit pools ──
        _sabotage.RegisterBitEvent(new DrainEnergySabotage(), BitTier.Small);
        _sabotage.RegisterBitEvent(new RainSabotage(),        BitTier.Small);
        _sabotage.RegisterBitEvent(new TrashSabotage(),       BitTier.Small);
        _sabotage.RegisterBitEvent(new DizzySabotage(),       BitTier.Small);
        _sabotage.RegisterBitEvent(new SpawnBugSabotage(),    BitTier.Small);
        _sabotage.RegisterBitEvent(new SpawnGrubSabotage(),   BitTier.Small);

        _sabotage.RegisterBitEvent(new SpawnMonsterSabotage(),   BitTier.Medium);
        _sabotage.RegisterBitEvent(new CrowsSabotage(),          BitTier.Medium);
        _sabotage.RegisterBitEvent(new StealSabotage(),          BitTier.Medium);
        _sabotage.RegisterBitEvent(new BatSabotage(),            BitTier.Medium);
        _sabotage.RegisterBitEvent(new SpeedUpSabotage(),        BitTier.Medium);
        _sabotage.RegisterBitEvent(new SpawnGolemSabotage(),     BitTier.Medium);
        _sabotage.RegisterBitEvent(new SpawnFrostBatSabotage(),  BitTier.Medium);
        _sabotage.RegisterBitEvent(new SpawnDustSpriteSabotage(), BitTier.Medium);

        _sabotage.RegisterBitEvent(new WarpSabotage(),            BitTier.Large);
        _sabotage.RegisterBitEvent(new SleepSabotage(),           BitTier.Large);
        _sabotage.RegisterBitEvent(new SwarmSabotage(),           BitTier.Large);
        _sabotage.RegisterBitEvent(new BombSabotage(),            BitTier.Large);
        _sabotage.RegisterBitEvent(new KillFarmSabotage(),        BitTier.Large);
        _sabotage.RegisterBitEvent(new SpawnSerpentSabotage(),    BitTier.Large);
        _sabotage.RegisterBitEvent(new SpawnSquidKidSabotage(),   BitTier.Large);
        _sabotage.RegisterBitEvent(new SpawnShadowBruteSabotage(), BitTier.Large);

        Monitor.Log("[ChatVsStreamer] Sabotages registered.", LogLevel.Debug);
    }

    // ─── SMAPI Events ─────────────────────────────────────────────────────────

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        // Register GMCM config menu
        GmcmSetup.Register(Helper, ModManifest, _config);

        var overlayPath = Path.Combine(Helper.DirectoryPath, "Overlay", "overlay.html");
        var mobilePath  = Path.Combine(Helper.DirectoryPath, "Overlay", "mobile.html");
        var chatPath    = Path.Combine(Helper.DirectoryPath, "Overlay", "chat.html");
        _overlay.Start(overlayPath, mobilePath, chatPath, _chatFeed);

        // Push new chat messages to the browser source
        _chatFeed.OnNewMessage   += msg  => _overlay.PushChatMessage(msg);
        _chatFeed.OnRemoveUser   += user => _overlay.PushChatRemoveUser(user);
        _chatFeed.OnRemoveMessage += id  => _overlay.PushChatRemoveMessage(id);
        _twitch.Connect();

        // TikTok via Tikfinity
        // Always create TikTokManager so /tiktok-test endpoint works even without EnableTikTok
        _tiktok = new TikTok.TikTokManager(_config, _points, _ledger, Monitor, _chatFeed, _sabotage);
        _overlay.SetTikTokManager(_tiktok);

        if (_config.EnableTikTok)
            _tiktok.Connect();
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        _twitch.SetGameActive(true);
        Monitor.Log("[ChatVsStreamer] Save loaded — sabotages enabled.", LogLevel.Info);
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        _twitch.SetGameActive(false);
        Monitor.Log("[ChatVsStreamer] Returned to title — sabotages disabled.", LogLevel.Info);
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady) return;

        _overlay.ProcessPendingNotifications();
        SDVChatVsStreamer.Sabotage.Sabotages.ToolSabotageHelper.ProcessPendingActions();

        // Drain general pending actions (e.g. TikTok emotes)
        while (PendingActions.Count > 0)
            PendingActions.Dequeue().Invoke();

        // Dismissal detection — wait one tick after setting so menu has time to open
        if (PendingDismissalAction != null)
        {
            if (_dismissalWaitTick)
            {
                if (Game1.activeClickableMenu == null)
                {
                    var action = PendingDismissalAction;
                    PendingDismissalAction = null;
                    _dismissalWaitTick = false;
                    action.Invoke();
                }
            }
            else
            {
                _dismissalWaitTick = true;
            }
        }

        TikTok.TikTokManager.ProcessPendingReturnToTitle();

        var activeViewers = _twitch.GetActiveViewers();
        _points.Update(activeViewers);

        // Push leaderboard every 60 seconds
        if (DateTime.UtcNow - _lastLeaderboardPush > _leaderboardInterval)
        {
            _lastLeaderboardPush = DateTime.UtcNow;
            var top = _ledger.GetTopViewers(5)
                .Select(v => (v.Username, v.Points))
                .ToList();
            _overlay.PushLeaderboard(top);

            // Push meta effects alongside leaderboard
            var raidEvents = _sabotage.RaidEvents;
            _overlay.PushMetaEffects(
                raidEvents.IsDoublePoints,
                raidEvents.IsHalvedCosts,
                (int)(raidEvents.IsDoublePoints ? (raidEvents._doublePointsExpiry - DateTime.UtcNow).TotalSeconds : 0),
                (int)(raidEvents.IsHalvedCosts  ? (raidEvents._halveCostsExpiry  - DateTime.UtcNow).TotalSeconds : 0));
        }
    }

    // ─── Clipboard P/Invoke (avoids System.Windows.Forms) ────────────────────

    [DllImport("user32.dll")]
    private static extern bool OpenClipboard(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll")]
    private static extern IntPtr GetClipboardData(uint uFormat);

    private static string? GetClipboardText()
    {
        const uint CF_UNICODETEXT = 13;
        try
        {
            if (!OpenClipboard(IntPtr.Zero)) return null;
            var handle = GetClipboardData(CF_UNICODETEXT);
            if (handle == IntPtr.Zero) return null;
            return Marshal.PtrToStringUni(handle);
        }
        catch { return null; }
        finally { CloseClipboard(); }
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (e.Button == SButton.F9 && TwitchAuth._pendingAuth)
        {
            var token = GetClipboardText();
            if (!string.IsNullOrWhiteSpace(token))
            {
                TwitchAuth.SubmitToken(token);
            }
            else
            {
                Game1.addHUDMessage(new HUDMessage(
                    "❌ Clipboard is empty — copy your token URL first.",
                    HUDMessage.error_type));
            }
        }

        // Open ignore list manager
        if (e.Button.ToString().Equals(_config.IgnoreListKey, StringComparison.OrdinalIgnoreCase)
            && Context.IsWorldReady
            && Game1.activeClickableMenu == null)
        {
            Game1.activeClickableMenu = new UI.IgnoreListMenu(_config, Helper);
        }
    }

}