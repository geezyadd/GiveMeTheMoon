using UnityEngine;

namespace Features.ShipModule.Scripts {
    public sealed class ShipItem : MonoBehaviour {
        [SerializeField] private ShipModuleType _type = ShipModuleType.Engine;
        [SerializeField] private ItemViewId _view = ItemViewId.Engine;

        public ShipModuleType Type => _type;
        public ItemViewId View => _view;
    }
}
