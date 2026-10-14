using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

// ─── Shared ban state ─────────────────────────────────────────────────────────

public static class BanState
{
    public static bool BanInventory  { get; private set; }
    public static bool BanHotbar     { get; private set; }
    public static bool BanTalk       { get; private set; }
    public static bool BanRunning    { get; private set; }
    public static bool BanShopping   { get; private set; }

    private static DateTime _inventoryExpires;
    private static DateTime _hotbarExpires;
    private static DateTime _talkExpires;
    private static DateTime _runningExpires;
    private static DateTime _shoppingExpires;
    private static int _lockedToolIndex;

    public static void Activate(string ban, int seconds)
    {
        var expires = DateTime.UtcNow.AddSeconds(seconds);
        switch (ban)
        {
            case "inventory": BanInventory = true; _inventoryExpires = expires; break;
            case "hotbar":
                BanHotbar        = true;
                _hotbarExpires   = expires;
                _lockedToolIndex = Game1.player.CurrentToolIndex;
                break;
            case "talk":      BanTalk      = true; _talkExpires      = expires; break;
            case "running":   BanRunning   = true; _runningExpires   = expires; break;
            case "shopping":  BanShopping  = true; _shoppingExpires  = expires; break;
        }
    }

    public static int SecsLeft(string ban) => ban switch
    {
        "inventory" => BanInventory ? Math.Max(0, (int)(_inventoryExpires - DateTime.UtcNow).TotalSeconds) : 0,
        "hotbar"    => BanHotbar    ? Math.Max(0, (int)(_hotbarExpires    - DateTime.UtcNow).TotalSeconds) : 0,
        "talk"      => BanTalk      ? Math.Max(0, (int)(_talkExpires      - DateTime.UtcNow).TotalSeconds) : 0,
        "running"   => BanRunning   ? Math.Max(0, (int)(_runningExpires   - DateTime.UtcNow).TotalSeconds) : 0,
        "shopping"  => BanShopping  ? Math.Max(0, (int)(_shoppingExpires  - DateTime.UtcNow).TotalSeconds) : 0,
        _           => 0
    };

    public static void Tick()
    {
        var now = DateTime.UtcNow;
        if (BanInventory && now >= _inventoryExpires) { BanInventory = false; Notify("Inventory ban lifted!"); }
        if (BanHotbar    && now >= _hotbarExpires)    { BanHotbar    = false; Notify("Hotbar ban lifted!"); }
        if (BanTalk      && now >= _talkExpires)      { BanTalk      = false; Notify("Talk ban lifted!"); }
        if (BanRunning   && now >= _runningExpires)   { BanRunning   = false; Notify("Running ban lifted!"); }
        if (BanShopping  && now >= _shoppingExpires)  { BanShopping  = false; Notify("Shopping ban lifted!"); }

        // Force close shop menus if shopping is banned
        if (BanShopping && Game1.activeClickableMenu is ShopMenu)
            Game1.activeClickableMenu = null;

        // Force close any dialogue if talking is banned — the OnButtonPressed suppression
        // in ModEntry only stops the interact key from OPENING a fresh conversation; it
        // can't catch every way an existing DialogueBox advances to its next line (mouse
        // click straight on the menu, gift-giving dialogue, ambient/scheduled greetings,
        // etc.), so this is the actual backstop that guarantees no conversation survives.
        if (BanTalk && Game1.activeClickableMenu is DialogueBox)
            Game1.activeClickableMenu = null;

        // Force walk if running is banned
        if (BanRunning)
            Game1.player.running = false;

        // Force the equipped slot to stay put if hotbar switching is banned. The number-key/
        // OemMinus/OemPlus suppression in ModEntry only blocks keyboard switching -- it can't
        // catch mouse-wheel scrolling or controller bumpers, so this closes those gaps too by
        // fixing the actual result regardless of which input method tried to change it.
        if (BanHotbar && Game1.player.CurrentToolIndex != _lockedToolIndex)
            Game1.player.CurrentToolIndex = _lockedToolIndex;
    }

    private static void Notify(string msg) =>
        Game1.addHUDMessage(new HUDMessage($"✅ {msg}", HUDMessage.newQuest_type));

    public static void Draw(SpriteBatch sb, ref float y)
    {
        var font = Game1.smallFont;
        float localY = y;

        void DrawIndicator(string label, Color color)
        {
            var pos = new Vector2(16, localY);
            sb.DrawString(font, label, pos + new Vector2(2, 2), Color.Black);
            sb.DrawString(font, label, pos, color);
            localY += 32f;
        }

        if (BanInventory) DrawIndicator($"🚫 Inventory Banned: {SecsLeft("inventory")}s", Color.OrangeRed);
        if (BanHotbar)    DrawIndicator($"🚫 Hotbar Banned: {SecsLeft("hotbar")}s",        Color.OrangeRed);
        if (BanTalk)      DrawIndicator($"🚫 Talking Banned: {SecsLeft("talk")}s",         Color.OrangeRed);
        if (BanRunning)   DrawIndicator($"🚫 Running Banned: {SecsLeft("running")}s",      Color.OrangeRed);
        if (BanShopping)  DrawIndicator($"🚫 Shopping Banned: {SecsLeft("shopping")}s",    Color.OrangeRed);

        y = localY;
    }
}

// ─── BAN INVENTORY ────────────────────────────────────────────────────────────

public class BanInventorySabotage : ISabotage, ILocallyRelayable
{
    public string Name         => "Ban Inventory";
    public string BuyCommand   => "baninventory";
    public string Description  => "locks your inventory for 60 seconds";
    public int Cost            => 200;
    public int CooldownSeconds => 180;

    public void Execute(string triggeredBy)
    {
        var target = MultiplayerTargeting.Resolve(triggeredBy);
        if (target != Game1.player && PeerRelay.TryRelay(target, BuyCommand, triggeredBy))
        {
            MultiplayerTargeting.Notify(target,
                $"🚫 {triggeredBy} banned your inventory for 60 seconds!",
                HUDMessage.error_type);
            return;
        }
        ExecuteLocal(triggeredBy);
    }

    public void ExecuteLocal(string triggeredBy)
    {
        BanState.Activate("inventory", 60);

        // Force close the inventory menu — but not if they're on the Options tab
        if (Game1.activeClickableMenu is GameMenu menu && menu.currentTab != GameMenu.optionsTab)
        {
            Game1.activeClickableMenu = null;
        }

        Game1.addHUDMessage(new HUDMessage(
            $"🚫 {triggeredBy} banned your inventory for 60 seconds!",
            HUDMessage.error_type));
    }
}

// ─── BAN HOTBAR ───────────────────────────────────────────────────────────────

public class BanHotbarSabotage : ISabotage, ILocallyRelayable
{
    public string Name         => "Ban Hotbar";
    public string BuyCommand   => "banhotbar";
    public string Description  => "prevents hotbar scrolling and switching for 60 seconds";
    public int Cost            => 150;
    public int CooldownSeconds => 120;

    public void Execute(string triggeredBy)
    {
        var target = MultiplayerTargeting.Resolve(triggeredBy);
        if (target != Game1.player && PeerRelay.TryRelay(target, BuyCommand, triggeredBy))
        {
            MultiplayerTargeting.Notify(target,
                $"🚫 {triggeredBy} banned your hotbar for 60 seconds!",
                HUDMessage.error_type);
            return;
        }
        ExecuteLocal(triggeredBy);
    }

    public void ExecuteLocal(string triggeredBy)
    {
        BanState.Activate("hotbar", 60);
        Game1.addHUDMessage(new HUDMessage(
            $"🚫 {triggeredBy} banned your hotbar for 60 seconds!",
            HUDMessage.error_type));
    }
}

// ─── BAN TALK ─────────────────────────────────────────────────────────────────

public class BanTalkSabotage : ISabotage, ILocallyRelayable
{
    public string Name         => "Ban Talk";
    public string BuyCommand   => "bantalk";
    public string Description  => "prevents talking to NPCs for 60 seconds";
    public int Cost            => 175;
    public int CooldownSeconds => 120;

    public void Execute(string triggeredBy)
    {
        var target = MultiplayerTargeting.Resolve(triggeredBy);
        if (target != Game1.player && PeerRelay.TryRelay(target, BuyCommand, triggeredBy))
        {
            MultiplayerTargeting.Notify(target,
                $"🚫 {triggeredBy} banned talking to NPCs for 60 seconds!",
                HUDMessage.error_type);
            return;
        }
        ExecuteLocal(triggeredBy);
    }

    public void ExecuteLocal(string triggeredBy)
    {
        BanState.Activate("talk", 60);
        Game1.addHUDMessage(new HUDMessage(
            $"🚫 {triggeredBy} banned talking to NPCs for 60 seconds!",
            HUDMessage.error_type));
    }
}

// ─── BAN RUNNING ──────────────────────────────────────────────────────────────

public class BanRunningSabotage : ISabotage, ILocallyRelayable
{
    public string Name         => "Ban Running";
    public string BuyCommand   => "banrunning";
    public string Description  => "forces walking speed for 60 seconds";
    public int Cost            => 150;
    public int CooldownSeconds => 120;

    public void Execute(string triggeredBy)
    {
        var target = MultiplayerTargeting.Resolve(triggeredBy);
        if (target != Game1.player && PeerRelay.TryRelay(target, BuyCommand, triggeredBy))
        {
            MultiplayerTargeting.Notify(target,
                $"🚫 {triggeredBy} banned running for 60 seconds! Walk, farmer, walk.",
                HUDMessage.error_type);
            return;
        }
        ExecuteLocal(triggeredBy);
    }

    public void ExecuteLocal(string triggeredBy)
    {
        BanState.Activate("running", 60);
        Game1.addHUDMessage(new HUDMessage(
            $"🚫 {triggeredBy} banned running for 60 seconds! Walk, farmer, walk.",
            HUDMessage.error_type));
    }
}

// ─── BAN SHOPPING ─────────────────────────────────────────────────────────────

public class BanShoppingSabotage : ISabotage, ILocallyRelayable
{
    public string Name         => "Ban Shopping";
    public string BuyCommand   => "banshopping";
    public string Description  => "closes and prevents opening any shop for 60 seconds";
    public int Cost            => 200;
    public int CooldownSeconds => 180;

    public void Execute(string triggeredBy)
    {
        var target = MultiplayerTargeting.Resolve(triggeredBy);
        if (target != Game1.player && PeerRelay.TryRelay(target, BuyCommand, triggeredBy))
        {
            MultiplayerTargeting.Notify(target,
                $"🚫 {triggeredBy} banned shopping for 60 seconds! Pierre will have to wait.",
                HUDMessage.error_type);
            return;
        }
        ExecuteLocal(triggeredBy);
    }

    public void ExecuteLocal(string triggeredBy)
    {
        BanState.Activate("shopping", 60);
        if (Game1.activeClickableMenu is ShopMenu) Game1.activeClickableMenu = null;
        Game1.addHUDMessage(new HUDMessage(
            $"🚫 {triggeredBy} banned shopping for 60 seconds! Pierre will have to wait.",
            HUDMessage.error_type));
    }
}