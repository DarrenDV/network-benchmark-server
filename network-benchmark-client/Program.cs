﻿using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;
using CsvHelper;
using GuerrillaNtp;


//Script set variables
const string serverIp = "213.93.255.165";
const int serverPort = 41234;









var incomingPacketDatas = new List<IncomingPacketData>();

int packetCount = 0;
int packetsReceived = 0;
int timeout = 10000; //ms
UdpClient client = new UdpClient(3000);


// NtpClient ntpClient = NtpClient.Default;
//
// NtpClock clock = ntpClient.Query();


//string tets2 = clock.UtcNow.UtcDateTime.ToString("yyyyMMddHHmmssfff");
string testId = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"); //testID allows the server to differ between multiple tests

//DateTimeOffset now = DateTimeOffset.UtcNow;

// Console.WriteLine(testId);
// Console.WriteLine(tets2);
//
// Console.ReadLine();


Start();
return;








void Start()
{
    Console.WriteLine("How many packets?");
    packetCount = int.Parse(Console.ReadLine() ?? string.Empty);
    
    try
    {
        client.Connect(serverIp, serverPort);
        
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        Thread networkListenThread = new Thread(() => NetworkListenWorker(cancellationTokenSource.Token));
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
        Thread.Sleep(16);
    }

}

void SendPacket(int packetNumber)
{
    JsonNode jsonNode = new JsonObject();
    jsonNode["testID"] = testId;
    jsonNode["packetCount"] = packetCount;
    jsonNode["packetID"] = packetNumber;
    
    TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
    long secondsSinceEpoch = (long)t.TotalMilliseconds;
    
    jsonNode["clientDatetime"] = secondsSinceEpoch;
    
    
    byte[] sendBytes = Encoding.ASCII.GetBytes(jsonNode.ToString());
    
    client.Send(sendBytes, sendBytes.Length);
}


void NetworkListenWorker(CancellationToken cancellationToken)
{
    while (packetsReceived < packetCount)
    {

        byte[]? receiveBytes = null;

        try
        {
            // Use the client.Receive method with a timeout
            receiveBytes = ReceiveWithTimeout(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // The CancellationToken was canceled, which means a timeout occurred
            Console.WriteLine("Timeout occurred.");
            break;
        }
        
        if (receiveBytes.Length == 0)
        {
            Console.WriteLine("Received nothing");
            continue;
        }

        
        packetsReceived++;

        string receivedString = Encoding.ASCII.GetString(receiveBytes);

        
        
         //Process received data here
         TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
         long secondsSinceEpoch = (long)t.TotalMilliseconds; //Calculate time as first thing so it is as accurate as possible
     
         
         JsonNode? receivedJson = JsonNode.Parse(receivedString);
         
         
         int packetID = int.Parse(receivedJson?["packetID"]?.ToString() ?? string.Empty);

         //Calculate average Client to Server (C2S) timing
         List<int> c2STimings = new List<int>();
         for (int i = 0; i < receivedJson?["C2STimings"]?.AsArray().Count; i++) 
         {
             c2STimings.Add(int.Parse(receivedJson?["C2STimings"]?.AsArray()[i]?.ToString() ?? string.Empty));
         }
         double average = c2STimings.Average();
         
         //Calculate Server to Client (S2C) timing
         long S2C = secondsSinceEpoch - long.Parse(receivedJson?["serverDatetime"]?.ToString() ?? string.Empty);

         //Get packet loss rates
         float C2SSuccessRate = float.Parse(receivedJson?["C2SSuccessRate"]?.ToString() ?? string.Empty);
         float S2CSuccessRate = (float)packetsReceived / (float)packetID * 100f;
         
         
         Console.WriteLine($"packetID: {receivedJson?["packetID"]}, " +
                           $"packet S2C: {S2C} ms, " +
                           $"S2C success rate: {S2CSuccessRate:0.000}%, " +
                           $"C2S average: {average:0.000} ms, " +
                           $"C2S success rate: {C2SSuccessRate:0.000}%.");

        
         
         incomingPacketDatas.Add(
             new IncomingPacketData(
                 receivedJson?["packetID"]?.ToString(),
                 packetCount.ToString(),
                 S2C.ToString(),
                 S2CSuccessRate.ToString(CultureInfo.InvariantCulture),
                 average.ToString(CultureInfo.InvariantCulture),
                 C2SSuccessRate.ToString(CultureInfo.InvariantCulture)
                 )
             );
    }
    
    //Write to CSV
    using var writer = new StreamWriter($"{testId}-client.csv");
    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
    {
        csv.WriteRecords(incomingPacketDatas);
    }
    
}

byte[]? ReceiveWithTimeout(CancellationToken cancellationToken)
{
    byte[]? receiveBytes = null;
    
    using (CancellationTokenRegistration registration = cancellationToken.Register(() => client.Close()))
    {
        int timeoutMilliseconds = timeout; // Adjust the timeout as needed
        
        if (client.Client.Poll(timeoutMilliseconds * 1000, SelectMode.SelectRead))
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            receiveBytes = client.Receive(ref remoteEndPoint);
        }
        else
        {
            cancellationToken.ThrowIfCancellationRequested(); // Check for cancellation before throwing
            throw new OperationCanceledException();
        }
    }
    
    return receiveBytes;
}