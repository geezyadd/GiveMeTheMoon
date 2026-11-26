using UnityEngine;

namespace Features.ExplosionModule.Configuration {
    // One kind of explosion (small, medium, large...) as data: how far its wave goes, how fast it spreads, how hard it
    // throws what it reaches and how long a ragdoll lies limp. A new kind is a new asset in the configuration's list.
    [CreateAssetMenu(fileName = "ExplosionPreset", menuName = "Explosion/Preset")]
    public sealed class ExplosionPreset : ScriptableObject {
        [Tooltip("Name shown on the debug window's button.")]
        [SerializeField] private string _displayName = "Explosion";
        [Tooltip("The wave stops at this distance from the centre, m.")]
        [SerializeField, Min(0.1f)] private float _radius = 5f;
        [Tooltip("How fast the wave's front spreads from the centre, m/s.")]
        [SerializeField, Min(0.1f)] private float _waveSpeed = 30f;
        [Tooltip("Velocity change given to a body right at the centre, m/s; farther away it is this times the falloff.")]
        [SerializeField, Min(0f)] private float _centerSpeed = 15f;
        [Tooltip("Share of the centre speed (0..1) by distance from the centre as a share of the radius (0..1).")]
        [SerializeField] private AnimationCurve _falloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        [Tooltip("Tips the throw up: the direction is outward + up times this, normalized.")]
        [SerializeField, Min(0f)] private float _upwardShare = 0.5f;
        [Tooltip("How long a ragdoll the wave reaches lies limp, s, before it gets up.")]
        [SerializeField, Min(0f)] private float _knockdownSeconds = 2f;
        [Tooltip("A ragdoll is knocked down only where the falloff share is at least this; farther it is only pushed.")]
        [SerializeField, Range(0f, 1f)] private float _knockdownMinShare = 0.2f;
        [Tooltip("The wave's sphere fades out over this many seconds once it has reached the radius.")]
        [SerializeField, Min(0f)] private float _sphereFadeSeconds = 0.3f;
        [SerializeField] private Color _sphereColor = new(1f, 0.55f, 0.15f, 0.45f);

        public string DisplayName => _displayName;
        public float Radius => _radius;
        public float WaveSpeed => _waveSpeed;
        public float CenterSpeed => _centerSpeed;
        public float UpwardShare => _upwardShare;
        public float KnockdownSeconds => _knockdownSeconds;
        public float KnockdownMinShare => _knockdownMinShare;
        public float SphereFadeSeconds => _sphereFadeSeconds;
        public Color SphereColor => _sphereColor;

        // The falloff share at this distance from the centre: the curve's value inside the radius, 0 past it.
        public float ShareAt(float distance) {
            if (distance > _radius)
                return 0f;

            return Mathf.Clamp01(_falloff.Evaluate(distance / _radius));
        }
    }
}
