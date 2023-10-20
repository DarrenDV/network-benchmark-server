public class IncomingPacketData
{
    public IncomingPacketData(string? packetNumber, string packetCount, string s2C, string s2CSuccessRate, string averageC2STime, string c2SSuccessRate)
    {
        PacketNumber = packetNumber;
        PacketCount = packetCount;
        S2C = s2C;
        S2CSuccessRate = s2CSuccessRate;
        AverageC2STime = averageC2STime;
        C2SSuccessRate = c2SSuccessRate;
    }

    public string? PacketNumber { get; set; }
    public string PacketCount { get; set; }
    public string S2C { get; set; }
    public string S2CSuccessRate { get; set; }
    public string AverageC2STime { get; set; }
    public string C2SSuccessRate { get; set; }
}