using Features.WeaponModule.Scripts.Definitions;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Feel {
    // A single shot throws the gun back and tips its muzzle up at once; it springs back to rest over the settle seconds.
    public sealed class RecoilKick {
        private readonly Transform _root;
        private readonly Vector3 _restPosition;
        private readonly Quaternion _restRotation;
        private readonly WeaponFeelProfile _profile;

        private float _weight;

        public RecoilKick(Transform root, WeaponFeelProfile profile) {
            _root = root;
            _restPosition = root.localPosition;
            _restRotation = root.localRotation;
            _profile = profile;
        }

        public void Kick() =>
            _weight = 1f;

        public void Advance(float deltaTime) {
            float settleRate = _profile.RecoilSettleSeconds > 0f ? deltaTime / _profile.RecoilSettleSeconds : 1f;
            _weight = Mathf.MoveTowards(_weight, 0f, settleRate);
            float eased = _weight * _weight;
            _root.localPosition = _restPosition + Vector3.back * (_profile.RecoilKickMeters * eased);
            _root.localRotation = _restRotation * Quaternion.Euler(-_profile.RecoilKickDegrees * eased, 0f, 0f);
        }
    }
}
