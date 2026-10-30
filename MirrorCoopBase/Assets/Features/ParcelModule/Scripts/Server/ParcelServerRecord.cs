using Features.ParcelModule.Scripts.Crate;
using UnityEngine;

namespace Features.ParcelModule.Scripts.Server {
    // Server: one spawned parcel crate and what only the server keeps about it: its cargo target for the bugs, when it
    // came into its current state and where its thief was last.
    public sealed class ParcelServerRecord {
        public uint NetId { get; }
        public ParcelCrate Crate { get; }
        public ParcelCargoTarget CargoTarget { get; }
        public float StateSince { get; private set; }
        public Vector3 CarrierPosition { get; private set; }
        // The rope that pulls it in (Pulled).
        public float PullSpeed { get; private set; }
        public float MaxRopeMeters { get; private set; }

        public ParcelServerRecord(uint netId, ParcelCrate crate, ParcelCargoTarget cargoTarget, float now) {
            NetId = netId;
            Crate = crate;
            CargoTarget = cargoTarget;
            StateSince = now;
        }

        public void RestartStateClock(float now) =>
            StateSince = now;

        public void RememberCarrier(Vector3 carrierPosition) =>
            CarrierPosition = carrierPosition;

        public void RememberPull(float pullSpeed, float maxRopeMeters) {
            PullSpeed = pullSpeed;
            MaxRopeMeters = maxRopeMeters;
        }
    }
}
