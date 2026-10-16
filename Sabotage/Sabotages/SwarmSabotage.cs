using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Monsters;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class SwarmSabotage : ISabotage
{
    public string Name         => "Swarm";
    public string BuyCommand   => "swarm";
    public string Description  => "spawns 5 slimes near you";
    public int Cost            => 300;
    public int CooldownSeconds => 180;

    private readonly Random _rng = new();

    public void Execute(string triggeredBy)
    {
        var target   = MultiplayerTargeting.Resolve(triggeredBy);
        var location = target.currentLocation;
        int season   = location.GetSeasonIndex();
        var origin   = new Vector2(target.TilePoint.X, target.TilePoint.Y);

        // Raw random offsets (as this used to do) have no passability check, so slimes
        // could land inside walls, water, or other solid terrain -- use the same
        // tile-search helper every other monster-spawn sabotage relies on.
        var tiles = MonsterSpawnHelper.FindSpawnTiles(location, origin, 5);
        foreach (var pos in tiles)
            location.characters.Add(new GreenSlime(pos, season));

        MultiplayerTargeting.Notify(target,
            $"🟢🟢🟢 {triggeredBy} sent a slime SWARM after you!",
            HUDMessage.error_type);
    }
}
