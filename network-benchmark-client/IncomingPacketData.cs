public class IncomingPacketData
{
    public IncomingPacketData(string? packetNumber, string packetCount, string s2C, string s2CSuccessRate, string averageC2STime, string c2SSuccessRate, string averagePing, string packetSize, string networkStrength)
    {
        PacketNumber = packetNumber;
        PacketCount = packetCount;
        S2C = s2C;
        S2CSuccessRate = s2CSuccessRate;
        AverageC2STime = averageC2STime;
        C2SSuccessRate = c2SSuccessRate;
        AveragePing = averagePing;
        PacketSize = packetSize;
        NetworkStrength = networkStrength;
    }

    public string? PacketNumber { get; set; }
    public string PacketCount { get; set; }
    public string S2C { get; set; }
    public string S2CSuccessRate { get; set; }
    public string AverageC2STime { get; set; }
    public string C2SSuccessRate { get; set; }
    public string AveragePing { get; set; }
    public string PacketSize { get; set; }
    public string NetworkStrength { get; set; }
}