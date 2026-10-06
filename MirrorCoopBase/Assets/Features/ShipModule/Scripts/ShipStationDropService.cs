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
            ModuleDefinition control = FindControlDrop();
            if (control != null) {
                SpawnDrop(control, origin, placed);
                placed++;
            }

            for (int i = 0; i < _stations.Drops.Length; i++) {
                ShipStationCatalog.DropEntry entry = _stations.Drops[i];
                if (entry.MinLoop > loopIndex || entry.Module.IsControlModule)
                    continue;

                for (int n = 0; n < entry.Count; n++) {
                    SpawnDrop(entry.Module, origin, placed);
                    placed++;
                }
            }
        }

        private static void SpawnDrop(ModuleDefinition module, Transform origin, int placed) {
            float side = placed % 2 == 0 ? -1f : 1f;
            float along = placed / 2 * DROP_ALONG_STEP;
            Vector3 local = new Vector3(side * DROP_LATERAL, DROP_HEIGHT, DROP_ALONG_ORIGIN - along);
            GameObject item = Object.Instantiate(module.ItemPrefab.gameObject, origin.TransformPoint(local), origin.rotation);
            NetworkServer.Spawn(item);
        }

        // A control module drops once at every station, ahead of the other drops, whatever its loop and count.
        private ModuleDefinition FindControlDrop() {
            for (int i = 0; i < _stations.Drops.Length; i++) {
                ModuleDefinition module = _stations.Drops[i].Module;
                if (module.IsControlModule)
                    return module;
            }

            return null;
        }
    }
}
