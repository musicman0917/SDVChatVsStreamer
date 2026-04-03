using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Monsters;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class BatSabotage : ISabotage
{
    public string Name         => "Bat";
    public string BuyCommand   => "bat";
    public string Description  => "spawns a bat near you";
    public int Cost            => 125;
    public int CooldownSeconds => 90;

    public void Execute(string triggeredBy)
    {
        var location = Game1.player.currentLocation;
        var origin   = new Vector2(Game1.player.TilePoint.X, Game1.player.TilePoint.Y);
        var pos      = MonsterSpawnHelper.FindSpawnTile(location, origin, 2, 0);

        location.characters.Add(new Bat(pos));

        Game1.addHUDMessage(new HUDMessage(
            $"🦇 {triggeredBy} sent a bat after you!",
            HUDMessage.error_type));
    }
}