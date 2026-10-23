using System;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Parts.Delivery {
    [Serializable]
    public sealed class HitscanDeliveryDefinition : IShotDeliveryDefinition {
        [Tooltip("Largest angle in degrees between the aim and a shot; shots spread evenly over this cone.")]
        [SerializeField, Min(0f)] private float _spreadDegrees;

        public float SpreadDegrees => _spreadDegrees;
        public int MaxHitsPerShot => 1;
    }
}
