using Mirror;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    public sealed class CruiseRock : NetworkBehaviour {
        [SyncVar(hook = nameof(OnParkedChanged))]
        private bool _parked = true;

        private Vector3 _velocity;
        private float _lifetime;
        private float _age;

        internal bool IsParked => _parked;

        internal void ServerLaunch(Vector3 velocity, float lifetime) {
            if (isServer == false)
                return;

            _velocity = velocity;
            _lifetime = Mathf.Max(0.2f, lifetime);
            _age = 0f;
            _parked = false;
            gameObject.SetActive(true);
        }

        internal bool ServerExpired() {
            return isServer && _parked == false && _age >= _lifetime;
        }

        internal void ServerPark() {
            if (isServer == false)
                return;

            _velocity = Vector3.zero;
            _parked = true;
            gameObject.SetActive(false);
        }

        private void Update() {
            if (isServer == false || _parked)
                return;

            float dt = Time.deltaTime;
            if (dt <= 0f)
                return;

            transform.position += _velocity * dt;
            _age += dt;
        }

        private void OnParkedChanged(bool previous, bool current) {
            gameObject.SetActive(current == false);
        }
    }
}
