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

        for (int i = 0; i < 5; i++)
        {
            var offset = new Vector2(
                target.TilePoint.X + _rng.Next(-3, 4),
                target.TilePoint.Y + _rng.Next(-3, 4)
            ) * 64f;

            location.characters.Add(new GreenSlime(offset, season));
        }

        MultiplayerTargeting.Notify(target,
            $"🟢🟢🟢 {triggeredBy} sent a slime SWARM after you!",
            HUDMessage.error_type);
    }
}
