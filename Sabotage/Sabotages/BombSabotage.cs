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
        var target   = MultiplayerTargeting.Resolve(triggeredBy);
        var location = target.currentLocation;

        // Create a bomb explosion — radius 3, damage 50
        location.explode(
            new Vector2(target.TilePoint.X, target.TilePoint.Y),
            3,
            target);

        MultiplayerTargeting.Notify(target,
            $"💣 {triggeredBy} dropped a bomb on you!",
            HUDMessage.error_type);
    }
}
