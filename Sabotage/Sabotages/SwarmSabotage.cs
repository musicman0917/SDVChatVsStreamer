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
        var location = Game1.player.currentLocation;
        int season   = Game1.player.currentLocation.GetSeasonIndex();

        for (int i = 0; i < 5; i++)
        {
            var offset = new Vector2(
                Game1.player.TilePoint.X + _rng.Next(-3, 4),
                Game1.player.TilePoint.Y + _rng.Next(-3, 4)
            ) * 64f;

            location.characters.Add(new GreenSlime(offset, season));
        }

        Game1.addHUDMessage(new HUDMessage(
            $"🟢🟢🟢 {triggeredBy} sent a slime SWARM after you!",
            HUDMessage.error_type));
    }
}
