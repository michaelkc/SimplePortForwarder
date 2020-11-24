// Decompiled with JetBrains decompiler
// Type: Mac.PortForwarder.BeforeTargetConnectArgs
// Assembly: PortForwarder, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: C3838E8D-701C-430E-815E-A6AFFF9F2D70
// Assembly location: C:\tools\PortForwarder.exe

using System;
using System.Net.Sockets;

namespace Mac.PortForwarder
{
  public class BeforeTargetConnectArgs : EventArgs
  {
    public Socket client;

    public BeforeTargetConnectArgs(Socket client) => this.client = client;
  }
}
