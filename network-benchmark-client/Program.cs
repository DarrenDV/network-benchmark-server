using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;
using CsvHelper;
using GuerrillaNtp;


//Script set variables
const string serverIp = "213.93.255.165";
const int serverPort = 41234;





List<IncomingPacketData> incomingPacketDatas = new ();

int packetCount;
int packetsReceived = 0;
const int timeout = 10000; //ms





UdpClient client = new (3000);

NtpClient ntpClient = NtpClient.Default;

NtpClock clock = ntpClient.Query();
DateTime clockTime = clock.UtcNow.UtcDateTime;
DateTime queryTime = DateTime.UtcNow;

string testId = clock.UtcNow.UtcDateTime.ToString("yyyyMMddHHmmssfff");

Start();
return;




void Start()
{
    Console.WriteLine("How many packets?");
    packetCount = int.Parse(Console.ReadLine() ?? string.Empty);

    try
    {
        client.Connect(serverIp, serverPort);

        CancellationTokenSource cancellationTokenSource = new ();
        Thread networkListenThread = new (() => NetworkListenWorker(cancellationTokenSource.Token));
        networkListenThread.Start();

        Thread networkSendThread = new (NetworkSendWorker);
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
    for (int i = 0; i < packetCount; i++)
    {
        SendPacket(i + 1);
        Thread.Sleep(16);
    }

}

void SendPacket(int packetNumber)
{
    JsonNode jsonNode = new JsonObject();
    jsonNode["testID"] = testId;
    jsonNode["packetCount"] = packetCount;
    jsonNode["packetID"] = packetNumber;
    
    jsonNode["clientDatetime"] = SecondsSinceEpoch();


    byte[] sendBytes = Encoding.ASCII.GetBytes(jsonNode.ToString());

    client.Send(sendBytes, sendBytes.Length);
}


void NetworkListenWorker(CancellationToken cancellationToken)
{
    List<int> S2CTimings = new ();
    while (packetsReceived < packetCount)
    {

        byte[]? receiveBytes;

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

        if (receiveBytes is { Length: 0 })
        {
            Console.WriteLine("Received nothing");
            continue;
        }


        packetsReceived++;

        string receivedString = Encoding.ASCII.GetString(receiveBytes);
        
        
        

        //Process received data here
        long secondsSinceEpoch = SecondsSinceEpoch(); //Get packet receive time

        JsonNode? receivedJson = JsonNode.Parse(receivedString);


        int packetId = int.Parse(receivedJson?["packetID"]?.ToString() ?? string.Empty);

        //Calculate average Client to Server (C2S) timing
        List<int> c2STimings = new ();
        for (int i = 0; i < receivedJson?["C2STimings"]?.AsArray().Count; i++)
        {
            //Console.WriteLine(i);
            c2STimings.Add(int.Parse(receivedJson?["C2STimings"]?.AsArray()[i]?.ToString() ?? string.Empty));
        }
        double C2SAverage = c2STimings.Average();

        //Calculate Server to Client (S2C) timing
        long S2C = secondsSinceEpoch - long.Parse(receivedJson?["serverDatetime"]?.ToString() ?? string.Empty);
        S2CTimings.Add((int)S2C);

        //Get packet loss rates
        float C2SSuccessRate = float.Parse(receivedJson?["C2SSuccessRate"]?.ToString() ?? string.Empty);
        float S2CSuccessRate = packetsReceived / (float)packetId * 100f;
        
        double averagePing = (S2CTimings.Average() + C2SAverage) / 2f;

        Console.WriteLine($"packetID: {receivedJson?["packetID"]}, " +
                          $"packet S2C: {S2C} ms, " +
                          $"S2C success rate: {S2CSuccessRate:0.000}%, " +
                          $"C2S average: {C2SAverage:0.000} ms, " +
                          $"C2S success rate: {C2SSuccessRate:0.000}%, " +
                          $"Average ping: {averagePing:0.000} ms, " + 
                          $"Packet size: {receiveBytes.Length} bytes, " +
                          $"Capable speed: {receiveBytes.Length / (averagePing / 1000f) / 1024f / 1024f:0.000} MB/s");



        incomingPacketDatas.Add(
            new IncomingPacketData(
                receivedJson?["packetID"]?.ToString(),
                packetCount.ToString(),
                S2C.ToString(),
                S2CSuccessRate.ToString(CultureInfo.InvariantCulture),
                C2SAverage.ToString(CultureInfo.InvariantCulture),
                C2SSuccessRate.ToString(CultureInfo.InvariantCulture),
                averagePing.ToString(CultureInfo.InvariantCulture),
                receiveBytes.Length.ToString()
                )
            );
    }
    

    
    //Write to CSV
    using StreamWriter writer = new($"{testId}-client.csv");
    using (CsvWriter csv = new (writer, CultureInfo.InvariantCulture))
    {
        csv.WriteRecords(incomingPacketDatas);
    }

}

byte[]? ReceiveWithTimeout(CancellationToken cancellationToken)
{
    using CancellationTokenRegistration registration = cancellationToken.Register(() => client.Close());
    
    byte[]? receiveBytes;

    if (client.Client.Poll(timeout * 1000, SelectMode.SelectRead))
    {
        IPEndPoint remoteEndPoint = new (IPAddress.Any, 0);
        receiveBytes = client.Receive(ref remoteEndPoint);
    }
    else
    {
        cancellationToken.ThrowIfCancellationRequested(); // Check for cancellation before throwing
        throw new OperationCanceledException();
    }

    return receiveBytes;
}

//Calculate the amount of milliseconds since 1/1/1970 keeping the NTP time in mind
long SecondsSinceEpoch()
{
    TimeSpan offset = DateTime.UtcNow - queryTime;
    DateTime adjustedQueryTime = clockTime + offset;
    TimeSpan t = adjustedQueryTime - new DateTime(1970, 1, 1);
    long secondsSinceEpoch = (long)t.TotalMilliseconds;
    return secondsSinceEpoch;
}