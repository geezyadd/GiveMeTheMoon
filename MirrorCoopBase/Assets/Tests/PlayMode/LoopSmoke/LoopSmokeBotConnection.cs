#if UNITY_INCLUDE_TESTS
using System;
using Mirror;

namespace Tests.PlayMode.LoopSmoke {
    // A server-side connection with no client behind it: what the server sends to it is dropped.
    public sealed class LoopSmokeBotConnection : NetworkConnectionToClient {
        public LoopSmokeBotConnection(int connectionId) : base(connectionId) { }

        public override void Disconnect() =>
            isReady = false;

        protected override void SendToTransport(ArraySegment<byte> segment, int channelId = Channels.Reliable) { }
    }
}
#endif
