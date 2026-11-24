using System.Collections.Generic;

namespace Features.BodySyncModule {
    // The body state channels of the players spawned on this peer, for whoever needs all of them (the debug report).
    public sealed class BodyStateChannelRegistry {
        private readonly List<BodyStateChannel> _channels = new();

        public IReadOnlyList<BodyStateChannel> Channels => _channels;

        public void Add(BodyStateChannel channel) {
            if (_channels.Contains(channel) == false)
                _channels.Add(channel);
        }

        public void Remove(BodyStateChannel channel) =>
            _channels.Remove(channel);
    }
}
