# Roadmap

Ideas being tracked for future consideration — nothing here is scheduled or in progress unless noted otherwise.

## Under consideration

- **Multiplayer support** — ⚠️ Alpha, untested in a real co-op session. The host enables up to 3 player slots in GMCM ("Multiplayer Targeting" page) and enters each co-op player's own Twitch channel name; the mod's bot joins that channel too, and any command typed there (by that player or their own viewers) redirects to their connected farmhand if its character name matches the channel name exactly — no mod install required on their end. Channel-to-farmhand routing is implemented via an ambient `MultiplayerTargeting.CurrentChannel`, set by `TwitchManager.OnMessageReceived` for the duration of each synchronous command dispatch. This only covers sabotages that mutate host-authoritative save state (money, health/stamina, buffs, inventory, nearby monster/explosion spawns); effects that read local input or draw to a screen (Confused, jump scares, bans, warps) can't reach a farmhand without the mod and still always hit whoever's game is running it. Needs real co-op testing to confirm: (1) the bot can actually join an arbitrary public channel and receive messages without being modded there, and (2) Farmer net-field writes made from the host actually stick and sync correctly for a non-host farmhand.
- **Kick integration** — possibly add Kick as another chat/points source alongside Twitch, YouTube, and TikTok. No design or implementation work started.
