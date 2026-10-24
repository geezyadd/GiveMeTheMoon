using System;
using UnityEngine;

namespace Features.ShipModule.Scripts.Impacts {
    // How one kind of rock hit looks and feels on every peer, at full strength (a hit at the reference speed).
    [Serializable]
    public sealed class ShipRockImpactFeel {
        [Tooltip("Camera shake on board: the largest turn of the view, degrees.")]
        [SerializeField] private float _shakeDegrees = 1f;
        [Tooltip("Camera shake on board: the largest move of the view, metres.")]
        [SerializeField] private float _shakeMeters = 0.03f;
        [Tooltip("How long the shake lasts, seconds; it fades out over this time.")]
        [SerializeField] private float _shakeSeconds = 0.35f;
        [Tooltip("How fast the view shakes, swings per second.")]
        [SerializeField] private float _shakeFrequency = 18f;
        [Tooltip("Size of the impact effect (dust and chunks) at the hit point, metres.")]
        [SerializeField] private float _effectSize = 3f;

        public float ShakeDegrees => _shakeDegrees;
        public float ShakeMeters => _shakeMeters;
        public float ShakeSeconds => _shakeSeconds;
        public float ShakeFrequency => _shakeFrequency;
        public float EffectSize => _effectSize;
    }
}
