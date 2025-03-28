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
    private static readonly int BROADCAST_PORT = 990;

    private IPEndPoint _broadcastSendIp;
    private IPEndPoint _broadcastRcvIp;
    private UdpClient _udpClient;

    public Network()
    {
      _broadcastRcvIp = new IPEndPoint(0, 0);
      _broadcastSendIp = new IPEndPoint(IPAddress.Any, BROADCAST_PORT);

      _udpClient = new UdpClient(_broadcastSendIp);
      _udpClient.EnableBroadcast = true;
    }

    public void Broadcast(string user, string pubKey)
    {
      // Broadcast format:  <username>::<public key>
      byte[] broadcastBuffer = Encoding.ASCII.GetBytes(user + "::" + pubKey);

      Console.WriteLine($"Broadcasting on port {BROADCAST_PORT}");

      _udpClient.SendAsync(broadcastBuffer, broadcastBuffer.Length, "255.255.255.255", BROADCAST_PORT);
    }

    public void ListenBroadcast()
    {
      Console.WriteLine($"Listening for broadcasts on port {BROADCAST_PORT}");

      while (true)
      {
        // Wait until a new broadcast comes in
        var result = _udpClient.Receive(ref _broadcastRcvIp);

        // Parse the broadcast packet
        string msg = Encoding.ASCII.GetString(result);

        // TODO: add a length check? maxUserLen + separatorLen + ecdhPubKeyLen


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
        Console.WriteLine($"Received broadcast: {msg}\n\tUsername: {entries[0]}\n\tPub key: {entries[1]}");
      }
    }
  }
}
