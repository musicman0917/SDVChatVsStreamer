using Microsoft.Xna.Framework;
using SDVChatVsStreamer.Economy;
using SDVChatVsStreamer.Overlay;
using SDVChatVsStreamer.Sabotage;
using SDVChatVsStreamer.Sabotage.Sabotages;
using SDVChatVsStreamer.Twitch;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System.Runtime.InteropServices;

namespace SDVChatVsStreamer;

public class ModEntry : Mod
{
    public static IMonitor? Logger { get; private set; }
    public static Queue<Action> PendingActions { get; } = new();
    public static Action? PendingDismissalAction { get; set; }
    public static SDVChatVsStreamer.YouTube.YouTubeManager? YouTubeManager { get; private set; }
    private bool _dismissalWaitTick = false;
    private ModConfig _config = null!;
    private ViewerLedger _ledger = null!;
    private PointsEngine _points = null!;
    private SabotageEngine _sabotage = null!;
    private TwitchManager _twitch = null!;
    private SDVChatVsStreamer.YouTube.YouTubeManager? _youtube;
    private OverlayServer _overlay = null!;
    private ClipService _clipService = null!;

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
        _sabotage = new SabotageEngine(_ledger, Monitor, _config);
        _clipService = new ClipService(_config, Monitor, Helper.DirectoryPath);
        _sabotage.SetClipService(_clipService);
        _clipService.SetOverlay(_overlay);
        SDVChatVsStreamer.Sabotage.Sabotages.ToolSabotageHelper.SetMonitor(Monitor);
        RegisterSabotages();

        // Wire raid event system into points engine for double points
        _points.SetRaidEventSystem(_sabotage.RaidEvents);

        // Chat feed
        _chatFeed = new UI.ChatFeed();
        _chatHud  = new UI.ChatHud(_config, _chatFeed, Monitor, helper);
        helper.Events.Display.RenderedHud    += _chatHud.OnRenderedHud;
        helper.Events.Display.MenuChanged        += OnMenuChanged;
        helper.Events.Display.RenderedWorld     += OnRenderedWorld;

        // Mr. Qi portrait
        MrQiDialogue.Init(helper);
        _overlay = new OverlayServer(_config, _sabotage, _ledger, Monitor);
        _sabotage.SetOverlay(_overlay);
        _clipService.SetOverlay(_overlay);

        // Twitch
        _twitch = new TwitchManager(_config, _points, _sabotage, _ledger, Monitor, _chatFeed, _overlay);

        // SMAPI events
        helper.Events.GameLoop.GameLaunched    += OnGameLaunched;
        helper.Events.GameLoop.UpdateTicked    += OnUpdateTicked;
        helper.Events.GameLoop.SaveLoaded      += OnSaveLoaded;
        helper.Events.GameLoop.DayStarted      += OnDayStarted;
        helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
        helper.Events.Input.ButtonPressed      += OnButtonPressed;
        helper.Events.Input.ButtonsChanged     += OnButtonsChanged;

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

        // ── Economy sabotages ──
        _sabotage.Register(new NarcolepsySabotage());
        _sabotage.Register(new StickySabotage());
        _sabotage.Register(new InflationSabotage());
        _sabotage.Register(new DebtCollectorSabotage());

        // ── Rename sabotages ──
        _sabotage.Register(new RenameAnimalSabotage());
        _sabotage.Register(new RenamePetSabotage());

        // ── Ban sabotages — batch 1 ──
        _sabotage.Register(new BanInventorySabotage());
        _sabotage.Register(new BanHotbarSabotage());
        _sabotage.Register(new BanTalkSabotage());
        _sabotage.Register(new BanRunningSabotage());
        _sabotage.Register(new BanShoppingSabotage());

        // ── New sabotages — batch 2 ──
        _sabotage.Register(new InfestationSabotage());
        _sabotage.Register(new BlindfoldSabotage());
        _sabotage.Register(new ConfusedSabotage());
        _sabotage.Register(new MashedSabotage());
        _sabotage.Register(new FreezeTimeSabotage());
        _sabotage.Register(new FloorIsLavaSabotage());
        //_sabotage.Register(new SlipNSlideSabotage()); // removed — too unstable
        _sabotage.Register(new WarpWhistleSabotage());
        _sabotage.Register(new WarpWhistlePlusSabotage());
        _sabotage.Register(new WarpWhistleMaxSabotage());
        _sabotage.Register(new WarpWhistleMaxPlusSabotage());

        // ── New sabotages — batch 1 ──
        _sabotage.Register(new TaxManSabotage());
        _sabotage.Register(new SugarRushBlessing());
        _sabotage.Register(new GiftingTreeBlessing());
        _sabotage.Register(new BankruptcySabotage());
        _sabotage.Register(new PoltergeistSabotage());

        // ── Pokemon sabotages — batch 1 ──
        _sabotage.Register(new TrickRoomSabotage());
        _sabotage.Register(new MetronomeSabotage());
        _sabotage.Register(new SoakSabotage());
        _sabotage.Register(new TeleportSabotage());
        _sabotage.Register(new TrashDaySabotage());

        // ── Pokemon blessings — batch 1 ──
        _sabotage.Register(new LuckyChantBlessing());
        _sabotage.Register(new PayDayBlessing());

        // ── Blessings ──
        _sabotage.Register(new RestoreEnergyBlessing());
        _sabotage.Register(new RestoreHealthBlessing());
        _sabotage.Register(new GiveGoldBlessing());
        _sabotage.Register(new GiveMoreGoldBlessing());
        _sabotage.Register(new GiveMostGoldBlessing());
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

        // YouTube via Streamer.bot
        if (_config.YouTubeEnabled)
        {
            _youtube = new SDVChatVsStreamer.YouTube.YouTubeManager(
                _config, _points, _sabotage, _ledger, Monitor, _chatFeed, _overlay);
            YouTubeManager = _youtube;
            _youtube.Connect();
        }

        // TikTok via Tikfinity
        // Always create TikTokManager so /tiktok-test endpoint works even without EnableTikTok
        _tiktok = new TikTok.TikTokManager(_config, _points, _ledger, Monitor, _chatFeed, _sabotage);
        _overlay.SetTikTokManager(_tiktok);

        if (_config.EnableTikTok)
            _tiktok.Connect();
    }

    private void OnDayStarted(object? sender, StardewModdingAPI.Events.DayStartedEventArgs e)
    {
        SDVChatVsStreamer.Sabotage.Sabotages.WarpWhistleState.OnDayStarted();
        SDVChatVsStreamer.Sabotage.Sabotages.InflationSabotage.OnDayStarted();
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        _twitch.SetGameActive(true);
        _youtube?.SetGameActive(true);
        Monitor.Log("[ChatVsStreamer] Save loaded — sabotages enabled.", LogLevel.Info);
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        _twitch.SetGameActive(false);
        _youtube?.SetGameActive(false);
        Monitor.Log("[ChatVsStreamer] Returned to title — sabotages disabled.", LogLevel.Info);
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady) return;

        _overlay.ProcessPendingNotifications();
        SDVChatVsStreamer.Sabotage.Sabotages.ToolSabotageHelper.ProcessPendingActions();

        // Auto-trigger check every 60 seconds
        if (e.IsMultipleOf(3600))
            _sabotage.TickAutoTrigger(msg => _twitch.SendMessage(msg));
        SDVChatVsStreamer.Sabotage.Sabotages.BlindfoldSabotage.Tick();
        SDVChatVsStreamer.Sabotage.Sabotages.ConfusedSabotage.Tick();
        SDVChatVsStreamer.Sabotage.Sabotages.MashedSabotage.Tick();
        SDVChatVsStreamer.Sabotage.Sabotages.FreezeTimeSabotage.Tick();
        SDVChatVsStreamer.Sabotage.Sabotages.FloorIsLavaSabotage.Tick();
        //SDVChatVsStreamer.Sabotage.Sabotages.SlipNSlideSabotage.Tick(); // removed
        SDVChatVsStreamer.Sabotage.Sabotages.WarpWhistleState.Tick();
        SDVChatVsStreamer.Sabotage.Sabotages.BanState.Tick();
        SDVChatVsStreamer.Sabotage.Sabotages.NarcolepsySabotage.Tick();
        SDVChatVsStreamer.Sabotage.Sabotages.StickySabotage.Tick();

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

    private void OnButtonsChanged(object? sender, StardewModdingAPI.Events.ButtonsChangedEventArgs e)
    {
        if (!ConfusedSabotage.IsActive || !Context.IsWorldReady) return;

        // Sticky — suppress item switching and dropping
        if (StickySabotage.IsActive)
        {
            foreach (var btn in e.Pressed)
            {
                bool isHotbarKey = btn >= SButton.D0 && btn <= SButton.D9;
                bool isDropKey   = btn == SButton.Q;
                if (isHotbarKey || isDropKey)
                    Helper.Input.Suppress(btn);
            }
        }

        var moveButtons = new[] {
            SButton.W, SButton.S, SButton.A, SButton.D,
            SButton.Up, SButton.Down, SButton.Left, SButton.Right
        };

        foreach (var btn in moveButtons)
        {
            if (Helper.Input.IsDown(btn))
            {
                Helper.Input.Suppress(btn);
                var opposite = btn switch
                {
                    SButton.W    or SButton.Up    => 2, // down
                    SButton.S    or SButton.Down  => 0, // up
                    SButton.A    or SButton.Left  => 1, // right
                    SButton.D    or SButton.Right => 3, // left
                    _                             => -1
                };
                if (opposite >= 0 && !Game1.player.movementDirections.Contains(opposite))
                    Game1.player.movementDirections.Add(opposite);
            }
        }
    }

    private void OnMenuChanged(object? sender, StardewModdingAPI.Events.MenuChangedEventArgs e)
    {
        if (!InflationSabotage.IsActive) return;
        if (e.NewMenu is not ShopMenu shop) return;

        // Apply inflation multiplier to all shop items
        foreach (var entry in shop.itemPriceAndStock)
        {
            var priceData = entry.Value;
            if (priceData.Price > 0)
                priceData.Price = (int)(priceData.Price * InflationSabotage.Multiplier);
        }

        int pct = (int)((InflationSabotage.Multiplier - 1f) * 100);
        Game1.addHUDMessage(new HUDMessage(
            $"📈 Inflation is active! Prices are {pct}% higher!",
            HUDMessage.error_type));
    }

    private void OnWarped(object? sender, StardewModdingAPI.Events.WarpedEventArgs e)
    {
        SDVChatVsStreamer.Sabotage.Sabotages.WarpWhistleState.OnWarped(e.NewLocation.Name);
    }

    private void OnRenderedWorld(object? sender, StardewModdingAPI.Events.RenderedWorldEventArgs e)
    {
        if (!Context.IsWorldReady) return;
        BlindfoldSabotage.Draw(e.SpriteBatch);
        FloorIsLavaSabotage.Draw(e.SpriteBatch);
        WarpWhistleState.Draw(e.SpriteBatch);
        BanState.Draw(e.SpriteBatch);

        var sb   = e.SpriteBatch;
        var font = Game1.smallFont;

        // Show freeze time indicator
        if (FreezeTimeSabotage.IsActive)
        {
            var text = $"Time Frozen: {FreezeTimeSabotage.SecsLeft}s";
            var pos  = new Vector2(16, 16);
            sb.DrawString(font, text, pos + new Vector2(2, 2), Color.Black);
            sb.DrawString(font, text, pos, Color.Cyan);
        }

        // Show confused indicator
        if (ConfusedSabotage.IsActive)
        {
            var text = $"Confused: {ConfusedSabotage.SecsLeft}s";
            var pos  = new Vector2(16, 48);
            sb.DrawString(font, text, pos + new Vector2(2, 2), Color.Black);
            sb.DrawString(font, text, pos, Color.Purple);
        }

        // Show mashed indicator
        if (MashedSabotage.IsActive)
        {
            var text = $"Mashed: {MashedSabotage.SecsLeft}s";
            var pos  = new Vector2(16, 80);
            sb.DrawString(font, text, pos + new Vector2(2, 2), Color.Black);
            sb.DrawString(font, text, pos, Color.HotPink);
        }

        // Show narcolepsy indicator
        if (NarcolepsySabotage.IsActive)
        {
            var text = $"Narcolepsy: {NarcolepsySabotage.SecsLeft}s";
            var pos  = new Vector2(16, 112);
            sb.DrawString(font, text, pos + new Vector2(2, 2), Color.Black);
            sb.DrawString(font, text, pos, Color.LightBlue);
        }

        // Show sticky indicator
        if (StickySabotage.IsActive)
        {
            var text = $"Sticky: {StickySabotage.SecsLeft}s";
            var pos  = new Vector2(16, 144);
            sb.DrawString(font, text, pos + new Vector2(2, 2), Color.Black);
            sb.DrawString(font, text, pos, Color.Goldenrod);
        }

        // Show inflation indicator
        if (InflationSabotage.IsActive)
        {
            int pct  = (int)((InflationSabotage.Multiplier - 1f) * 100);
            var text = $"Inflation: +{pct}% prices";
            var pos  = new Vector2(16, 176);
            sb.DrawString(font, text, pos + new Vector2(2, 2), Color.Black);
            sb.DrawString(font, text, pos, Color.Red);
        }
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        // Ban state input suppression
        if (Context.IsWorldReady && SDVChatVsStreamer.Sabotage.Sabotages.BanState.BanInventory)
        {
            if (e.Button == SButton.E || e.Button == SButton.Escape)
            {
                // Menu isn't open yet — this press is trying to OPEN it, so block it
                if (Game1.activeClickableMenu == null)
                {
                    Helper.Input.Suppress(e.Button);
                    return;
                }
            }
        }

        if (Context.IsWorldReady && SDVChatVsStreamer.Sabotage.Sabotages.BanState.BanHotbar)
        {
            bool isHotbarKey =
                (e.Button >= SButton.D0 && e.Button <= SButton.D9) ||
                e.Button == SButton.OemMinus || e.Button == SButton.OemPlus;
            if (isHotbarKey) { Helper.Input.Suppress(e.Button); return; }
        }

        if (Context.IsWorldReady && SDVChatVsStreamer.Sabotage.Sabotages.BanState.BanTalk)
        {
            if (e.Button == SButton.MouseRight || e.Button == SButton.X || e.Button == SButton.ControllerA)
            {
                var facing  = Game1.player.FacingDirection;
                var tile    = Game1.player.TilePoint;
                var checkTile = facing switch {
                    0 => new Point(tile.X, tile.Y - 1),
                    1 => new Point(tile.X + 1, tile.Y),
                    2 => new Point(tile.X, tile.Y + 1),
                    3 => new Point(tile.X - 1, tile.Y),
                    _ => tile
                };
                var npc = Game1.player.currentLocation.isCharacterAtTile(checkTile.ToVector2());
                if (npc != null) { Helper.Input.Suppress(e.Button); return; }
            }
        }

        if (Context.IsWorldReady && SDVChatVsStreamer.Sabotage.Sabotages.BanState.BanShopping)
        {
            // Suppress action key near shop counters
            if (e.Button == SButton.MouseLeft || e.Button == SButton.ControllerA)
                if (Game1.activeClickableMenu is ShopMenu)
                { Helper.Input.Suppress(e.Button); return; }
        }
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