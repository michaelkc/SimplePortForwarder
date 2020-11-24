// Decompiled with JetBrains decompiler
// Type: Mac.PortForwarder.Program
// Assembly: PortForwarder, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: C3838E8D-701C-430E-815E-A6AFFF9F2D70
// Assembly location: C:\tools\PortForwarder.exe

using System;

namespace Mac.PortForwarder
{
  internal class Program
  {
    private static void Main(string[] args)
    {
      bool flag = false;
      int result1;
      int result2;
      if (args.Length == 3 && int.TryParse(args[0], out result1) && int.TryParse(args[2], out result2))
      {
        flag = true;
        string targetHost = args[1];
        new TcpPortForwarder(result1, result2, targetHost).Start();
        Console.WriteLine("Forwarding local port {0} to {1}:{2}", (object) result1, (object) targetHost, (object) result2);
        Console.WriteLine("Press enter to terminate.");
        Console.ReadLine();
        Console.WriteLine("Terminated.");
      }
      if (flag)
        return;
      Program.ShowUsage();
    }

    private static void ShowUsage()
    {
      Console.WriteLine("Usage:   PortForwarder.exe LocalPort RemoteHost RemotePort");
      Console.WriteLine("Example: PortForwarder.exe 3390 192.168.15.7 3389");
    }
  }
}
