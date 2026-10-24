using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Rocks;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The broken rock list without a network bridge: what the server writes is what every reader sees.
    public sealed class RecordingBrokenRocks : IFieldBrokenRocks, IFieldBrokenRockWriter {
        private readonly HashSet<FieldRockId> _brokenRocks = new();

        public bool IsAvailable => true;
        public int Count => _brokenRocks.Count;

        public bool IsBroken(FieldRockId id) =>
            _brokenRocks.Contains(id);

        public void ServerAdd(FieldRockId id) =>
            _brokenRocks.Add(id);

        public void ServerClear() =>
            _brokenRocks.Clear();
    }
}
