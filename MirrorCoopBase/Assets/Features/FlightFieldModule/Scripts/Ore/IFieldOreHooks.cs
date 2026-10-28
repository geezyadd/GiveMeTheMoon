using Features.FlightFieldModule.Contracts;
using Features.NetworkModelModule.Scripts;
using Features.WeaponModule.Contracts;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Ore {
    // Server: hooks a piece of ore on a player's rope (it is pulled to the ship from then on) and lets it go again (it
    // drifts on from where it is). A player pulls one piece at a time: a new hook lets the old one go.
    public interface IFieldOreHooks {
        public FieldOreHookResult ServerHook(FieldOreId id, PlayerKey hooker, string weaponId, Vector3 hitPoint, in WeaponPull pull);
        public void ServerRelease(FieldOreId id);
    }
}
