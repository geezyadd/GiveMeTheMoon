using Mirror;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    public sealed class CruiseRock : NetworkBehaviour {
        private Vector3 _velocity;
        private float _lifetime;
        private float _age;

        internal void ServerLaunch(Vector3 velocity, float lifetime) {
            if (isServer == false)
                return;

            _velocity = velocity;
            _lifetime = Mathf.Max(0.2f, lifetime);
            _age = 0f;
        }

        private void Update() {
            if (isServer == false)
                return;

            float dt = Time.deltaTime;
            if (dt <= 0f)
                return;

            transform.position += _velocity * dt;
            _age += dt;
            if (_age < _lifetime)
                return;

            NetworkServer.Destroy(gameObject);
        }
    }
}
