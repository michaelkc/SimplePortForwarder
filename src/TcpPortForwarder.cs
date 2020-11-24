using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PortForwarder
{
    internal sealed class TcpPortForwarder
    {
        private readonly int _localPort;
        private readonly string _targetHost;
        private readonly int _targetPort;
        private TcpListener _listener;

        public TcpPortForwarder(int localPort, int targetPort, string targetHost)
        {
            _localPort = localPort;
            _targetPort = targetPort;
            _targetHost = targetHost;
        }

        public void Start()
        {
            _listener = new TcpListener(IPAddress.Any, _localPort);
            _listener.Start();
            _listener.BeginAcceptTcpClient(AcceptTcpClient, _listener);
        }

        private void AcceptTcpClient(IAsyncResult asyncResult)
        {
            if (!(asyncResult.AsyncState is TcpListener asyncState)) throw new Exception("Non-TcpListener AsyncState");
            var clientPair = new ClientPair();
            try
            {
                clientPair.connectRetryCount = 0;
                clientPair.disconnected = false;
                clientPair.source = asyncState.EndAcceptTcpClient(asyncResult);
                clientPair.target = new TcpClient();
                clientPair.target.BeginConnect(_targetHost, _targetPort, TargetConnect, clientPair);
                asyncState.BeginAcceptTcpClient(AcceptTcpClient, _listener);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                Trace.TraceError("Failed when trying to accept new clients with '{0}'", (object) ex.ToString());
            }
        }

        private void TargetConnect(IAsyncResult asyncResult)
        {
            var clientPair = asyncResult.AsyncState != null
                ? (ClientPair) asyncResult.AsyncState
                : throw new ArgumentNullException(nameof(asyncResult));
            try
            {
                clientPair.target.EndConnect(asyncResult);
                clientPair.targetStream = clientPair.target.GetStream();
                clientPair.sourceStream = clientPair.source.GetStream();
                clientPair.sourceStream.BeginRead(clientPair.sourceBuffer, 0, clientPair.sourceBuffer.Length,
                    SourceRead, clientPair);
                clientPair.targetStream.BeginRead(clientPair.targetBuffer, 0, clientPair.targetBuffer.Length,
                    TargetRead, clientPair);
            }
            catch (SocketException ex)
            {
                if (clientPair.connectRetryCount < 2)
                {
                    ++clientPair.connectRetryCount;
                    clientPair.target.BeginConnect(_targetHost, _targetPort, TargetConnect, clientPair);
                    Trace.TraceWarning("Retrying connect");
                }
                else
                {
                    Trace.TraceError("Connection failed: {0}", (object) ex.ToString());
                    clientPair.source.Close();
                }
            }
            catch (ObjectDisposedException)
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
            var asyncState = (ClientPair) asyncResult.AsyncState;
            if (!asyncState.disconnected)
                if (asyncState.source.Connected)
                    try
                    {
                        var count = asyncState.sourceStream.EndRead(asyncResult);
                        if (count > 0)
                        {
                            Encoding.UTF8.GetString(asyncState.sourceBuffer, 0, count);
                            if (asyncState.target.Connected)
                            {
                                asyncState.targetStream.BeginWrite(asyncState.sourceBuffer, 0, count, TargetWrite,
                                    asyncState);
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceInformation("Client disconnected: '{0}'", (object) ex.Message);
                    }

            if (asyncState.disconnected)
                return;
            DisconnectPair(asyncState);
        }

        private void TargetRead(IAsyncResult asyncResult)
        {
            var asyncState = asyncResult.AsyncState as ClientPair;
            if (!asyncState.disconnected)
                if (asyncState.target.Connected)
                    try
                    {
                        var count = asyncState.targetStream.EndRead(asyncResult);
                        if (count > 0)
                            if (asyncState.source.Connected)
                            {
                                asyncState.sourceStream.BeginWrite(asyncState.targetBuffer, 0, count, SourceWrite,
                                    asyncState);
                                return;
                            }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceWarning("Server disconnected '{0}'", (object) ex.Message);
                    }

            if (asyncState.disconnected)
                return;
            DisconnectPair(asyncState);
        }

        private void DisconnectPair(ClientPair pair)
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
            var pair = asyncResult.AsyncState != null
                ? asyncResult.AsyncState as ClientPair
                : throw new ArgumentNullException(nameof(asyncResult));
            if (pair.disconnected)
                try
                {
                    pair.targetStream.EndWrite(asyncResult);
                }
                catch
                {
                }
            else
                try
                {
                    pair.targetStream.EndWrite(asyncResult);
                    pair.sourceStream.BeginRead(pair.sourceBuffer, 0, pair.sourceBuffer.Length, SourceRead, pair);
                }
                catch
                {
                    DisconnectPair(pair);
                }
        }

        private void SourceWrite(IAsyncResult asyncResult)
        {
            var asyncState = asyncResult.AsyncState as ClientPair;
            if (asyncState.disconnected)
                try
                {
                    asyncState.sourceStream.EndWrite(asyncResult);
                }
                catch
                {
                }
            else
                try
                {
                    asyncState.sourceStream.EndWrite(asyncResult);
                    asyncState.targetStream.BeginRead(asyncState.targetBuffer, 0, asyncState.targetBuffer.Length,
                        TargetRead, asyncState);
                }
                catch
                {
                    DisconnectPair(asyncState);
                }
        }

        public void Stop()
        {
            _listener.Stop();
        }
    }
}