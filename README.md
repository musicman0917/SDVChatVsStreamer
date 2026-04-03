# Chat vs Streamer — Stardew Valley SMAPI Mod

> Twitch chat earns chaos points and spends them to sabotage the streamer in real time.

**By [NeighborhoodofMusic](https://twitch.tv/neighborhoodofmusic)**

---

## What is this?

Chat vs Streamer is a Twitch integration mod for Stardew Valley. Viewers earn **chaos points** just by watching and chatting, then spend them on sabotages — bad weather, stolen items, spawned monsters, broken tools, and more. The streamer suffers. Chat wins.

---

## Requirements

- [Stardew Valley](https://www.stardewvalley.net/) 1.6+
- [SMAPI](https://smapi.io/) 4.0+
- [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) *(optional, recommended)*
- A Twitch account and a registered Twitch application

---

## Installation

1. Install SMAPI and drop the `ChatVsStreamer` folder into your mods directory
2. Launch the game once to generate `config.json`
3. Set your `ChannelName` in GMCM or `config.json`
4. On first launch, press **F9** in-game to paste your Twitch OAuth token
5. Add your OBS browser sources (see [OBS Setup](#obs-setup))

---

## How Points Work

| Event | Points |
|---|---|
| Passive tick (every N minutes) | Configurable base amount |
| Chatting | +5pts (60s cooldown) |
| Follow | +25pts |
| New sub / resub | +100pts |
| Gift sub | +50pts per gift to the gifter |
| Raid (leader) | +2pts × viewer count |
| Raider joins chat | +15pts |
| Bits | +1pt per bit |

**Sub tier multipliers** apply to passive ticks:

| Tier | Multiplier |
|---|---|
| Non-sub | ×1.0 |
| Twitch Prime | ×1.75 |
| Tier 1 | ×1.5 |
| Tier 2 | ×2.0 |
| Tier 3 | ×3.0 |

---

## Chat Commands

| Command | Who | Description |
|---|---|---|
| `!balance` | Everyone | Check your chaos points |
| `!shop` | Everyone | List available sabotages |
| `!buy <command>` | Everyone | Purchase a sabotage |
| `!give <user> <amount>` | Mods + Broadcaster | Give points to a viewer |

---

## Sabotage Shop

### Weather (affects tomorrow's forecast)

| Command | Cost | Effect |
|---|---|---|
| `!buy rain` | 50pts | Rain tomorrow |
| `!buy wind` | 50pts | Windy tomorrow |
| `!buy storm` | 75pts | Lightning storm tomorrow |
| `!buy snow` | 75pts | Snow tomorrow |
| `!buy greenrain` | 200pts | Mysterious green rain tomorrow |

### Nuisance (50–100pts)

| Command | Cost | Effect |
|---|---|---|
| `!buy trash` | 75pts | Fills a random inventory slot with trash |
| `!buy drain` | 100pts | Drains 50 stamina |
| `!buy dizzy` | 100pts | Speed -1 debuff for 30 seconds |
| `!buy speedup` | 100pts | Fast-forwards time by 1 hour |

### Disruptive (125–250pts)

| Command | Cost | Effect |
|---|---|---|
| `!buy slime` | 125pts | Spawns a slime near the player |
| `!buy bat` | 125pts | Spawns a bat near the player |
| `!buy crows` | 150pts | Crows kill a random crop |
| `!buy broke` | 175pts | Steals 100–500g from the player |
| `!buy steal` | 200pts | Removes a random item from inventory |
| `!buy drunk` | 200pts | Speed -3 debuff for 60 seconds |
| `!buy weeds` | 250pts | Spawns 5 weeds on the farm |

### Painful (300–400pts)

| Command | Cost | Effect |
|---|---|---|
| `!buy bomb` | 300pts | Explodes an area near the player |
| `!buy swarm` | 300pts | Spawns 5 slimes near the player |
| `!buy sleep` | 350pts | Fast-forwards time to 2am |
| `!buy warp` | 400pts | Warps the player to a random location |

### Devastating (500+pts)

| Command | Cost | Effect |
|---|---|---|
| `!buy killfarm` | 600pts | Kills ALL crops on the farm |

### Tool Sabotages

Each operation has a random version and a targeted version:

| Operation | Random | Axe | Pickaxe | Hoe | Watering Can | Rod |
|---|---|---|---|---|---|---|
| Downgrade one level | `downgraderandom` (175pts) | `downgradeaxe` | `downgradepickaxe` | `downgradehoe` | `downgradewateringcan` | `downgraderod` |
| Upgrade one level | `upgraderandom` (125pts) | `upgradeaxe` | `upgradepickaxe` | `upgradehoe` | `upgradewateringcan` | `upgraderod` |
| Set to copper | `copperrandom` (250pts) | `copperaxe` | `copperpickaxe` | `copperhoe` | `copperwateringcan` | `copperrod` |
| Set to iridium | `iridiumrandom` (250pts) | `iridiumaxe` | `iridiumpickaxe` | `iridiumhoe` | `iridiumwateringcan` | `iridiumrod` |

> Targeted tool sabotages cost 25pts more than random versions.

---

## Raid Events

When a raid comes in, a weighted random event fires based on raid size:

| Raid Size | Chaos | Blessing | Meta |
|---|---|---|---|
| Small (1–9) | 70% | 15% | 15% |
| Medium (10–49) | 60% | 20% | 20% |
| Large (50–99) | 50% | 25% | 25% |
| Massive (100+) | 40% | 30% | 30% |

**Chaos** — a scaled sabotage fires (Massive raids fire two events)

**Blessing** — one of: full energy restore, gold bonus (scales with raid size), all crops watered

**Meta** — one of: double points for 10 min, sabotage costs halved for 5 min, raiders get bonus points

---

## Bit Events

Bit cheers trigger sabotages from tiered pools on top of awarding points:

| Threshold | Pool |
|---|---|
| 100+ bits | Small (drain, rain, trash, dizzy) |
| 500+ bits | Medium (slime, crows, steal, bat, speedup) |
| 1000+ bits | Large (warp, sleep, swarm, bomb, killfarm) |

---

## OBS Setup

The mod runs a local HTTP/WebSocket server on port **7373** (configurable in GMCM).

### Sidebar Overlay
```
URL:    http://localhost:7373/
Width:  280px (configurable)
Height: Match your scene height
```

### Vertical Shop Overlay (1080×1920)
```
URL:    http://localhost:7373/mobile
Width:  1080px
Height: 1920px
```

### URL Parameters
Override settings without touching GMCM:
```
http://localhost:7373/?mode=Ticker&theme=Dark&width=320
```

| Parameter | Values |
|---|---|
| `mode` | `Sidebar`, `Ticker` |
| `theme` | `Stardew`, `Dark`, `Light`, `Custom` |
| `width` | Any integer (pixels) |
| `fontSize` | Any integer (px) |
| `tickerPosition` | `Top`, `Bottom` |
| `tickerSpeed` | `Slow`, `Medium`, `Fast` |

### Themes
- **Stardew** — pixel art brown/gold (default)
- **Dark** — dark background, purple accent
- **Light** — white background, green accent
- **Custom** — set your own hex colors in GMCM

---

## Configuration

All settings are available in **Generic Mod Config Menu** under *Chat vs Streamer*. Pages:

- **General** — channel name, bot username, token setup
- **Points & Economy** — passive tick rate, base points, chat bonus
- **Sub Multipliers** — per-tier point multipliers
- **Event Bonuses** — follow, sub, raid, bit bonus amounts
- **Bit Thresholds** — configure the 3 bit event tiers
- **Feature Toggles** — enable/disable chat commands, channel points, bit events, raids
- **Overlay** — mode, theme, panels, width, font size, ticker settings, custom colors
- **Ignored Users** — open the in-game ignore list manager

### Key Bindings

| Key | Action |
|---|---|
| **F8** | Open Ignored Users manager (in-game) |
| **F9** | Paste Twitch OAuth token from clipboard |

---

## Building from Source

```bash
git clone https://github.com/musicman0917/SDVChatVsStreamer
cd SDVChatVsStreamer
dotnet build ChatVsStreamer.csproj -c Release
```

The mod auto-deploys to your Stardrop mods folder on build. Edit `GameModsPath` in `ChatVsStreamer.csproj` if your path differs.

**Dependencies (NuGet):**
- `Pathoschild.Stardew.ModBuildConfig` 4.4.0
- `TwitchLib.Client` 4.0.0
- `Microsoft.Data.Sqlite.Core` 8.0.0
- `SQLitePCLRaw.bundle_e_sqlite3` 2.1.8

---

## Changelog

### v0.2.0
- 25+ sabotages including weather, tools, monsters, farm chaos
- Full tool upgrade/downgrade system (24 commands)
- Raid event system with chaos/blessing/meta tiers scaled by raid size
- OBS overlay with sidebar, ticker, and vertical (1080×1920) modes
- 4 overlay themes (Stardew, Dark, Light, Custom)
- In-game settings panel, URL param overrides, GMCM overlay page
- Mod/broadcaster `!give` command
- Ignore list manager (F8)
- Balance pass on sabotage costs

### v0.1.0
- Initial release
- Twitch IRC + PubSub connection
- Point economy with sub multipliers
- Basic sabotage shop (!buy rain, !buy warp)
- GMCM config

---

## License

MIT — do whatever you want, just credit [NeighborhoodofMusic](https://twitch.tv/neighborhoodofmusic).
