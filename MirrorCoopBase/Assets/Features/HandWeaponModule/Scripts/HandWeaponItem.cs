using Features.WeaponModule.Scripts.Core;
using Features.WeaponModule.Scripts.Definitions;
using Features.WeaponModule.Scripts.Feel;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Features.HandWeaponModule.Scripts {
    // A grabbable item as a weapon of its definition, and the item's feel bound once. The feel's effects live in a
    // frame of their own in the item's scene, so a tracer does not swing with the hand that fired it. While held, the
    // weapon model (the pose root, with the muzzle) is placed in the view or the hand; let go, it is back on the item.
    // Its capabilities (a magazine) are components on the item.
    public sealed class HandWeaponItem : MonoBehaviour, IWeapon {
        private const string FEEL_FRAME_NAME = "HandWeaponFeelFrame";

        [SerializeField] private HandWeaponDefinition _definition;
        [SerializeField] private Transform _poseRoot;

        private Transform _feelFrame;

        public HandWeaponDefinition HandDefinition => _definition;
        public WeaponDefinition Definition => _definition.Weapon;
        public IWeaponFeelPresenter Feel { get; private set; }
        public bool IsUsable => true;

        private void Awake() {
            Feel = GetComponent<IWeaponFeelPresenter>();
            _feelFrame = new GameObject(FEEL_FRAME_NAME).transform;
            SceneManager.MoveGameObjectToScene(_feelFrame.gameObject, gameObject.scene);
            Feel.Bind(_definition.Weapon.FeelProfile, new OpenAirFeelFloor(_feelFrame));
        }

        // The frame may already be gone with its scene.
        private void OnDestroy() {
            if (_feelFrame != null)
                Destroy(_feelFrame.gameObject);
        }

        public bool TryGetCapability<TCapability>(out TCapability capability) where TCapability : class =>
            WeaponCapabilities.TryGet(this, this, out capability);

        public void SetPose(Pose localPose) =>
            _poseRoot.SetLocalPositionAndRotation(localPose.position, localPose.rotation);

        public void ResetPose() =>
            _poseRoot.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }
}
