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
        [Tooltip("A press is reported as held at least this long, so every other peer sees even a short click.")]
        [SerializeField, Min(0f)] private float _minTriggerReportSeconds = 0.25f;
        [Tooltip("What the crosshair's ray can rest on; the shot flies from the muzzle to that point. Use the weapon's hit layers.")]
        [SerializeField] private LayerMask _crosshairLayers;
        [Tooltip("Where the weapon sits in its holder's own first-person view, in the view's frame (right, up, forward).")]
        [SerializeField] private Vector3 _viewOffset;
        [Tooltip("Where the weapon sits from the holder's arm point when seen in the hand (third person, other players).")]
        [SerializeField] private Vector3 _handOffset;

        public string Id => _id;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public string Description => _description;
        public long Price => _price;
        public HandWeaponItem ItemPrefab => _itemPrefab;
        public WeaponDefinition Weapon => _weapon;
        public float MinTriggerReportSeconds => _minTriggerReportSeconds;
        public LayerMask CrosshairLayers => _crosshairLayers;
        public Vector3 ViewOffset => _viewOffset;
        public Vector3 HandOffset => _handOffset;
    }
}
