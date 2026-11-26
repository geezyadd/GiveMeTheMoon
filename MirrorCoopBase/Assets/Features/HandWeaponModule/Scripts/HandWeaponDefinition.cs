using Features.WeaponModule.Scripts.Definitions;
using UnityEngine;

namespace Features.HandWeaponModule.Scripts {
    // The single description of one weapon carried in the hand: what the shop sells and which weapon it fires.
    [CreateAssetMenu(menuName = "Game/Hand Weapon Definition", fileName = "HandWeaponDefinition")]
    public sealed class HandWeaponDefinition : ScriptableObject {
        [Tooltip("Stable id: never rename it once it is used.")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private long _price;
        [SerializeField] private HandWeaponItem _itemPrefab;
        [SerializeField] private WeaponDefinition _weapon;
        [Tooltip("What the crosshair's ray can rest on; the shot flies from the muzzle to that point. Use the weapon's hit layers.")]
        [SerializeField] private LayerMask _crosshairLayers;
        [Tooltip("The point of the weapon the hand holds (the middle of the grip), in the weapon model's frame (right, up, forward): it sits in the holder's palm.")]
        [SerializeField] private Vector3 _gripOffset;
        [Tooltip("A crosshair point closer than this to the hand, m, (the barrel pressed to the floor or a player) makes the weapon aim along the view instead.")]
        [SerializeField, Min(0f)] private float _minAimDistance = 1.5f;
        [Tooltip("The holder sends where the weapon points at most this often, s, so other players see it turn with the view.")]
        [SerializeField, Min(0f)] private float _aimSendIntervalSeconds = 0.05f;
        [Tooltip("The holder sends a new aim only once it turned at least this many degrees from the last one sent.")]
        [SerializeField, Min(0f)] private float _aimSendMinDegrees = 1f;
        [Tooltip("Seconds other players' view of the weapon takes to catch up with the received aim.")]
        [SerializeField, Min(0f)] private float _remoteAimSmoothingSeconds = 0.08f;

        public string Id => _id;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public string Description => _description;
        public long Price => _price;
        public HandWeaponItem ItemPrefab => _itemPrefab;
        public WeaponDefinition Weapon => _weapon;
        public LayerMask CrosshairLayers => _crosshairLayers;
        public Vector3 GripOffset => _gripOffset;
        public float MinAimDistance => _minAimDistance;
        public float AimSendIntervalSeconds => _aimSendIntervalSeconds;
        public float AimSendMinDegrees => _aimSendMinDegrees;
        public float RemoteAimSmoothingSeconds => _remoteAimSmoothingSeconds;
    }
}
