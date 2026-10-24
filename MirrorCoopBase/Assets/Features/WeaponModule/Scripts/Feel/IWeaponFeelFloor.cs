using UnityEngine;

namespace Features.WeaponModule.Scripts.Feel {
    // The space a weapon's casings and tracers live in and the floor casings land on: a turret gives its ship, so they
    // ride along with it. Points are in the frame's local space.
    public interface IWeaponFeelFloor {
        public Transform Frame { get; }

        public bool TryGetFloorHeight(Vector3 framePoint, out float floorHeight);
    }
}
