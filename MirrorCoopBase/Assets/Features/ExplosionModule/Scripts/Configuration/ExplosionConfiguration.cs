using System.Collections.Generic;
using UnityEngine;

namespace Features.ExplosionModule.Configuration {
    // The explosion presets and what every explosion shares: which layers its wave throws.
    // A preset goes over the network as its index in this list, so every peer has the same list.
    [CreateAssetMenu(fileName = "ExplosionConfiguration", menuName = "Explosion/Configuration")]
    public sealed class ExplosionConfiguration : ScriptableObject {
        [SerializeField] private List<ExplosionPreset> _presets = new();
        [Tooltip("Layers whose loose rigidbodies the wave throws (receivers such as players are reached anyway).")]
        [SerializeField] private LayerMask _affectedLayers = ~0;
        [Tooltip("The debug window's explosions go off this far in front of the host's camera, m.")]
        [SerializeField, Min(0f)] private float _debugDistance = 4f;

        public IReadOnlyList<ExplosionPreset> Presets => _presets;
        public LayerMask AffectedLayers => _affectedLayers;
        public float DebugDistance => _debugDistance;

        public int IndexOf(ExplosionPreset preset) =>
            preset != null ? _presets.IndexOf(preset) : -1;

        public bool TryGet(int index, out ExplosionPreset preset) {
            bool valid = index >= 0 && index < _presets.Count && _presets[index] != null;
            preset = valid ? _presets[index] : null;
            return valid;
        }
    }
}
