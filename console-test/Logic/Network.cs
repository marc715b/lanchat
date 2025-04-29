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
  internal class Contact
  {
    private static readonly int MAX_NAME_LEN = 32;
    private static readonly int ECDH_PUBKEY_LEN = 65; // NOTE: ECDH pub key represented as hex = 65 letters
                                                      // TODO: move into Crypto.cs file later

    private string _name;
    private string _ip;
    private string _pubKey;

    public Contact(string name, string ip, string pubKey)
    {
      // Set all fields with input verification
      SetName(name);
      SetIp(ip);
      SetPubKey(pubKey);
    }

    public void SetName(string name)
    {
      if (name.Length > MAX_NAME_LEN)
        throw new ArgumentException("Contact display name is too long");

      _name = name;
    }

    public string GetName()
    {
      return _name;
    }

    public void SetIp(string ip)
    {
      if (!IPAddress.TryParse(ip, out _))
        throw new ArgumentException("IP address is invalid");

      _ip = ip;
    }

    public string GetIp()
    {
      return _ip;
    }

    public void SetPubKey(string pubKey)
    {
      if (pubKey.Length != ECDH_PUBKEY_LEN)
        throw new ArgumentException("Invalid public key length");

      _pubKey = pubKey;
    }

    public string GetPubKey()
    {
      return _pubKey;
    }
  }

  internal class Session
  {
    private TcpClient _tcpClient;
    private NetworkStream _networkStream;

    // Outgoing session (we connect to other party)
    public Session(Contact contact)
    {
      Console.WriteLine("Creating session from Contact: " + contact.GetIp());

      _tcpClient = new TcpClient(contact.GetIp(), Network.SESSION_PORT);
      _networkStream = _tcpClient.GetStream();
    }

    // Incoming session (other party connects to us)
    public Session(TcpClient tcpClient)
    {
      Console.WriteLine("Creating session from TcpClient: " + tcpClient.ToString());

      _tcpClient = tcpClient;
      _networkStream = _tcpClient.GetStream();
    }

    // Test for now. TODO: pass msg as bytes
    // async?
    public void Send(string msg)
    {
      var buffer = Encoding.UTF8.GetBytes(msg);
      _networkStream.Write(buffer, 0, buffer.Length);
    }

    public void Listen()
    {
      Console.WriteLine("Listening to TCP socket from " + _tcpClient.ToString());

      MemoryStream messageStream = new MemoryStream();
      byte[] buffer = new byte[65535];
      int bytesRead;

      try
      {
        while ((bytesRead = _networkStream.Read(buffer, 0, buffer.Length)) > 0)
        {
          messageStream.Write(buffer, 0, bytesRead);

          // Print debug info
          byte[] data = messageStream.ToArray();
          string msg = Encoding.UTF8.GetString(data);

          Console.WriteLine($"Received message from {_tcpClient.ToString()}:\n\t{msg}");

          messageStream.SetLength(0);
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error while listening to socket: {ex.Message}");
      }
    }
  }

  internal class Network
  {
    public static readonly int BROADCAST_PORT = 420;
    public static readonly int SESSION_PORT = 421;

    private IPEndPoint _broadcastIp;

    private UdpClient _udpSendClient;
    private UdpClient _udpRcvClient;

    private TcpListener _tcpListener;

    public Network()
    {
      // Initialize TCP stuff
      _tcpListener = new TcpListener(IPAddress.Any, SESSION_PORT);

      // Initialize UDP stuff
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

    public Contact ListenBroadcast()
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

        string ip = remoteIp.Address.ToString();
        string user = entries[0];
        string pubKey = entries[1];

        // TEMP: hardcoded
        if (user == "TestUsername123")
          continue;

        // Print some debug information
        Console.WriteLine($"Received broadcast: {msg}\n\tSender: {remoteIp.ToString()}\n\tUsername: {user}\n\tPub key: {pubKey}");

        // Return a new Contact object for this user
        return new Contact(user, ip, pubKey);
      }
    }

    // Really bad ABI, but eh
    public void StartListener()
    {
      _tcpListener.Start();
    }

    public void StopListener()
    {
      _tcpListener.Stop();
    }

    public Session ListenSessions()
    {
      Console.WriteLine($"Listening for TCP connections on {SESSION_PORT}");

      return new Session(_tcpListener.AcceptTcpClient());
    }
  }
}
