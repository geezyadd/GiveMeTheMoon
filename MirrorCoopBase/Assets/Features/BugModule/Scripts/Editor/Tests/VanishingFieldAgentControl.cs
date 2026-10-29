using System;
using System.Collections.Generic;
using Features.FieldAgentModule.Scripts.Agents;
using Features.FieldAgentModule.Scripts.Behaviours;
using Features.FieldAgentModule.Scripts.Configuration;
using Features.FieldAgentModule.Scripts.Server;
using UnityEngine;

namespace Features.BugModule.Scripts.Editor.Tests {
    // Field agents that are either still flying at a known point or already gone (removed from outside), for the bug
    // removal tests; records the despawns.
    public sealed class VanishingFieldAgentControl : IFieldAgentControl {
        private readonly Dictionary<int, Vector3> _flying = new();

        public List<int> Despawned { get; } = new();
        public int Count => _flying.Count;

        public void Fly(int id, Vector3 worldPosition) =>
            _flying[id] = worldPosition;

        public void Vanish(int id) =>
            _flying.Remove(id);

        public bool TryGetWorldPosition(int id, out Vector3 worldPosition) =>
            _flying.TryGetValue(id, out worldPosition);

        public bool Despawn(int id) {
            Despawned.Add(id);
            return _flying.Remove(id);
        }

        public int Spawn(FieldAgentProfile profile, Vector3 worldPosition, IFieldAgentBehaviour behaviour) =>
            throw new NotSupportedException();

        public void DespawnAll() =>
            _flying.Clear();

        public bool SetBehaviour(int id, IFieldAgentBehaviour behaviour) =>
            _flying.ContainsKey(id);

        public bool TakeOff(int id) =>
            _flying.ContainsKey(id);

        public bool KnockOff(int id, Vector3 worldVelocity) =>
            _flying.ContainsKey(id);

        public bool TryGetAgent(int id, out FieldAgentState state) {
            state = default;
            return _flying.ContainsKey(id);
        }
    }
}
