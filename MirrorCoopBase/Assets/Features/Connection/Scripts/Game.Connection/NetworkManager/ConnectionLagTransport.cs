using System;
using Mirror;
using UnityEngine;

namespace Game.Connection
{
    // Debug builds only: a transport that wraps the chosen one and holds this process's outgoing messages in a lag
    // queue per direction. Incoming messages pass straight through. Unlike Mirror's LatencySimulation, the jitter is
    // drawn per message and reliable messages never change order.
    [DisallowMultipleComponent]
    public sealed class ConnectionLagTransport : Transport, PortTransport
    {
        static readonly System.Random Random = new System.Random();

        readonly ConnectionLagQueue clientToServer = new ConnectionLagQueue(Random);
        readonly ConnectionLagQueue serverToClient = new ConnectionLagQueue(Random);

        Transport wrap;

        public ushort Port
        {
            get => wrap is PortTransport port ? port.Port : (ushort)0;
            set
            {
                if (wrap is PortTransport port)
                    port.Port = value;
            }
        }

        public void SetWrapped(Transport chosen) =>
            wrap = chosen;

        public void ApplySettings(ConnectionLagSettings settings)
        {
            clientToServer.Settings = settings;
            serverToClient.Settings = settings;
        }

        public override bool Available() => wrap.Available();
        public override bool ClientConnected() => wrap.ClientConnected();
        public override Uri ServerUri() => wrap.ServerUri();
        public override bool ServerActive() => wrap.ServerActive();
        public override string ServerGetClientAddress(int connectionId) => wrap.ServerGetClientAddress(connectionId);
        public override void ServerDisconnect(int connectionId) => wrap.ServerDisconnect(connectionId);
        public override int GetBatchThreshold(int channelId) => wrap.GetBatchThreshold(channelId);
        public override int GetMaxPacketSize(int channelId = Channels.Reliable) => wrap.GetMaxPacketSize(channelId);
        public override void Shutdown() => wrap.Shutdown();
        public override void ClientEarlyUpdate() => wrap.ClientEarlyUpdate();
        public override void ServerEarlyUpdate() => wrap.ServerEarlyUpdate();

        public override void ClientConnect(string address)
        {
            ForwardClientCallbacks();
            wrap.ClientConnect(address);
        }

        public override void ClientConnect(Uri uri)
        {
            ForwardClientCallbacks();
            wrap.ClientConnect(uri);
        }

        public override void ClientDisconnect()
        {
            wrap.ClientDisconnect();
            clientToServer.Clear();
        }

        public override void ClientSend(ArraySegment<byte> segment, int channelId = Channels.Reliable) =>
            clientToServer.Enqueue(0, segment, channelId, Time.unscaledTimeAsDouble);

        public override void ServerSend(int connectionId, ArraySegment<byte> segment, int channelId = Channels.Reliable) =>
            serverToClient.Enqueue(connectionId, segment, channelId, Time.unscaledTimeAsDouble);

        public override void ServerStart()
        {
#pragma warning disable CS0618 // Mirror still raises the obsolete callback; forward it like LatencySimulation does.
            wrap.OnServerConnected = OnServerConnected;
#pragma warning restore CS0618
            wrap.OnServerConnectedWithAddress = OnServerConnectedWithAddress;
            wrap.OnServerDataReceived = OnServerDataReceived;
            wrap.OnServerError = OnServerError;
            wrap.OnServerTransportException = OnServerTransportException;
            wrap.OnServerDisconnected = OnServerDisconnected;
            wrap.ServerStart();
        }

        public override void ServerStop()
        {
            wrap.ServerStop();
            serverToClient.Clear();
        }

        public override void ClientLateUpdate()
        {
            clientToServer.Deliver(Time.unscaledTimeAsDouble, SendToServer);
            wrap.ClientLateUpdate();
        }

        public override void ServerLateUpdate()
        {
            serverToClient.Deliver(Time.unscaledTimeAsDouble, SendToClient);
            wrap.ServerLateUpdate();
        }

        public override string ToString() => $"{nameof(ConnectionLagTransport)} {wrap}";

        void ForwardClientCallbacks()
        {
            wrap.OnClientConnected = OnClientConnected;
            wrap.OnClientDataReceived = OnClientDataReceived;
            wrap.OnClientError = OnClientError;
            wrap.OnClientTransportException = OnClientTransportException;
            wrap.OnClientDisconnected = OnClientDisconnected;
        }

        void SendToServer(int connectionId, ArraySegment<byte> segment, int channelId) =>
            wrap.ClientSend(segment, channelId);

        void SendToClient(int connectionId, ArraySegment<byte> segment, int channelId) =>
            wrap.ServerSend(connectionId, segment, channelId);
    }
}
