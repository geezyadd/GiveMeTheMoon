using System;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Parts.Delivery {
    [Serializable]
    public sealed class HitscanDeliveryDefinition : IShotDeliveryDefinition {
        [Tooltip("Largest angle in degrees between the aim and a shot; shots spread evenly over this cone.")]
        [SerializeField, Min(0f)] private float _spreadDegrees;
        [Tooltip("What the ray can strike. Leave out the own ship and the players: a weapon never hits them.")]
        [SerializeField] private LayerMask _hitLayers;

        public float SpreadDegrees => _spreadDegrees;
        public LayerMask HitLayers => _hitLayers;
        public int MaxHitsPerShot => 1;

        public IShotDelivery CreateDelivery(IShotDeliveryContext context) =>
            new HitscanDelivery(_spreadDegrees, context.CreateRaycast(_hitLayers), context.Random);
    }
}
