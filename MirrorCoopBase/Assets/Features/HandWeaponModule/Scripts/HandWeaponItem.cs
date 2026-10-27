using Features.WeaponModule.Scripts.Feel;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Features.HandWeaponModule.Scripts {
    // Marks a grabbable item as a hand weapon of its definition and binds the item's feel once. The feel's effects
    // live in a frame of their own in the item's scene, so a tracer does not swing with the hand that fired it.
    public sealed class HandWeaponItem : MonoBehaviour {
        private const string FEEL_FRAME_NAME = "HandWeaponFeelFrame";

        [SerializeField] private HandWeaponDefinition _definition;

        private Transform _feelFrame;

        public HandWeaponDefinition Definition => _definition;
        public IWeaponFeelPresenter Feel { get; private set; }

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
    }
}
