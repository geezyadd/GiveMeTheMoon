using System;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Parts.Delivery {
    [Serializable]
    public sealed class RopeDeliveryDefinition : IShotDeliveryDefinition {
        [Tooltip("How near the rope's line, meters, a target is caught: small, far targets need a forgiving aim.")]
        [SerializeField, Min(0f)] private float _catchRadiusMeters;
        [Tooltip("What the rope can strike. Leave out the own ship and the players: a rope never hooks them.")]
        [SerializeField] private LayerMask _hitLayers;

        public int MaxHitsPerShot => 1;

        public IShotDelivery CreateDelivery(IShotDeliveryContext context) =>
            new RopeDelivery(context.CreateSphereCast(_hitLayers, _catchRadiusMeters));
    }
}
