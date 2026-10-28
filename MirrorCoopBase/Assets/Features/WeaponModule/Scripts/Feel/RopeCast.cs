using System;
using Features.WeaponModule.Scripts.Definitions;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Feel {
    // One thrown rope as the feel shows it: its tip flies out to where the shot ended, stays there a moment when it
    // caught a target, and is reeled back to the muzzle. The end point stays where it was in the world; the rope
    // starts at the muzzle wherever the holder has moved.
    public sealed class RopeCast {
        private readonly WeaponFeelProfile _profile;

        private Vector3 _end;
        private float _length;
        private float _tipDistance;
        private float _holdSecondsLeft;
        private bool _isCaught;

        public RopeCastPhase Phase { get; private set; }
        public bool IsVisible => Phase != RopeCastPhase.None;

        public RopeCast(WeaponFeelProfile profile) =>
            _profile = profile;

        public void Throw(Vector3 origin, Vector3 end, bool isCaught) {
            _end = end;
            _length = Vector3.Distance(origin, end);
            _tipDistance = 0f;
            _isCaught = isCaught;
            Phase = RopeCastPhase.Out;
        }

        public void Advance(float deltaTime) {
            switch (Phase) {
                case RopeCastPhase.None:
                    return;
                case RopeCastPhase.Out:
                    AdvanceOut(deltaTime);
                    return;
                case RopeCastPhase.Hold:
                    AdvanceHold(deltaTime);
                    return;
                case RopeCastPhase.Return:
                    AdvanceReturn(deltaTime);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Phase), Phase, "Unknown rope cast phase.");
            }
        }

        public Vector3 GetTip(Vector3 origin) =>
            Vector3.MoveTowards(origin, _end, _tipDistance);

        private void AdvanceOut(float deltaTime) {
            _tipDistance += _profile.RopeOutSpeedMetersPerSecond * deltaTime;
            if (_tipDistance < _length)
                return;

            _tipDistance = _length;
            _holdSecondsLeft = _profile.RopeHoldSeconds;
            Phase = _isCaught ? RopeCastPhase.Hold : RopeCastPhase.Return;
        }

        private void AdvanceHold(float deltaTime) {
            _holdSecondsLeft -= deltaTime;
            if (_holdSecondsLeft <= 0f)
                Phase = RopeCastPhase.Return;
        }

        private void AdvanceReturn(float deltaTime) {
            _tipDistance -= _profile.RopeReturnSpeedMetersPerSecond * deltaTime;
            if (_tipDistance > 0f)
                return;

            _tipDistance = 0f;
            Phase = RopeCastPhase.None;
        }
    }
}
