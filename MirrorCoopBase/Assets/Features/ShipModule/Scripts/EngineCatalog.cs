using System;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    [CreateAssetMenu(menuName = "Game/Engine Catalog", fileName = "EngineCatalog")]
    public sealed class EngineCatalog : ScriptableObject {
        public const string ResourceName = "EngineCatalog";

        [Serializable]
        public sealed class EngineStats {
            [SerializeField] private ItemViewId _view = ItemViewId.Engine;
            [SerializeField] private float _thrust = 3f;
            [SerializeField] private float _flightSpeed;

            public ItemViewId View => _view;
            public float Thrust => _thrust;
            public float FlightSpeed => Mathf.Max(0f, _flightSpeed);
        }

        [SerializeField] private EngineStats[] _engines;

        public bool TryGet(ItemViewId view, out EngineStats stats) {
            stats = null;
            if (view == ItemViewId.None || _engines == null)
                return false;

            for (int i = 0; i < _engines.Length; i++) {
                EngineStats entry = _engines[i];
                if (entry == null || entry.View != view)
                    continue;

                stats = entry;
                return true;
            }

            return false;
        }
    }
}
