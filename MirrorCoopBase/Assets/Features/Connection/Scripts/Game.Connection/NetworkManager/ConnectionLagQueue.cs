using System;
using System.Collections.Generic;
using Mirror;

namespace Game.Connection
{
    // Debug builds only: holds one direction's outgoing messages until their simulated arrival time. Each message gets
    // its own random jitter, as on a real link, so unreliable messages can overtake each other. Reliable ones keep
    // their order: a lost reliable message is resent one round trip later and holds back the ones behind it.
    public sealed class ConnectionLagQueue
    {
        readonly struct LaggedMessage
        {
            public readonly int ConnectionId;
            public readonly byte[] Bytes;
            public readonly int ChannelId;
            public readonly double DeliverAt;

            public LaggedMessage(int connectionId, byte[] bytes, int channelId, double deliverAt)
            {
                ConnectionId = connectionId;
                Bytes = bytes;
                ChannelId = channelId;
                DeliverAt = deliverAt;
            }
        }

        readonly List<LaggedMessage> pending = new List<LaggedMessage>();
        readonly Random random;
        double lastReliableDeliverAt;

        public ConnectionLagQueue(Random random)
        {
            this.random = random;
        }

        public ConnectionLagSettings Settings { get; set; }

        public int PendingCount => pending.Count;

        public void Enqueue(int connectionId, ArraySegment<byte> segment, int channelId, double now)
        {
            bool lost = random.NextDouble() * 100d < Settings.LossPercent;
            if (lost && channelId == Channels.Unreliable)
                return;

            double deliverAt = now + Settings.LatencySeconds + random.NextDouble() * Settings.JitterSeconds;
            if (channelId != Channels.Unreliable)
            {
                if (lost)
                    deliverAt += Settings.LatencySeconds * 2d;
                deliverAt = Math.Max(deliverAt, lastReliableDeliverAt);
                lastReliableDeliverAt = deliverAt;
            }

            byte[] bytes = new byte[segment.Count];
            Buffer.BlockCopy(segment.Array, segment.Offset, bytes, 0, segment.Count);
            pending.Add(new LaggedMessage(connectionId, bytes, channelId, deliverAt));
        }

        // Sends every message whose time has come, in the order they were queued.
        public void Deliver(double now, Action<int, ArraySegment<byte>, int> send)
        {
            for (int i = 0; i < pending.Count; i++)
            {
                LaggedMessage message = pending[i];
                if (message.DeliverAt > now)
                    continue;

                send(message.ConnectionId, new ArraySegment<byte>(message.Bytes), message.ChannelId);
                pending.RemoveAt(i);
                i--;
            }
        }

        public void Clear()
        {
            pending.Clear();
            lastReliableDeliverAt = 0d;
        }
    }
}
