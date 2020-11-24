// Decompiled with JetBrains decompiler
// Type: Mac.PortForwarder.TcpPortForwarder
// Assembly: PortForwarder, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: C3838E8D-701C-430E-815E-A6AFFF9F2D70
// Assembly location: C:\tools\PortForwarder.exe

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Mac.PortForwarder
{
  public class TcpPortForwarder
  {
    private int localPort;
    private int targetPort;
    private string targetHost;
    private TcpListener listener;

    public TcpPortForwarder(int localPort, int targetPort, string targetHost)
    {
      this.localPort = localPort;
      this.targetPort = targetPort;
      this.targetHost = targetHost;
    }

    public int LocalPort
    {
      get => this.localPort;
      set
      {
        if (this.localPort == value)
          return;
        this.localPort = value;
      }
    }

    public int TargetPort
    {
      get => this.targetPort;
      set
      {
        if (this.targetPort == value)
          return;
        this.targetPort = value;
      }
    }

    public string TargetHost
    {
      get => this.targetHost;
      set
      {
        if (this.targetHost == value)
          return;
        this.targetHost = value;
      }
    }

    public void Start()
    {
      this.listener = new TcpListener(IPAddress.Any, this.localPort);
      this.listener.Start();
      this.listener.BeginAcceptTcpClient(new AsyncCallback(this.AcceptTcpClient), (object) this.listener);
    }

    private void AcceptTcpClient(IAsyncResult asyncResult)
    {
      TcpListener asyncState = asyncResult.AsyncState as TcpListener;
      TcpPortForwarder.ClientPair clientPair = new TcpPortForwarder.ClientPair();
      try
      {
        clientPair.connectRetryCount = 0;
        clientPair.disconnected = false;
        clientPair.source = asyncState.EndAcceptTcpClient(asyncResult);
        this.OnBeforeTargetConnect(clientPair.source.Client);
        clientPair.target = new TcpClient();
        clientPair.target.BeginConnect(this.targetHost, this.targetPort, new AsyncCallback(this.TargetConnect), (object) clientPair);
        asyncState.BeginAcceptTcpClient(new AsyncCallback(this.AcceptTcpClient), (object) this.listener);
      }
      catch (ObjectDisposedException ex)
      {
      }
      catch (Exception ex)
      {
        Trace.TraceError("Failed when trying to accept new clients with '{0}'", (object) ex.ToString());
      }
    }

    private void TargetConnect(IAsyncResult asyncResult)
    {
      TcpPortForwarder.ClientPair clientPair = asyncResult.AsyncState != null ? (TcpPortForwarder.ClientPair) asyncResult.AsyncState : throw new ArgumentNullException("asyncResult.AsyncState");
      try
      {
        clientPair.target.EndConnect(asyncResult);
        clientPair.targetStream = clientPair.target.GetStream();
        clientPair.sourceStream = clientPair.source.GetStream();
        clientPair.sourceStream.BeginRead(clientPair.sourceBuffer, 0, clientPair.sourceBuffer.Length, new AsyncCallback(this.SourceRead), (object) clientPair);
        clientPair.targetStream.BeginRead(clientPair.targetBuffer, 0, clientPair.targetBuffer.Length, new AsyncCallback(this.TargetRead), (object) clientPair);
      }
      catch (SocketException ex)
      {
        if (clientPair.connectRetryCount < 2)
        {
          ++clientPair.connectRetryCount;
          clientPair.target.BeginConnect(this.targetHost, this.targetPort, new AsyncCallback(this.TargetConnect), (object) clientPair);
          Trace.TraceWarning("Retrying connect");
        }
        else
        {
          Trace.TraceError("Connection failed: {0}", (object) ex.ToString());
          clientPair.source.Close();
        }
      }
      catch (ObjectDisposedException ex)
      {
      }
      catch (Exception ex)
      {
        Trace.TraceError("Failed connecting to target with '{0}'", (object) ex.ToString());
        clientPair.source.Close();
      }
    }

    private void SourceRead(IAsyncResult asyncResult)
    {
      TcpPortForwarder.ClientPair asyncState = (TcpPortForwarder.ClientPair) asyncResult.AsyncState;
      if (!asyncState.disconnected)
      {
        if (asyncState.source.Connected)
        {
          try
          {
            int count = asyncState.sourceStream.EndRead(asyncResult);
            if (count > 0)
            {
              Encoding.UTF8.GetString(asyncState.sourceBuffer, 0, count);
              if (asyncState.target.Connected)
              {
                asyncState.targetStream.BeginWrite(asyncState.sourceBuffer, 0, count, new AsyncCallback(this.TargetWrite), (object) asyncState);
                return;
              }
            }
          }
          catch (Exception ex)
          {
            Trace.TraceInformation("Client disconnected: '{0}'", (object) ex.Message);
          }
        }
      }
      if (asyncState.disconnected)
        return;
      this.DisconnectPair(asyncState);
    }

    private void TargetRead(IAsyncResult asyncResult)
    {
      TcpPortForwarder.ClientPair asyncState = asyncResult.AsyncState as TcpPortForwarder.ClientPair;
      if (!asyncState.disconnected)
      {
        if (asyncState.target.Connected)
        {
          try
          {
            int count = asyncState.targetStream.EndRead(asyncResult);
            if (count > 0)
            {
              if (asyncState.source.Connected)
              {
                asyncState.sourceStream.BeginWrite(asyncState.targetBuffer, 0, count, new AsyncCallback(this.SourceWrite), (object) asyncState);
                return;
              }
            }
          }
          catch (Exception ex)
          {
            Trace.TraceWarning("Server disconnected '{0}'", (object) ex.Message);
          }
        }
      }
      if (asyncState.disconnected)
        return;
      this.DisconnectPair(asyncState);
    }

    private void DisconnectPair(TcpPortForwarder.ClientPair pair)
    {
      if (pair.disconnected)
        return;
      try
      {
        try
        {
          if (pair.target.Client.Connected)
            pair.target.Client.Close();
        }
        catch
        {
        }
        if (!pair.disconnected)
        {
          pair.disconnected = true;
          this.OnAfterTargetDisconnect(pair.source.Client);
        }
        try
        {
          if (!pair.source.Client.Connected)
            return;
          pair.source.Client.Close();
        }
        catch
        {
        }
      }
      catch (Exception ex)
      {
        Trace.TraceError(ex.ToString());
      }
    }

    private void TargetWrite(IAsyncResult asyncResult)
    {
      TcpPortForwarder.ClientPair pair = asyncResult.AsyncState != null ? asyncResult.AsyncState as TcpPortForwarder.ClientPair : throw new ArgumentNullException("asyncResult.AsyncState");
      if (pair.disconnected)
      {
        try
        {
          pair.targetStream.EndWrite(asyncResult);
        }
        catch
        {
        }
      }
      else
      {
        try
        {
          pair.targetStream.EndWrite(asyncResult);
          pair.sourceStream.BeginRead(pair.sourceBuffer, 0, pair.sourceBuffer.Length, new AsyncCallback(this.SourceRead), (object) pair);
        }
        catch
        {
          this.DisconnectPair(pair);
        }
      }
    }

    private void SourceWrite(IAsyncResult asyncResult)
    {
      TcpPortForwarder.ClientPair asyncState = asyncResult.AsyncState as TcpPortForwarder.ClientPair;
      if (asyncState.disconnected)
      {
        try
        {
          asyncState.sourceStream.EndWrite(asyncResult);
        }
        catch
        {
        }
      }
      else
      {
        try
        {
          asyncState.sourceStream.EndWrite(asyncResult);
          asyncState.targetStream.BeginRead(asyncState.targetBuffer, 0, asyncState.targetBuffer.Length, new AsyncCallback(this.TargetRead), (object) asyncState);
        }
        catch
        {
          this.DisconnectPair(asyncState);
        }
      }
    }

    public void Stop() => this.listener.Stop();

    public event AfterTargetDisconnectHandler AfterTargetDisconnect;

    public virtual void OnAfterTargetDisconnect(Socket client)
    {
      if (this.AfterTargetDisconnect == null)
        return;
      try
      {
        this.AfterTargetDisconnect((object) this, new AfterTargetDisconnectArgs(client));
      }
      catch
      {
      }
    }

    public event BeforeTargetConnectHandler BeforeTargetConnect;

    public virtual void OnBeforeTargetConnect(Socket client)
    {
      if (this.BeforeTargetConnect == null)
        return;
      try
      {
        this.BeforeTargetConnect((object) this, new BeforeTargetConnectArgs(client));
      }
      catch
      {
      }
    }

    private class ClientPair
    {
      public int connectRetryCount;
      public bool disconnected;
      public TcpClient source;
      public TcpClient target;
      public NetworkStream sourceStream;
      public NetworkStream targetStream;
      public byte[] sourceBuffer = new byte[65536];
      public byte[] targetBuffer = new byte[65536];
    }
  }
}
