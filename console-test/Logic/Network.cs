using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace console_test.Logic
{
  internal class Network
  {
    private static readonly int BROADCAST_PORT = 420;

    private IPEndPoint _broadcastIp;

    private UdpClient _udpSendClient;
    private UdpClient _udpRcvClient;

    public Network()
    {
      _udpSendClient = new UdpClient();
      _udpSendClient.EnableBroadcast = true;

      _broadcastIp = new IPEndPoint(IPAddress.Broadcast, BROADCAST_PORT);

      _udpRcvClient = new UdpClient(BROADCAST_PORT);
    }

    public void Broadcast(string user, string pubKey)
    {
      // Broadcast format:  <username>::<public key>
      byte[] broadcastBuffer = Encoding.ASCII.GetBytes(user + "::" + pubKey);

      Console.WriteLine($"Broadcasting on port {BROADCAST_PORT}");

      try {  
        int sent = _udpSendClient.Send(broadcastBuffer, broadcastBuffer.Length, _broadcastIp);
        Console.WriteLine($"Broadcast sent {sent}/{broadcastBuffer.Length} bytes");
      } 
      catch (Exception ex)
      {
        Console.WriteLine($"Broadcast failed: {ex.Message}");
      }
    }

    public void ListenBroadcast()
    {
      Console.WriteLine($"Listening for broadcasts on port {BROADCAST_PORT}");

      IPEndPoint remoteIp = new IPEndPoint(0, 0);

      while (true)
      {
        // Wait until a new broadcast comes in
        var result = _udpRcvClient.Receive(ref remoteIp);

        // Parse the broadcast packet
        string msg = Encoding.ASCII.GetString(result);

        // Handle malformed data - there must be exactly one ocurrence of the "::" substring,
        // as it is the separator between username and public key. The first check might seem
        // superfluous considering the last check, but a simple .Contains is way cheaper than
        // matching a regex pattern.
        if (!msg.Contains("::"))
          continue;

        if (Regex.Matches(msg, "::").Count != 1)
          continue;

        string[] entries = msg.Split("::");

        // Print some debug information
        Console.WriteLine($"Received broadcast: {msg}\n\tSender: {remoteIp.ToString()}\n\tUsername: {entries[0]}\n\tPub key: {entries[1]}");
      }
    }
  }
}
