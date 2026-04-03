using StardewValley;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class WarpSabotage : ISabotage
{
    public string Name         => "Warp";
    public string BuyCommand   => "warp";
    public string Description  => "warps you to a random location";
    public int Cost            => 400;
    public int CooldownSeconds => 300;

    private readonly Random _rng = new();

    private static readonly (string Location, int X, int Y)[] _destinations =
    {
        ("Town",     50, 80),
        ("Mountain", 30, 20),
        ("Beach",    30,  5),
        ("Forest",   60, 40),
        ("BusStop",  12, 10),
        ("Mine",      8,  8),
    };

    public void Execute(string triggeredBy)
    {
        var dest = _destinations[_rng.Next(_destinations.Length)];
        Game1.warpFarmer(dest.Location, dest.X, dest.Y, false);

        Game1.addHUDMessage(new HUDMessage(
            $"🌀 {triggeredBy} warped you to {dest.Location}!",
            HUDMessage.error_type));
    }
}
