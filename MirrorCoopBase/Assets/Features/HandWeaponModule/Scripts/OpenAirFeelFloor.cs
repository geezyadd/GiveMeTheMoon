using Features.WeaponModule.Scripts.Feel;
using UnityEngine;

namespace Features.HandWeaponModule.Scripts {
    // A frame with no floor for a weapon in a hand: its tracers and impacts live in the frame, nothing lands on it.
    public sealed class OpenAirFeelFloor : IWeaponFeelFloor {
        public Transform Frame { get; }

        public OpenAirFeelFloor(Transform frame) =>
            Frame = frame;

        public bool TryGetFloorHeight(Vector3 framePoint, out float floorHeight) {
            floorHeight = 0f;
            return false;
        }
    }
}
