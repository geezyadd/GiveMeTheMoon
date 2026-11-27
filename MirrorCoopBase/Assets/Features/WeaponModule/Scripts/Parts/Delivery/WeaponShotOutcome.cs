using Features.WeaponModule.Contracts;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Parts.Delivery {
    // Where a shot ended: in the air at its range, on a surface, or on a weapon target. The feel draws tracers and
    // impacts from it; a target hit is what the shooter reports to the server.
    public readonly struct WeaponShotOutcome {
        public WeaponShot Shot { get; }
        public Vector3 EndPoint { get; }
        public Vector3 Normal { get; }
        public bool IsImpact { get; }
        public bool IsTargetHit { get; }
        public ulong TargetId { get; }
        // What shows the strike on the struck surface; null for a miss or a surface that shows nothing.
        public IWeaponImpactFeedback ImpactFeedback { get; }
        // A struck body that reacts to the shot (a player's ragdoll): its network id and the struck part.
        public bool HasReceiver { get; }
        public uint ReceiverId { get; }
        public int ReceiverPartIndex { get; }

        private WeaponShotOutcome(WeaponShot shot, Vector3 endPoint, Vector3 normal, bool isImpact, bool isTargetHit, ulong targetId,
                                  IWeaponImpactFeedback impactFeedback, bool hasReceiver = false, uint receiverId = 0, int receiverPartIndex = -1) {
            HasReceiver = hasReceiver;
            ReceiverId = receiverId;
            ReceiverPartIndex = receiverPartIndex;
            Shot = shot;
            EndPoint = endPoint;
            Normal = normal;
            IsImpact = isImpact;
            IsTargetHit = isTargetHit;
            TargetId = targetId;
            ImpactFeedback = impactFeedback;
        }

        public static WeaponShotOutcome CreateMiss(WeaponShot shot, Vector3 endPoint) =>
            new WeaponShotOutcome(shot, endPoint, Vector3.zero, isImpact: false, isTargetHit: false, targetId: 0, impactFeedback: null);

        public static WeaponShotOutcome CreateImpact(WeaponShot shot, WeaponRaycastHit hit) =>
            new WeaponShotOutcome(shot, hit.Point, hit.Normal, isImpact: true, hit.HasTarget, hit.TargetId, hit.ImpactFeedback,
                hit.HasReceiver, hit.ReceiverId, hit.ReceiverPartIndex);

        // A shot that lands somewhere later (a projectile): seen from here it only leaves the muzzle along the aim.
        public static WeaponShotOutcome CreateLaunch(WeaponShot shot, Vector3 direction) =>
            new WeaponShotOutcome(shot, shot.Origin + direction * Mathf.Max(shot.RangeMeters, 1f), Vector3.zero, isImpact: false, isTargetHit: false,
                targetId: 0, impactFeedback: null);

        // A shot as another peer heard of it, rebuilt for the feel and the struck body.
        public static WeaponShotOutcome CreateReceived(WeaponShot shot, Vector3 endPoint, Vector3 normal, bool isImpact, bool isTargetHit,
                                                       ulong targetId, IWeaponImpactFeedback impactFeedback, bool hasReceiver,
                                                       uint receiverId, int receiverPartIndex) =>
            new WeaponShotOutcome(shot, endPoint, normal, isImpact, isTargetHit, targetId, impactFeedback, hasReceiver, receiverId,
                receiverPartIndex);
    }
}
