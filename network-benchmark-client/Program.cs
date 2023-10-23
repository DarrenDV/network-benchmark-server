using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using CsvHelper;
using GuerrillaNtp;


//Script set variables
const string serverIp = "127.0.0.1";
const int serverPort = 41234;









var incomingPacketDatas = new List<IncomingPacketData>();

int packetCount = 0;
int packetsReceived = 0;
int timeout = 10000; //ms
UdpClient client = new UdpClient(3000);


NtpClient ntpClient = NtpClient.Default;

NtpClock clock = ntpClient.Query();
DateTime clockTime = clock.UtcNow.UtcDateTime;
DateTime QueryTime = DateTime.UtcNow;


//string tets2 = clock.UtcNow.UtcDateTime.ToString("yyyyMMddHHmmssfff");
//string testId = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"); //testID allows the server to differ between multiple tests
string testId = clock.UtcNow.UtcDateTime.ToString("yyyyMMddHHmmssfff");
//DateTimeOffset now = DateTimeOffset.UtcNow;

//Console.WriteLine(testId);
//Console.WriteLine(clock.UtcNow.UtcDateTime.ToString("yyyyMMddHHmmssfff"));

//Thread.Sleep(1000);

//Console.WriteLine(clock.UtcNow.UtcDateTime.ToString("yyyyMMddHHmmssfff"));

//Console.ReadLine();


Start();
return;








void Start()
{

    // while (true)
    // {
    //     TimeSpan t = clock.UtcNow.UtcDateTime - new DateTime(1970, 1, 1);
    //     long secondsSinceEpoch = (long)t.TotalMilliseconds; //Calculate time as first thing so it is as accurate as possible
    //
    //     if (secondsSinceEpoch % 1000 == 0)
    //     {
    //         Console.WriteLine(secondsSinceEpoch);
    //         
    //     }
    //     
    // }
    
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

    // TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
    // long secondsSinceEpoch = (long)t.TotalMilliseconds;

    TimeSpan t = clock.UtcNow.UtcDateTime - new DateTime(1970, 1, 1);
    long secondsSinceEpoch = (long)t.TotalMilliseconds;
    
    jsonNode["clientDatetime"] = SecondsSinceEpoch();


    byte[] sendBytes = Encoding.ASCII.GetBytes(jsonNode.ToString());

    client.Send(sendBytes, sendBytes.Length);
}


void NetworkListenWorker(CancellationToken cancellationToken)
{
    List<int> S2CTimings = new List<int>();
    double C2SAverage = 0;
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
        // TimeSpan t = clock.UtcNow.UtcDateTime - new DateTime(1970, 1, 1);
        // long secondsSinceEpoch = (long)t.TotalMilliseconds; //Calculate time as first thing so it is as accurate as possible
        long _secondsSinceEpoch = SecondsSinceEpoch();

        JsonNode? receivedJson = JsonNode.Parse(receivedString);


        int packetID = int.Parse(receivedJson?["packetID"]?.ToString() ?? string.Empty);

        //Calculate average Client to Server (C2S) timing
        List<int> c2STimings = new List<int>();
        for (int i = 0; i < receivedJson?["C2STimings"]?.AsArray().Count; i++)
        {
            //Console.WriteLine(i);
            c2STimings.Add(int.Parse(receivedJson?["C2STimings"]?.AsArray()[i]?.ToString() ?? string.Empty));
        }
        C2SAverage = c2STimings.Average();

        //Calculate Server to Client (S2C) timing
        long S2C = _secondsSinceEpoch - long.Parse(receivedJson?["serverDatetime"]?.ToString() ?? string.Empty);
        S2CTimings.Add((int)S2C);

        //Get packet loss rates
        float C2SSuccessRate = float.Parse(receivedJson?["C2SSuccessRate"]?.ToString() ?? string.Empty);
        float S2CSuccessRate = (float)packetsReceived / (float)packetID * 100f;
        

        Console.WriteLine($"packetID: {receivedJson?["packetID"]}, " +
                          $"packet S2C: {S2C} ms, " +
                          $"S2C success rate: {S2CSuccessRate:0.000}%, " +
                          $"C2S average: {C2SAverage:0.000} ms, " +
                          $"C2S success rate: {C2SSuccessRate:0.000}%.");



        incomingPacketDatas.Add(
            new IncomingPacketData(
                receivedJson?["packetID"]?.ToString(),
                packetCount.ToString(),
                S2C.ToString(),
                S2CSuccessRate.ToString(CultureInfo.InvariantCulture),
                C2SAverage.ToString(CultureInfo.InvariantCulture),
                C2SSuccessRate.ToString(CultureInfo.InvariantCulture)
                )
            );
    }
    
    Console.WriteLine(S2CTimings.Average());
    Console.WriteLine((S2CTimings.Average() + C2SAverage) / 2);
    
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

long SecondsSinceEpoch()
{
    TimeSpan offset = DateTime.UtcNow - QueryTime;
    DateTime adjustedQueryTime = clockTime + offset;
    TimeSpan t = adjustedQueryTime - new DateTime(1970, 1, 1);
    long secondsSinceEpoch = (long)t.TotalMilliseconds;
    return secondsSinceEpoch;
}