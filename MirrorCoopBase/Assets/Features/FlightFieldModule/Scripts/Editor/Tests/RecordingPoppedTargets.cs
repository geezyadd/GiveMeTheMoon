using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Targets;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The popped balloon list without a network bridge: what the server writes is what every reader sees.
    public sealed class RecordingPoppedTargets : IFieldPoppedTargets, IFieldPoppedTargetWriter {
        private readonly HashSet<FieldTargetId> _poppedTargets = new();

        public bool IsAvailable => true;
        public int Count => _poppedTargets.Count;

        public bool IsPopped(FieldTargetId id) =>
            _poppedTargets.Contains(id);

        public void ServerAdd(FieldTargetId id) =>
            _poppedTargets.Add(id);

        public void ServerClear() =>
            _poppedTargets.Clear();
    }
}
