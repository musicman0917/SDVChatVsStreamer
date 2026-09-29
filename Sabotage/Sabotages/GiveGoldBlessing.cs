using StardewValley;

namespace SDVChatVsStreamer.Sabotage.Sabotages;

public class GiveGoldBlessing : ISabotage
{
    public string Name         => "Give Gold";
    public string BuyCommand   => "givegold";
    public string Description  => "gives 500g";
    public int Cost            => 150;
    public int CooldownSeconds => 120;
    public SabotageTier Tier   => SabotageTier.Blessing;

    public void Execute(string triggeredBy)
    {
        var target = MultiplayerTargeting.Resolve(triggeredBy);
        target.Money += 500;
        MultiplayerTargeting.Notify(target,
            $"💰 {triggeredBy} gave you 500g!",
            HUDMessage.newQuest_type);
    }
}

public class GiveMoreGoldBlessing : ISabotage
{
    public string Name         => "Give More Gold";
    public string BuyCommand   => "givemoregold";
    public string Description  => "gives 5000g";
    public int Cost            => 200;
    public int CooldownSeconds => 180;
    public SabotageTier Tier   => SabotageTier.Blessing;

    public void Execute(string triggeredBy)
    {
        var target = MultiplayerTargeting.Resolve(triggeredBy);
        target.Money += 5000;
        MultiplayerTargeting.Notify(target,
            $"💰 {triggeredBy} gave you 5000g!",
            HUDMessage.newQuest_type);
    }
}

public class GiveMostGoldBlessing : ISabotage
{
    public string Name         => "Give Most Gold";
    public string BuyCommand   => "givemostgold";
    public string Description  => "gives 50000g";
    public int Cost            => 1000;
    public int CooldownSeconds => 600;
    public SabotageTier Tier   => SabotageTier.Blessing;

    public void Execute(string triggeredBy)
    {
        var target = MultiplayerTargeting.Resolve(triggeredBy);
        target.Money += 50000;
        MultiplayerTargeting.Notify(target,
            $"💰 {triggeredBy} gave you 50000g! What a legend!",
            HUDMessage.newQuest_type);
    }
}