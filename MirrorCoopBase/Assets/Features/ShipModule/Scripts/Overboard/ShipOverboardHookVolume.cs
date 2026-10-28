using Features.WeaponModule.Contracts;
using UnityEngine;

namespace Features.ShipModule.Scripts.Overboard {
    // The trigger round a drifting player that a rope's ray strikes: it names the player as a weapon target.
    public sealed class ShipOverboardHookVolume : MonoBehaviour, IWeaponTargetCollider {
        [SerializeField] private ShipOverboardRider _overboardRider;

        public ulong TargetId => _overboardRider.WeaponTargetId;
    }
}
