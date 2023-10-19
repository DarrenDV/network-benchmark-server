
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;






int packetCount = 0;
int packetsReceived = 0;
UdpClient client = new UdpClient(3000);

string testId = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"); //testID allows the server to differ between multiple tests

Console.WriteLine(testId);

Start();


return;







void Start()
{
    Console.WriteLine("How many packets?");
    packetCount = int.Parse(Console.ReadLine() ?? string.Empty);

    
    try
    {
    
        client.Connect("127.0.0.1", 41234);
    
        Thread networkListenThread = new Thread(NetworkListenWorker);
        networkListenThread.Start();
    
    
        Thread networkSendThread = new Thread(NetworkSendWorker);
        networkSendThread.Start();
    
    
        networkListenThread.Join();
        networkSendThread.Join();
    }
    catch (Exception e)
    {
        Console.WriteLine(e.ToString());
    }
}


void NetworkSendWorker()
{
    for(int i = 0; i < packetCount; i++)
    {
        SendPacket(i+1);
        Thread.Sleep(1000);
    }

}




void SendPacket(int packetNumber)
{
    TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
    long secondsSinceEpoch = (long)t.TotalMilliseconds;

    JsonNode jsonNode = new JsonObject();
    jsonNode["testID"] = testId;
    jsonNode["packetCount"] = packetCount;
    jsonNode["packetID"] = packetNumber;
    jsonNode["clientDatetime"] = secondsSinceEpoch;
    
    
    byte[] sendBytes = Encoding.ASCII.GetBytes(jsonNode.ToString());
    
    client.Send(sendBytes, sendBytes.Length);
}





void NetworkListenWorker()
{
    bool done = false;
    IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
    while (!done)
    {
        byte[] receiveBytes = client.Receive(ref remoteEndPoint);
        
        if(receiveBytes.Length == 0)
        {
            continue;
        }
        
        packetsReceived++;
        
        string receivedString = Encoding.ASCII.GetString(receiveBytes);
        
        TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
        long secondsSinceEpoch = (long)t.TotalMilliseconds; 
    
        JsonNode? receivedJson = JsonNode.Parse(receivedString);
        
        
        //Get array from receivedJson?["C2STimings"]

        List<int> c2sTimings = new List<int>();

        for (int i = 0; i < receivedJson?["C2STimings"]?.AsArray().Count; i++) 
        {
            c2sTimings.Add(int.Parse(receivedJson?["C2STimings"]?.AsArray()[i]?.ToString() ?? string.Empty));
        }
        
        double average = c2sTimings.Average();
        
        
        long S2C = secondsSinceEpoch - long.Parse(receivedJson?["serverDatetime"]?.ToString() ?? string.Empty);
        
        Console.WriteLine($"packetID: {receivedJson?["packetID"]}, " +
                          $"packet S2C: {S2C}, " +
                          $"C2S average: {average}." );
        
        if(packetsReceived == packetCount)
        {
            done = true;
        }
    }
}

