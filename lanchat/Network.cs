static class Networker
{
//idek yet, we'll figure out what to communicate in and out of this
//func to broadcast public key
//listen to broadcasts on same port number

 private int Broadcast_port;
 private int Msg_port;

  public void SendBroadcast(string Usrname, string PublicKey) // so user has a default username to show for the public key it finds
    {
        using (UdpClient udpClient = new UdpClient())
        {
            udpClient.EnableBroadcast = true;
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, Broadcast_port);
            byte[] data = Encoding.UTF8.GetBytes(Usrname + "::" + PublicKey); // idk about this seperator, lmk if theres better idea

            udpClient.SendAsync(data, data.Length, endPoint); //async
            Console.WriteLine($"Broadcast sent"); // remove later, debugging uses
        }
    }

// here methods for key exchange and encrypted data transfer
  
}
