using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Monsters;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class SpawnMonsterSabotage : ISabotage
{
    public string Name         => "SpawnSlime";
    public string BuyCommand   => "slime";
    public string Description  => "spawns a slime near you";
    public int Cost            => 125;
    public int CooldownSeconds => 90;

    public void Execute(string triggeredBy)
    {
        var location = Game1.player.currentLocation;
        var origin   = new Vector2(Game1.player.TilePoint.X, Game1.player.TilePoint.Y);
        var pos      = MonsterSpawnHelper.FindSpawnTile(location, origin, 2, 0);

        location.characters.Add(new GreenSlime(pos, Game1.player.currentLocation.GetSeasonIndex()));
        Game1.addHUDMessage(new HUDMessage(
            $"🟢 {triggeredBy} sent a slime after you!",
            HUDMessage.error_type));
    }
}