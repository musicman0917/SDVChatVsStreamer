using Microsoft.Xna.Framework;
using StardewValley;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class BombSabotage : ISabotage
{
    public string Name         => "Bomb";
    public string BuyCommand   => "bomb";
    public string Description  => "explodes an area near the player";
    public int Cost            => 250;
    public int CooldownSeconds => 180;

    public void Execute(string triggeredBy)
    {
        var location = Game1.player.currentLocation;
        var pos      = new Vector2(
            Game1.player.TilePoint.X,
            Game1.player.TilePoint.Y
        ) * 64f;

        // Create a bomb explosion — radius 3, damage 50
        location.explode(
            new Vector2(Game1.player.TilePoint.X, Game1.player.TilePoint.Y),
            3,
            Game1.player);

        Game1.addHUDMessage(new HUDMessage(
            $"💣 {triggeredBy} dropped a bomb on you!",
            HUDMessage.error_type));
    }
}
