


using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

UdpClient client = new UdpClient(3000);
try
{
    
    client.Connect("127.0.0.1", 41234);
    
    TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
    long secondsSinceEpoch = (long)t.TotalMilliseconds;

    
    byte[] sendBytes = Encoding.ASCII.GetBytes(secondsSinceEpoch.ToString());
    

    
    client.Send(sendBytes, sendBytes.Length);
    
    
    IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

    byte[] receiveBytes = client.Receive(ref remoteEndPoint);
    string receivedString = Encoding.ASCII.GetString(receiveBytes);

    t = DateTime.UtcNow - new DateTime(1970, 1, 1);
    secondsSinceEpoch = (long)t.TotalMilliseconds; 
    
    var poep = JsonNode.Parse(receivedString);
    long serverToClient = secondsSinceEpoch - long.Parse(poep["serverdatetime"].ToString());
    
    Console.WriteLine("Client to server ping: " + poep["clientToServerPing"]);
    Console.WriteLine("Server to Client ping: " + serverToClient );
    
    //Console.WriteLine("Message received from the server \n " + receivedString);
}
catch (Exception e)
{
    Console.WriteLine(e.ToString());
}