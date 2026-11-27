using Features.ScreenShakeModule;
using Features.VfxModule;
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
        [Header("Look")]
        [Tooltip("The effect played where it goes off, on each peer by itself.")]
        [SerializeField] private VfxDefinition _effect;
        [Tooltip("The effect's size; 1 is its prefab's own.")]
        [SerializeField, Min(0.01f)] private float _effectScale = 1f;
        [Tooltip("The effects played instead when it goes off on wide flat ground, one of them at random each time. Empty: always the effect above.")]
        [SerializeField] private VfxDefinition[] _groundEffects = new VfxDefinition[0];
        [Tooltip("The ground effect's size; 1 is its prefab's own.")]
        [SerializeField, Min(0.01f)] private float _groundEffectScale = 1f;
        [Tooltip("The ground must be flat this far around the centre, m, for the ground effect: nearer an edge the effect above plays.")]
        [SerializeField, Min(0f)] private float _groundFlatRadius = 2f;
        [Tooltip("The screen shake of a player who sees it go off.")]
        [SerializeField] private ScreenShakeProfile _shake;
        [Tooltip("The shake is full at the centre and fades to nothing at this distance from the camera, m.")]
        [SerializeField, Min(0.1f)] private float _shakeDistance = 25f;

        public string DisplayName => _displayName;
        public float Radius => _radius;
        public float WaveSpeed => _waveSpeed;
        public float CenterSpeed => _centerSpeed;
        public float UpwardShare => _upwardShare;
        public float KnockdownSeconds => _knockdownSeconds;
        public float KnockdownMinShare => _knockdownMinShare;
        public VfxDefinition Effect => _effect;
        public float EffectScale => _effectScale;
        public float GroundEffectScale => _groundEffectScale;
        public float GroundFlatRadius => _groundFlatRadius;
        public ScreenShakeProfile Shake => _shake;
        public float ShakeDistance => _shakeDistance;

        // One of the ground effects at random; null when there is none.
        public VfxDefinition PickGroundEffect() =>
            _groundEffects.Length > 0 ? _groundEffects[Random.Range(0, _groundEffects.Length)] : null;

        // The falloff share at this distance from the centre: the curve's value inside the radius, 0 past it.
        public float ShareAt(float distance) {
            if (distance > _radius)
                return 0f;

            return Mathf.Clamp01(_falloff.Evaluate(distance / _radius));
        }
    }
}
