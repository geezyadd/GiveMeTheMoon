using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts {
    // Marks a spot on the ship's deck where a revived player appears, standing, facing the marker's forward.
    public sealed class ShipDeckSpawnPoint : MonoBehaviour {
        private IShipDeckSpawnPointRegistry _shipDeckSpawnPointRegistry;

        [Inject]
        private void InjectDependencies(IShipDeckSpawnPointRegistry shipDeckSpawnPointRegistry) =>
            _shipDeckSpawnPointRegistry = shipDeckSpawnPointRegistry;

        private void OnEnable() =>
            _shipDeckSpawnPointRegistry.Add(this);

        private void OnDisable() =>
            _shipDeckSpawnPointRegistry.Remove(this);
    }
}
