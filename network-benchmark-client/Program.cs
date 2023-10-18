// See https://aka.ms/new-console-template for more information


using System.Net;
using System.Net.Sockets;
using System.Text;

UdpClient client = new UdpClient(41234);
try
{
    client.Connect("127.0.0.1", 41234);
    byte[] sendBytes = Encoding.ASCII.GetBytes("Is anybody there?");
    client.Send(sendBytes, sendBytes.Length);

    IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 41234);

    byte[] receiveBytes = client.Receive(ref remoteEndPoint);
    string receivedString = Encoding.ASCII.GetString(receiveBytes);

    Console.WriteLine("Message received from the server \n " + receivedString);
}
catch (Exception e)
{
    Console.WriteLine(e.ToString());
}