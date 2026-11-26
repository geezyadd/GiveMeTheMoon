#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;

namespace Tests.Coop.Shared {
    // What the connection tests compare beyond CoopObservation: the hull and its damage, and the local player's own
    // state (hand, seat, body, scene, session role). Public fields for JsonUtility.
    [Serializable]
    public sealed class CoopRunState {
        public string Scene;
        public bool IsHosting;
        public string PresetId;
        public float Hull;
        public float HullMax;
        public string[] BrokenSockets;
        public string[] OpenBreaches;
        public bool HasLocalPlayer;
        public Vector3 PlayerPosition;
        public uint HeldItemNetId;
        // Bound to a seat of the ship.
        public bool IsRiding;
        // On the ship: seated or carried by its deck (SessionQueries.IsAboard).
        public bool IsAboard;
        public bool IsSeated;
        public bool IsTurretGunner;
        public bool IsKinematic;
        public bool DetectsCollisions;
        public bool IsHovering;
        public bool IsControlBlocked;
        public bool StandsOnGround;
        public float WalkedMeters;
        public int PlayerCount;

        public string Describe() =>
            $"scene {Scene}, hosting {IsHosting}, preset '{PresetId}', hull {Hull:0.0}/{HullMax:0.0}, "
            + $"broken [{string.Join(CoopObservation.LIST_SEPARATOR, BrokenSockets ?? Array.Empty<string>())}], "
            + $"breaches [{string.Join(CoopObservation.LIST_SEPARATOR, OpenBreaches ?? Array.Empty<string>())}], "
            + $"players {PlayerCount}, local player {HasLocalPlayer} at {PlayerPosition}: held #{HeldItemNetId}, riding {IsRiding}, aboard {IsAboard}, seated {IsSeated}, "
            + $"gunner {IsTurretGunner}, kinematic {IsKinematic}, collisions {DetectsCollisions}, hover {IsHovering}, "
            + $"controls blocked {IsControlBlocked}, on ground {StandsOnGround}, walked {WalkedMeters:0.00} m";
    }
}
#endif
