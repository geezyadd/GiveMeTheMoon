using UnityEngine;

namespace Features.ShipModule.Scripts {
    [CreateAssetMenu(menuName = "Game/Ship Radar Catalog", fileName = "ShipRadarCatalog")]
    public sealed class ShipRadarCatalog : ScriptableObject {
        public const string ResourceName = "ShipRadarCatalog";

        [SerializeField] private GameObject _mapStatusPrefab;
        [SerializeField] private GameObject _camMapPrefab;
        [SerializeField] private GameObject _iconPrefab;
        [SerializeField] private GameObject _iconArrowPrefab;
        [SerializeField] private GameObject _arrowIconPrefab;
        [SerializeField] private Sprite _destinationSprite;

        public GameObject MapStatusPrefab => _mapStatusPrefab;
        public GameObject CamMapPrefab => _camMapPrefab;
        public GameObject IconPrefab => _iconPrefab;
        public GameObject IconArrowPrefab => _iconArrowPrefab;
        public GameObject ArrowIconPrefab => _arrowIconPrefab;
        public Sprite DestinationSprite => _destinationSprite;
    }
}
