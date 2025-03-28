using console_test.Logic;

namespace console_test
{
  static class Program
  {
    private static Network _network = new Network();
    
    public static void Main(string[] args)
    {
      // Start listening for UDP broadcasts
      Task listenerTask = Task.Run(() =>
      {
        _network.ListenBroadcast();
      });

      // Send out a test broadcast
      _network.Broadcast("TestUsername123", "NnQ5ZXZUalVjeWJ0djA1N2plaHpuWUtDU2thSVh5TlA=");

      // Send out another test broadcast
      _network.Broadcast("1DebugAccount", "Zmo3MnlmQ0F2VTQ0a3RMSFplNWtQckF2Y2NCYWE0eUk=");

      listenerTask.Wait();
    }
  }
}