using Mirror;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    internal sealed class ShipStationDropService : IShipStationDropService {
        private const float DROP_LATERAL = 7.5f;
        private const float DROP_ALONG_ORIGIN = 2f;
        private const float DROP_ALONG_STEP = 1.8f;
        private const float DROP_HEIGHT = 1f;

        private readonly ShipStationCatalog _stations;
        private readonly IShipRunBindingModel _binding;

        public ShipStationDropService(ShipStationCatalog stations, IShipRunBindingModel binding) {
            _stations = stations;
            _binding = binding;
        }

        public void SpawnDrops(ShipLandingPad pad, int loopIndex) {
            ShipBase ship = _binding.Ship;
            Transform origin = pad != null ? pad.transform : (ship != null ? ship.transform : null);
            if (origin == null)
                return;

            int placed = 0;
            GameObject helm = FindDropPrefab(ShipModuleType.Control);
            if (helm != null) {
                SpawnDrop(helm, origin, placed);
                placed++;
            }

            for (int i = 0; i < _stations.Drops.Length; i++) {
                ShipStationCatalog.DropEntry entry = _stations.Drops[i];
                if (entry == null || entry.Prefab == null || entry.MinLoop > loopIndex)
                    continue;

                if (IsDropType(entry.Prefab, ShipModuleType.Control))
                    continue;

                for (int n = 0; n < entry.Count; n++) {
                    SpawnDrop(entry.Prefab, origin, placed);
                    placed++;
                }
            }
        }

        private static void SpawnDrop(GameObject prefab, Transform origin, int placed) {
            float side = placed % 2 == 0 ? -1f : 1f;
            float along = placed / 2 * DROP_ALONG_STEP;
            Vector3 local = new Vector3(side * DROP_LATERAL, DROP_HEIGHT, DROP_ALONG_ORIGIN - along);
            GameObject item = Object.Instantiate(prefab, origin.TransformPoint(local), origin.rotation);
            NetworkServer.Spawn(item);
        }

        private GameObject FindDropPrefab(ShipModuleType type) {
            for (int i = 0; i < _stations.Drops.Length; i++) {
                ShipStationCatalog.DropEntry entry = _stations.Drops[i];
                if (entry != null && IsDropType(entry.Prefab, type))
                    return entry.Prefab;
            }

            return null;
        }

        private static bool IsDropType(GameObject prefab, ShipModuleType type) {
            if (prefab == null)
                return false;

            ShipItem item = prefab.GetComponent<ShipItem>();
            return item != null && item.Type == type;
        }
    }
}
