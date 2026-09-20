using UnityEngine;

namespace Features.ShipModule.Scripts {
    public sealed class ShipLandingPad : MonoBehaviour {
        [SerializeField] private Transform _landingPoint;
        [SerializeField] private Transform _buildBerth;
        [SerializeField] private Transform _playerSpawn;

        public Transform LandingPoint => _landingPoint != null ? _landingPoint : transform;
        public Transform BuildBerth => _buildBerth != null ? _buildBerth : LandingPoint;
        public Transform PlayerSpawn => _playerSpawn != null ? _playerSpawn : BuildBerth;
    }
}
