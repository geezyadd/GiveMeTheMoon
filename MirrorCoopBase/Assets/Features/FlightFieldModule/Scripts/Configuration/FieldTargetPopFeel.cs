using System;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // How a balloon's pop looks on every peer: the coins burst out, hang a moment and fly into the ship, the "+N"
    // text rises and fades.
    [Serializable]
    public struct FieldTargetPopFeel {
        [Tooltip("Coins that burst out of a pop and fly to the ship.")]
        [Min(0)] public int CoinCount;
        [Tooltip("How fast the coins burst out, meters per second.")]
        [Min(0f)] public float CoinBurstSpeed;
        [Tooltip("Rings of coins one above the other the coins leave in (1 = all level).")]
        [Min(1)] public int CoinRings;
        [Tooltip("Angle between the lowest and the highest ring of coins, degrees.")]
        [Min(0f)] public float CoinRingSpreadDegrees;
        [Tooltip("How far above the ship's pivot (over the deck) the coins disappear into the ship, meters.")]
        public float CoinShipHeightMeters;
        [Tooltip("How long the coins fly outwards before they are sucked to the ship, seconds.")]
        [Min(0.01f)] public float CoinBurstSeconds;
        [Tooltip("How long a coin takes from the end of the burst into the ship, seconds.")]
        [Min(0.01f)] public float CoinFlySeconds;
        [Tooltip("Where the '+N' text starts above the balloon's middle, as a share of its radius.")]
        [Min(0f)] public float RewardTextStartHeightShare;
        [Tooltip("How far the '+N' text rises while it fades, meters.")]
        [Min(0f)] public float RewardTextRiseMeters;
        [Tooltip("How long the '+N' text shows, seconds.")]
        [Min(0.01f)] public float RewardTextSeconds;
        [Tooltip("Size of the '+N' text: the world height of a character, meters.")]
        [Min(0.01f)] public float RewardTextSizeMeters;
    }
}
