using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace console_test.Logic
{
  internal class Chat
  {
    private Contact _contact;
    private Session _session;

    private Crypto.AES _aes;

    public Chat(Contact contact)
    {
      _contact = contact;
      _session = new Session(_contact);
    }

    public Chat(Session session)
    {
      _session = session;
    }

    // Blocking worker function for threads
    public void Run()
    {
      byte[] buffer;
      
      while (true)
      {
        // Wait for buffer to be populated
        while ((buffer = _session.Listen()) == null);

        // Fetch the packet buffer
        string packet = Encoding.UTF8.GetString(buffer);

        // Decrypt the packet
        string decryptedPacket = _aes.Decrypt(packet);

        // Parse packet
        string[] entries = decryptedPacket.Split('|');

        int packetOp = int.Parse(entries[0]);
        switch (packetOp)
        {
          // handshake packet
          case 1: break;

          // incoming message
          case 2:
          {
            string message = entries[1];
            Console.WriteLine($"New message: {message}");
          } break;

          // unknown packet
          default: break; 
        }
      }
    }

    public void Handshake(string name, string pubKey, string prvKey)
    {
      // Send initial handshake packet
      _session.Send(Encoding.UTF8.GetBytes(String.Join("|", 1, name, pubKey)));

      // Receive and parse handshake packet
      string handshakePacket = Encoding.UTF8.GetString(_session.Listen());
      string[] entries = handshakePacket.Split('|');

      if (entries[0].CompareTo("1") == 0)
      {
        // Recipient name and public key
        string rcpName = entries[1];
        string rcpPubKey = entries[2];

        byte[] sharedSecret = Crypto.ECDH.DeriveKey(
          Encoding.UTF8.GetBytes(pubKey),
          Encoding.UTF8.GetBytes(prvKey),
          Encoding.UTF8.GetBytes(rcpPubKey));

        _contact = new Contact(rcpName, _session.GetIp(), rcpPubKey/*, Encoding.UTF8.GetString(sharedSecret)*/);
      }
    }

    public void SendMessage(string message)
    {
      string encryptedPacket = _aes.Encrypt(String.Join("|", 2, message));
      _session.Send(Encoding.UTF8.GetBytes(encryptedPacket));
    }
  }
}
