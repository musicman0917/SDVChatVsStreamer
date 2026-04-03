namespace SDVChatVsStreamer.Economy;

public class ViewerRecord
{
    public string Username { get; set; } = "";
    public int Points { get; set; } = 0;
    public SubTier SubTier { get; set; } = SubTier.None;
    public DateTime LastChat { get; set; } = DateTime.MinValue;
    public int TotalEarned { get; set; } = 0;
    public int TotalSpent { get; set; } = 0;
}

public enum SubTier
{
    None  = 0,
    T1    = 1,
    T2    = 2,
    T3    = 3,
    Prime = 4
}
