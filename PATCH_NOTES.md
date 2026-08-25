# Patch Notes

## v0.5.0

**New**
- DonorDrive / Extra Life integration — polls a DonorDrive donation page and turns each new donation into chaos points (2pts/cent, double the bits rate) plus a random sabotage/blessing scaled by donation size. Configurable in GMCM.
- Airdrop sabotage (2000pts) — a jackpot loot chest dropped at a random open tile anywhere on the farm.
- Raids now roll real Chaos/Blessing/Meta events — previously raids only spawned a slime swarm; they now use the full tiered system, including temporary double points, halved shop costs, and bonus points for raiders who chat in.
- New OBS overlays: an alert popup (`/alert`) for every trigger, plus a full-screen vertical/mobile version (`/alert-mobile`) for a phone or vertical canvas.
- `The Chaos Almanac` — a full command/cost/points reference page, linked by `!shop` instead of dumping every command into chat.
- `/debug` now actually works as a URL (`http://localhost:7373/debug`), not just as a local file.

**Fixed**
- `!warp` (and Teleport/Warp Whistle) could softlock a player for the day by landing on an isolated, disconnected tile. Warps now prefer a location's own exit points and verify connectivity before committing; a total failure now refunds the cost instead of silently keeping it.
- `!shop` no longer floods chat with every command — it posts a link to the Chaos Almanac instead.
- Twitch IRC now explicitly joins the channel instead of relying on an unreliable auto-join, and the reconnect loop can no longer get permanently stuck after a token refresh.
- Care Package could place its chest on water or overwrite an existing placed object; both fixed.
- Auto-Petter now also pets animals sheltering indoors (barn/coop) instead of only ones out on the farm.
- Several smaller correctness fixes found in a full review of every sabotage/blessing command (duplicated bit/donation firing logic, Warp Whistle failing silently, Geode Crack losing loot on a full inventory).

**Changed**
- GMCM reorganized into fewer, clearer pages, and fixed a bug where three pages (Auto Trigger, YouTube, DonorDrive) were silently buried inside the wrong page instead of showing on the main list.
- All OBS-facing overlay pages (shop sidebar, shop board, chat feed) reskinned to a consistent Stardew wood/parchment theme.
- Installer rebuilt: TikTok and YouTube are now independent optional steps instead of an either/or choice, added a YouTube setup walkthrough, and the completion screen lists every overlay URL.

## v0.4.0

- YouTube Live chat integration via Streamer.bot (beta), sharing the same points pool as Twitch.
- New economy sabotages, warp whistle upgrade tiers, ban sabotage fixes.
- Twitch OAuth token now refreshes automatically in the background.
- Floor Is Lava sabotage reworked.

## v0.3.8

- Auto-clipping: a Twitch clip is created automatically when a sabotage fires, configurable per tier with cooldowns.
- Give More Gold / Give Most Gold blessings added.
- Floor Is Lava's Mr. Qi dialogue and dismiss-timer behavior fixed.

## v0.3.7

- Floor Is Lava sabotage added — bare ground damages you, only placed paths/floors are safe.

## v0.3.6

- Batch 2 of chaos effects: Infestation, Blindfold, Confused, Freeze Time.

## v0.3.5

- New sabotages: Tax Man, Sugar Rush, Gifting Tree.

## v0.3.4

- Pokémon-inspired sabotages batch 1 (Trick Room, Metronome, Lucky Chant, Pay Day, and more).
- Fixed a tool-sabotage crash.
- Added minimal overlay mode.
- TikTok integration marked deprecated/use-at-your-own-risk in the docs.

## v0.3.3

- Minimal overlay mode added; `!clear` chat command fixed.

## v0.3.2

- Fixed `!clear` clearing the chat overlay incorrectly.
- Added the PowerShell installer and expanded the README.
- Added a GitHub update key to the manifest so SMAPI can detect new versions.

## v0.3.1

- Weapon chaos sabotages and a raid slime-spawn overhaul.

## v0.3.0

- Initial public release.
