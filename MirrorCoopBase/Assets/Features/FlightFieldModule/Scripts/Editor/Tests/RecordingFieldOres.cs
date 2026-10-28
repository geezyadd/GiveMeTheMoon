using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Ore;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The synced ore list without a network: a field at the world origin with no turn, written and read in place.
    public sealed class RecordingFieldOres : IFieldOres, IFieldOreWriter {
        private readonly List<FieldOreEntry> _entries = new();

        public bool HasField { get; set; } = true;
        public bool IsAvailable { get; set; } = true;
        public Vector3 Origin { get; set; } = Vector3.zero;
        public Quaternion Rotation => Quaternion.identity;
        public int Count => _entries.Count;

        public FieldOreEntry Get(int index) =>
            _entries[index];

        public bool TryFindIndex(int id, out int index) {
            index = _entries.FindIndex(entry => entry.Id == id);
            return index >= 0;
        }

        public void ServerAdd(FieldOreEntry entry) =>
            _entries.Add(entry);

        public void ServerSet(int index, FieldOreEntry entry) =>
            _entries[index] = entry;

        public void ServerRemoveAt(int index) =>
            _entries.RemoveAt(index);
    }
}
