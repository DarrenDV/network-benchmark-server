
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;






int packetCount = 0;
int lastPacketID = 0;
int packetsReceived = 0;
UdpClient client = new UdpClient(3000);


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
        
        HandleReceivedPacket(receivedString);
        
        
        if(packetsReceived == packetCount)
        {
            done = true;
        }
    }
}

void HandleReceivedPacket(string receivedPacket)
{
    TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
    long secondsSinceEpoch = (long)t.TotalMilliseconds; 
    
    JsonNode? receivedJson = JsonNode.Parse(receivedPacket);
    
    
    if(receivedJson?["type"]?.ToString() == "errorPacket")
    {
        
        Console.WriteLine(receivedJson?["errorType"]?.ToString());
        
        switch (receivedJson?["errorType"]?.ToString())
        {
            case "missingPacket":
                Console.WriteLine("A packet was lost!");

                //packetsReceived += int.Parse(receivedJson?["packetID"]?.ToString()) - lastPacketID; 
                Console.WriteLine(int.Parse(receivedJson?["packetID"]?.ToString() ?? string.Empty) - lastPacketID);
                break;
            case "delayedPacket":
                Console.WriteLine("A packet was delayed!");
                return;
        }
    }
    
    if(int.Parse(receivedJson?["packetID"]?.ToString() ?? string.Empty) != lastPacketID + 1)
    {
        Console.WriteLine("A packet was lost!");
       // Console.WriteLine(int.Parse(receivedJson?["packetID"]?.ToString() ?? string.Empty) - lastPacketID);
    }
    
    
    lastPacketID = int.Parse(receivedJson?["packetID"]?.ToString() ?? string.Empty);
    
    long serverToClient = secondsSinceEpoch - long.Parse(receivedJson?["serverDatetime"]?.ToString() ?? string.Empty);
        
    long averagePing = (long.Parse(receivedJson?["clientToServerPing"]?.ToString() ?? string.Empty) + serverToClient) / 2;
    
        
    Console.WriteLine("Received Packet!");
    Console.WriteLine($"PacketID: {receivedJson?["packetID"]}, average ping: {averagePing}, C2S: {receivedJson?["clientToServerPing"]}, S2C: {serverToClient}");
    
}