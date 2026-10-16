# 🌾 Chat vs Streamer

A Stardew Valley SMAPI mod that lets your Twitch and TikTok viewers earn chaos points just by watching — then spend them to sabotage (or occasionally bless) your farm in real time.

Built by [NeighborhoodofMusic](https://twitch.tv/neighborhoodofmusic).

---

## 🚀 Installation

1. Go to [Releases](https://github.com/musicman0917/SDVChatVsStreamer/releases) and download the latest zip
2. Unzip it anywhere
3. Right-click `Install.ps1` and select **Run with PowerShell**
4. If Windows blocks it, open PowerShell as admin and run:
```powershell
Set-ExecutionPolicy -Scope CurrentUser RemoteSigned
```
Then try again

The installer will:
- Detect Stardew Valley automatically
- Download and install SMAPI if it's missing
- Copy the mod files to your Mods folder
- Walk you through Twitch credential setup
- Check for Tikfinity for TikTok integration
- Show you the OBS browser source URLs

---

## ✨ Features

### 💬 Twitch Integration
- Viewers earn points by chatting, watching, subscribing, gifting subs, cheering bits, and raiding
- Sub multipliers — higher tier subs earn passive points faster
- Full command system: `!balance`, `!shop`, `!buy <command>`, `!info <command>`
- Raid events spawn slimes equal to the number of raiders — 20+ raiders also triggers a devastating sabotage
- Bit events trigger sabotages based on cheer amount
- Channel point redemptions award bonus points
- Broadcaster ID auto-fetched on startup
- Ban, timeout, and message delete events clear the chat overlay automatically

### 🎵 TikTok Integration (via Tikfinity)
- Connects to [Tikfinity](https://tikfinity.zerinity.com/)'s local WebSocket
- Chat, follow, like, share, and subscribe events all award points
- **Named gift overrides** with special in-game effects:

| Gift | Effect |
|---|---|
| Heart Me | Farmer does a heart emote 💖 |
| Rose | Farmer does a happy emote 🌹 |
| Ice Cream | Adds an Ice Cream to inventory 🍦 |
| Fireworks | Mega Bomb explodes at your feet 💣 |
| Mystery Box | Adds a Mystery Box + triggers a devastating sabotage 📦 |
| TikTok Universe | Mr. Qi appears and forces the day to end ⚡ |

- **Diamond tier system** for all other gifts (Nuisance → Disruptive → Painful → Devastating → Blessing)
- **No `!buy`/`!balance` chat commands on TikTok, by design.** TikTok doesn't allow bots to post in Live chat at all, and the intent here is to fold TikTok viewers into the Twitch experience rather than build a parallel one. Instead: TikTok viewers now earn the same passive "just by watching" points Twitch viewers do, and the current top TikTok point-earner shows up in the OBS overlay with a configurable call-to-action pointing at your Twitch channel (GMCM → TikTok page → Twitch Shoutout).

### 📺 YouTube Integration (via Streamer.bot) — Beta

⚠️ **Experimental.** Connects to a local [Streamer.bot](https://streamer.bot/) WebSocket, which relays your YouTube Live chat events into the mod.

- Chat messages award the normal chat bonus, same as Twitch/TikTok
- Memberships award the sub bonus and set the member's multiplier tier
- Super Chats convert to points using Twitch's bits-to-dollar ratio (100 "bits" per $1), then the same Points-Per-Bit rate as Twitch cheers
- `!balance`, `!shop`, `!info`, and `!buy` all work — points are checked/deducted and sabotages fire correctly

**Known limitation:** none of those commands reply in YouTube chat yet — only to the SMAPI log. Unlike Twitch IRC, Streamer.bot can't just be told "send this string"; it has to run a pre-built **Action** that you configure once, and the mod triggers it by name. Setup for that, whenever you're ready to enable it:

1. In Streamer.bot, create a new **Action** (any name — e.g. `SendYouTubeMessage`).
2. Add a sub-action: **YouTube → Send Message to Channel**.
3. Set **Broadcast** to `Latest` (or `Variable` if you ever run multiple concurrent broadcasts).
4. Set the **Message** field to `%message%` — Streamer.bot auto-populates this variable from whatever the mod passes in over the WebSocket.
5. Save it, then send me the exact Action name (or its GUID, shown in the Actions list) — that's the one piece the mod's code needs to trigger it via a `DoAction` WebSocket request.

Once that's wired up, `!balance`/`!buy`/etc. will post real replies into your YouTube Live chat the same way they already do on Twitch.

### 👹 Sabotages & Blessings

**Sabotages:**
- Weather chaos — rain, storm, snow, green rain, wind
- Monster spawns — slimes, bugs, grubs, golems, frost bats, dust sprites, ghosts, serpents, shadow brutes, shadow shamans, iridium golems, squid kids
- Farm destruction — kill crops, spawn weeds, steal gold, drain energy
- Player debuffs — dizzy, drunk, forced sleep, warp
- Player status effects — speedup (movement speed, for better or worse)
- Tool sabotage — upgrade or downgrade any tool mid-session
- Explosions — bombs and mega bombs
- **Jump Scare** (`!buy jumpscare`) — a startling full-screen flash and sound at a random moment in the next ~40 seconds. No warning when it's bought, none when it hits.
- **Nuclear Chaos** (`!buy nuclear`, 50,000pts) — the ultimate sabotage. The screen darkens, a sting plays, and the Wizard (or, very rarely, the mod's own dev) builds dread across a real multi-page dialogue box before the warning lands; once you click through it, demolishes every non-housing building (animals inside included), kills every crop, fells every tree, and clears every rock, fence, and piece of debris on the farm. Screen flash, explosion sounds, and aftershocks follow.

**Weapons (give the farmer a random weapon):**
- `!buy weaponnormal` — early game weapons
- `!buy weaponrandom` — truly random, every weapon including mods
- `!buy weaponbetter` — mid-game weapons
- `!buy weaponepic` — late game galaxy/obsidian tier
- `!buy weaponlegendary` — Infinity weapons and Meowmere

**Blessings:**
- Restore energy and health, water crops, fertilize, clear debris, give gold, speed boost, force sunny weather

### 🐔 Animal Challenge

A togglable "100 Chicken Challenge"-style goal — enable it in GMCM, pick an animal type (or "Any"), and set a target count. A live counter shows on the overlay and ticker. Chat can help or hurt progress:
- `!buy addanimal` (blessing) — buys the farm a free matching animal, if a coop/barn has room
- `!buy spookanimal` (sabotage) — permanently scares off a random matching animal

### ⚡ Chaos Gods Auto-Trigger

If chat goes quiet, the mod can fire a random sabotage on its own from a configurable command pool (GMCM → Sabotage Behavior). There's also a hotkey (default `F7`) to force-fire one immediately, ignoring the quiet-period cooldown — handy for clip farming on demand.

### 👥 Multiplayer Targeting (Alpha)

⚠️ **Alpha feature — untested in a real multiplayer session.** Playing co-op with other streamers? In GMCM's "Multiplayer Targeting" page, mark a player slot **Live** and type in their Twitch channel name — the mod joins their channel too, their chat starts earning chaos points the same way yours already does, and any command typed there (by them or their own viewers) lands on **their** farmhand instead of you, as long as their in-game character name matches their channel name **exactly**. Up to 7 co-op players (host + 7 = the game's 8-player multiplayer cap). No mod install needed on their end — just their channel name. Replies (`!balance`, `!buy` confirmations, etc.) post back into whichever channel triggered them, not always your own. Each player slot also has its own **Allow Bot Replies** toggle — turn it off if that player would rather the bot stayed silent in their chat; it still reads their channel either way, since that's what makes targeting and points work.

Effects that change save-file state (money, health/stamina, buffs, inventory, nearby monster/explosion spawns) redirect with no extra setup. Effects that read your keyboard or draw straight to a screen — Confused, jump scares, bans, warps, and similar — are different: those can only ever run on whoever's game is actually executing the mod's code, so they land on the host **unless** that co-op player also installs this mod themselves, in which case the effect is relayed over SMAPI's own multiplayer messaging to run locally on their game instead. See the in-game GMCM tooltip on the "Multiplayer Targeting" page for the full explanation.

### 💬 Chat Overlay
- **In-game HUD** — live Twitch + TikTok chat in a configurable corner with TTL expiry
- **OBS Browser Source** at `http://localhost:7373/chat` — Twitch emote images, emoji support, auto-reconnects

### 🖥️ OBS Overlays

| Source | URL |
|---|---|
| Chat overlay | `http://localhost:7373/chat` |
| Shop sidebar | `http://localhost:7373/` |
| Mobile shop | `http://localhost:7373/mobile` |
| TikTok gift guide | `http://localhost:7373/tiktok` |
| TikTok live feed (vertical) | `http://localhost:7373/tiktoklive` |

---

## 🎮 Twitch Commands

| Command | Description |
|---|---|
| `!balance` | Check your chaos points |
| `!shop` | Browse all sabotages by tier |
| `!buy <command>` | Spend points on a sabotage or blessing |
| `!info <command>` | Get details on a specific sabotage |
| `!give <user> <amount>` | Transfer points (mods only) |
| `!pointdrop` | Start a timed giveaway — announces a claim keyword (default `!1`), then awards points (default 500) to everyone who types it within the window (default 30s). Mods/broadcaster only. |

---

## ⚙️ Configuration

All settings are available via Generic Mod Config Menu (GMCM) in-game:
- Points economy (base amounts, cooldowns, sub multipliers)
- Event bonuses (bits, raids, subs, follows, TikTok events)
- Channel point redemption amounts
- Chat overlay position, TTL, and max messages
- Overlay theme and appearance
- Feature toggles (TikTok, chat overlay, raid events, etc.)

---

## 📦 Requirements

- [Stardew Valley](https://www.stardewvalley.net/) 1.6+
- [SMAPI](https://smapi.io/) 4.0+ *(the installer handles this)*
- [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) (optional, for in-game config)
- [Tikfinity](https://tikfinity.zerinity.com/) (optional, for TikTok integration)

---

## 🔧 Debug Tools

Open `debug.html` directly in Chrome while the game is running for a live dashboard:
- Twitch and TikTok connection status
- Real-time event log with platform filtering
- Sabotage and points tracking
- Test event injection — including raid simulation with custom viewer counts

---

## 📝 Changelog

See [PATCH_NOTES.md](PATCH_NOTES.md) for full version history.

---

## 📜 License

MIT — feel free to fork, modify, and build on this. Credit appreciated but not required.

---

*Built live on stream at [twitch.tv/neighborhoodofmusic](https://twitch.tv/neighborhoodofmusic) 🎸*