using System;
using Features.CameraModule.Contracts;
using UnityEngine;

namespace Features.ShipImpactModule.Scripts {
    // How one kind of rock hit looks and feels on every peer, at full strength (a hit at the reference speed).
    [Serializable]
    public sealed class ShipRockImpactFeel {
        [Tooltip("Camera shake on board at full strength; the hit's strength scales it.")]
        [SerializeField] private CameraShakeProfile _shake;
        [Tooltip("Size of the impact effect (dust and chunks) at the hit point, metres.")]
        [SerializeField] private float _effectSize = 3f;

        public CameraShakeProfile Shake => _shake;
        public float EffectSize => _effectSize;
    }
}
