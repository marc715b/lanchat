using console_test.Logic;

namespace console_test
{
  static class Program
  {
    private static Network _network = new Network();
    
    public static void Main(string[] args)
    {
      // Start listening for UDP broadcasts
      Task listenerTask = Task.Run(async () =>
      {
        while (true)
        {
          Contact contact = await _network.ListenBroadcast();
          Console.WriteLine($"Got new contact:\n\tName: {contact.GetName()}\n\tPub key: {contact.GetPubKey()}\n\tIP: {contact.GetIp()}");
        }
      });

      // Send out a test broadcast
      _network.Broadcast("TestUsername123", "28701b10ed531c2107f973954e3dde5f28dcc07c06f211c385e6be8a5defe3107");

      // Send out another test broadcast
      // _network.Broadcast("1DebugAccount", "90894f84e80fe4561530d68da3bae3d42bb750a3e336a62c2b6a6e0a58bfc11ae");

      listenerTask.Wait();
    }
  }
}