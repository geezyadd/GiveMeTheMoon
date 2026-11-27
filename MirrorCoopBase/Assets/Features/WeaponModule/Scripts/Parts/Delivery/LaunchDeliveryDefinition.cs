using System;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Parts.Delivery {
    [Serializable]
    public sealed class LaunchDeliveryDefinition : IShotDeliveryDefinition {
        [Tooltip("Largest angle in degrees between the aim and a launch; launches spread evenly over this cone.")]
        [SerializeField, Min(0f)] private float _spreadDegrees;

        public int MaxHitsPerShot => 0;

        public IShotDelivery CreateDelivery(IShotDeliveryContext context) =>
            new LaunchDelivery(_spreadDegrees, context.Random);
    }
}
