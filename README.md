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

### 👹 Sabotages & Blessings

**Sabotages:**
- Weather chaos — rain, storm, snow, green rain, wind
- Monster spawns — slimes, bugs, grubs, golems, frost bats, dust sprites, ghosts, serpents, shadow brutes, shadow shamans, iridium golems, squid kids
- Farm destruction — kill crops, spawn weeds, steal gold, drain energy
- Player debuffs — dizzy, drunk, speedup, forced sleep, warp
- Tool sabotage — upgrade or downgrade any tool mid-session
- Explosions — bombs and mega bombs

**Weapons (give the farmer a random weapon):**
- `!buy weaponnormal` — early game weapons
- `!buy weaponrandom` — truly random, every weapon including mods
- `!buy weaponbetter` — mid-game weapons
- `!buy weaponepic` — late game galaxy/obsidian tier
- `!buy weaponlegendary` — Infinity weapons and Meowmere

**Blessings:**
- Restore energy and health, water crops, fertilize, clear debris, give gold, speed boost, force sunny weather

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

---

## 🎮 Twitch Commands

| Command | Description |
|---|---|
| `!balance` | Check your chaos points |
| `!shop` | Browse all sabotages by tier |
| `!buy <command>` | Spend points on a sabotage or blessing |
| `!info <command>` | Get details on a specific sabotage |
| `!give <user> <amount>` | Transfer points (mods only) |

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