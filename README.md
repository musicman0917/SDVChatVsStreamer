# 🌾 Chat vs Streamer

A Stardew Valley SMAPI mod that lets your Twitch and TikTok viewers earn chaos points just by watching — then spend them to sabotage (or occasionally bless) your farm in real time.

Built by [NeighborhoodofMusic](https://twitch.tv/neighborhoodofmusic).

---

## ✨ Features

### 💬 Twitch Integration
- Viewers earn points by chatting, watching, subscribing, gifting subs, cheering bits, and raiding
- Sub multipliers — higher tier subs earn passive points faster
- Full command system: `!balance`, `!shop`, `!buy <command>`, `!info <command>`
- Raid events scale to viewer count — chaos, blessings, or meta effects like double points
- Bit events trigger sabotages based on cheer amount
- Channel point redemptions award bonus points
- Broadcaster ID auto-fetched on startup — no manual config needed
- Ban, timeout, and message delete events clear the chat overlay automatically

### 🎵 TikTok Integration (via Tikfinity)
- Connects to [Tikfinity](https://tikfinity.zerinity.com/)'s local WebSocket at `ws://localhost:21213/`
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

### 👹 30+ Sabotages & Blessings

**Sabotages** (things chat can buy to ruin your day):
- Weather chaos — rain, storm, snow, green rain, wind
- Monster spawns — slimes, bugs, grubs, golems, frost bats, dust sprites, ghosts, serpents, shadow brutes, shadow shamans, iridium golems, squid kids
- Farm destruction — kill crops, spawn weeds, steal gold, drain energy
- Player debuffs — dizzy, drunk, speedup, forced sleep, warp
- Tool sabotage — upgrade or downgrade any tool mid-session
- Explosions — bombs and mega bombs

**Blessings** (chat can also be nice, occasionally):
- Restore energy and health
- Water all crops
- Fertilize crops
- Clear debris
- Give gold
- Speed boost
- Force sunny weather

All monsters spawn on passable tiles — no enemies inside walls.

### 💬 Chat Overlay
- **In-game HUD** — live Twitch + TikTok chat in a configurable corner with TTL-based expiry
- **OBS Browser Source** at `http://localhost:7373/chat` — real platform icons, Twitch emote images, emoji support, auto-reconnects

### 🖥️ OBS Overlays
- **Sidebar** (`/`) — chaos feed, leaderboard, shop ticker
- **Mobile shop** (`/mobile`) — full tiered shop with live cooldown countdowns
- **TikTok gift guide** (`/tiktok`) — scrolling reference card with real gift images

### 🛡️ Moderation
- Content filter with unicode normalization, leet-speak detection, and configurable blocked keywords
- Ban/timeout/message delete events remove messages from the overlay in real time

---

## 📦 Requirements

- [Stardew Valley](https://www.stardewvalley.net/) 1.6+
- [SMAPI](https://smapi.io/) 4.0+
- [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) (optional, for in-game config)
- [Tikfinity](https://tikfinity.zerinity.com/) (optional, for TikTok integration)

---

## 🚀 Installation

1. Install SMAPI and Stardew Valley 1.6
2. Download the latest release and unzip into your `Mods` folder
3. Launch the game through SMAPI
4. On first launch, you'll need to add your Twitch credentials to the `TwitchAuth` folder — see the auth setup guide below

---

## 🔑 Twitch Auth Setup

The mod uses OAuth to connect to Twitch chat and PubSub. On first launch, a `TwitchAuth` folder will be created inside the mod folder. Add a `config.json` there with:

```json
{
  "ClientId": "your_client_id",
  "AccessToken": "your_oauth_token",
  "BotUsername": "your_bot_or_channel_username"
}
```

You can generate an OAuth token at [twitchtokengenerator.com](https://twitchtokengenerator.com). The token needs `chat:read`, `chat:edit`, and `channel:read:redemptions` scopes.

---

## ⚙️ Configuration

All settings are available via Generic Mod Config Menu in-game, including:

- Points economy (base amounts, cooldowns, sub multipliers)
- Event bonuses (bits, raids, subs, follows)
- Bit thresholds for each sabotage tier
- Channel point redemption point amounts
- TikTok event bonuses
- Chat overlay position, TTL, and max messages
- Overlay theme and appearance
- Feature toggles (enable/disable TikTok, chat overlay, raid events, etc.)

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

## 🔧 Debug Tools

Open `debug.html` directly in Chrome while the game is running for a live dashboard showing:
- Twitch and TikTok connection status
- Real-time event log with platform filtering
- Sabotage and points tracking
- Test event injection panel

---

## 📝 Changelog

See [PATCH_NOTES.md](PATCH_NOTES.md) for full version history.

---

## 📜 License

MIT — feel free to fork, modify, and build on this. Credit appreciated but not required.

---

*Built live on stream at [twitch.tv/neighborhoodofmusic](https://twitch.tv/neighborhoodofmusic) 🎸*