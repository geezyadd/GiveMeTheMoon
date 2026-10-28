using System;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Network {
    // One piece of ore as it travels over the network: enough for every peer to place it at any moment by itself, so
    // the entry changes only when the piece drops, is hooked, is let go or is gone, never per frame. A drifting piece
    // is at FieldPosition + FieldVelocity x (now - StartTime) in field space; a hooked one (HookerId set) is pulled in
    // at PullSpeed from ShipOffset (its offset from the ship's middle, world axes, at StartTime) towards the ship.
    [Serializable]
    public struct FieldOreEntry {
        public int Id;
        public string KindId;
        public Vector3 FieldPosition;
        public Vector3 FieldVelocity;
        // Network time the motion starts from: the drop, the hook or the release.
        public double StartTime;
        public double ExpiresAt;
        // The key of the player whose rope pulls the piece; empty while it drifts.
        public string HookerId;
        // The weapon that hooked it: the rope holds while its hooker holds this weapon.
        public string HookWeaponId;
        public Vector3 ShipOffset;
        public float PullSpeed;
        public float MaxRopeMeters;
    }
}
