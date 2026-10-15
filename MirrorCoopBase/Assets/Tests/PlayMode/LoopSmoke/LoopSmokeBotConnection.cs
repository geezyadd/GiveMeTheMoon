#if UNITY_INCLUDE_TESTS
using System;
using Game.Connection;
using Mirror;

namespace Tests.PlayMode.LoopSmoke {
    // A server-side connection with no client behind it: what the server sends to it is dropped, except the answer to
    // its authentication request, which the bot reads like a client would.
    public sealed class LoopSmokeBotConnection : NetworkConnectionToClient {
        private readonly Unbatcher _unbatcher = new Unbatcher();

        public LoopSmokeBotConnection(int connectionId) : base(connectionId) { }

        public ConnectionRejectReason? AuthResponse { get; private set; }

        public override void Disconnect() =>
            isReady = false;

        protected override void SendToTransport(ArraySegment<byte> segment, int channelId = Channels.Reliable) {
            if (channelId != Channels.Reliable || _unbatcher.AddBatch(segment) == false)
                return;

            while (_unbatcher.GetNextMessage(out ArraySegment<byte> message, out double _))
                ReadAuthResponse(message);
        }

        private void ReadAuthResponse(ArraySegment<byte> message) {
            using NetworkReaderPooled reader = NetworkReaderPool.Get(message);
            if (NetworkMessages.UnpackId(reader, out ushort id) == false)
                return;

            if (id == NetworkMessageId<ConnectionAuthenticator.AuthResponseMessage>.Id)
                AuthResponse = (ConnectionRejectReason)reader.Read<ConnectionAuthenticator.AuthResponseMessage>().rejection;
        }
    }
}
#endif
