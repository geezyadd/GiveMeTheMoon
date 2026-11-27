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

        [Header("Ground look")]
        [Tooltip("What an explosion can go off on: the ground effect plays only over these layers.")]
        [SerializeField] private LayerMask _groundLayers = (1 << 0) | (1 << 6) | (1 << 8);
        [Tooltip("The surface must be at most this far under the centre, m: farther, it went off in the air.")]
        [SerializeField, Min(0f)] private float _groundProbeDistance = 1f;
        [Tooltip("The surface right under the centre may lean at most this much, degrees.")]
        [SerializeField, Range(0f, 89f)] private float _groundMaxSlopeDegrees = 40f;
        [Tooltip("The surface around the centre may be at most this much higher or lower than under it, m (rock islands are uneven by about 0.4 m).")]
        [SerializeField, Min(0f)] private float _groundFlatTolerance = 0.6f;

        public IReadOnlyList<ExplosionPreset> Presets => _presets;
        public LayerMask AffectedLayers => _affectedLayers;
        public float DebugDistance => _debugDistance;
        public ExplosionGroundSettings Ground =>
            new(_groundLayers, _groundProbeDistance, _groundMaxSlopeDegrees, _groundFlatTolerance);

        // Matched by the asset's name, not by reference: in a player build a preset referenced from a scene object or a
        // weapon asset is a copy of its own, apart from the one this configuration loads from Addressables.
        public int IndexOf(ExplosionPreset preset) {
            if (preset == null)
                return -1;

            int index = _presets.IndexOf(preset);
            if (index >= 0)
                return index;

            for (int i = 0; i < _presets.Count; i++)
                if (_presets[i] != null && string.Equals(_presets[i].name, preset.name, System.StringComparison.Ordinal))
                    return i;

            return -1;
        }

        public bool TryGet(int index, out ExplosionPreset preset) {
            bool valid = index >= 0 && index < _presets.Count && _presets[index] != null;
            preset = valid ? _presets[index] : null;
            return valid;
        }
    }
}
