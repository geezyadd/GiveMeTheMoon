#if UNITY_INCLUDE_TESTS
using System;
using Mirror;
using NUnit.Framework;

namespace Tests.PlayMode.LoopSmoke {
    // Hands one message to a peer the way its transport would: packed, batched, through the transport's receive
    // callback, so the peer's real message handlers process it.
    public static class LoopSmokeTransportInput {
        private const int BATCH_THRESHOLD = 1200;

        public static void Deliver<T>(T message, Action<ArraySegment<byte>, int> receive) where T : struct, NetworkMessage {
            using NetworkWriterPooled packed = NetworkWriterPool.Get();
            NetworkMessages.Pack(message, packed);
            Batcher batcher = new Batcher(BATCH_THRESHOLD);
            batcher.AddMessage(packed.ToArraySegment(), NetworkTime.localTime);
            using NetworkWriterPooled batch = NetworkWriterPool.Get();
            Assert.IsTrue(batcher.GetBatch(batch), $"{typeof(T).Name} could not be batched.");
            receive(batch.ToArraySegment(), Channels.Reliable);
        }
    }
}
#endif
