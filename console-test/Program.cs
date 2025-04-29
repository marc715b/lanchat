using console_test.Logic;

namespace console_test
{
  static class Program
  {
    private static Network _network = new Network();
    private static List<Session> _sessions = new List<Session>();

    private static void listenTcp()
    {
      _network.StartListener();

      Task tcpListenerTask = Task.Run(() =>
      {
        while (true)
        {
          Session session = _network.ListenSessions();

          Task.Run(() =>
          {
            _sessions.Add(session);

            Console.Write("Sending test text: ");
            session.Send(Console.ReadLine());

            // Start a blocking listen
            session.Listen();
          });
        }

        _network.StopListener();
      });
    }

    private static Task listenUdp()
    {
      Task udpListenerTask = Task.Run(() =>
      {
        while (true)
        {
          Contact contact = _network.ListenBroadcast();
          Task.Run(() =>
          {
            // Open a new TCP connection to the contact
            Session session = new Session(contact);

            _sessions.Add(session);

            session.Send("Outgoing connection send test");

            // Start a blocking listen
            session.Listen();
          });
        }
      });

      return udpListenerTask;
    }

    public static void Main(string[] args)
    {
      // Listen for incoming TCP connections (sessions)
      listenTcp();
      
      // Listen for incoming UDP broadcasts (peer announcements)
      Task udpListenerTask = listenUdp();

      // Send out a test broadcast
      _network.Broadcast("TestUsername123", "28701b10ed531c2107f973954e3dde5f28dcc07c06f211c385e6be8a5defe3107");

      udpListenerTask.Wait();
    }
  }
}