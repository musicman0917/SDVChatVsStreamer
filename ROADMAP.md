# Roadmap

Ideas being tracked for future consideration — nothing here is scheduled or in progress unless noted otherwise.

## Under consideration

- **Multiplayer support** — ⚠️ Alpha, untested in a real co-op session. The host can list co-op players' Twitch usernames in GMCM ("Multiplayer Targeting" page); a sabotage/blessing a listed chatter triggers redirects to their own connected farmhand if its character name matches their Twitch name exactly — no mod install required on their end. This only covers sabotages that mutate host-authoritative save state (money, health/stamina, buffs, inventory, nearby monster/explosion spawns); effects that read local input or draw to a screen (Confused, jump scares, bans, warps) can't reach a farmhand without the mod and still always hit the host. Needs real co-op testing to confirm Farmer net-field writes actually stick and sync correctly for a non-host farmhand.
- **Kick integration** — possibly add Kick as another chat/points source alongside Twitch, YouTube, and TikTok. No design or implementation work started.
