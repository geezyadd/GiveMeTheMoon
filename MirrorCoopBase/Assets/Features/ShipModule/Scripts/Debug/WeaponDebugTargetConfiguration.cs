#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace Features.ShipModule.Scripts.Debug {
    // Editor / development builds only: the weapon test dummy and where in front of the ship it floats.
    [CreateAssetMenu(menuName = "Game/Debug/Weapon Debug Target Configuration", fileName = "WeaponDebugTargetConfiguration")]
    public sealed class WeaponDebugTargetConfiguration : ScriptableObject {
        [SerializeField] private WeaponDebugTarget _targetPrefab;
        [Tooltip("Ship-local spot of the dummy: ahead of the ship, above the deck, in front of the turret socket.")]
        [SerializeField] private Vector3 _shipOffset = new Vector3(-2.6f, 7f, 24f);

        public WeaponDebugTarget TargetPrefab => _targetPrefab;
        public Vector3 ShipOffset => _shipOffset;
    }
}
#endif
