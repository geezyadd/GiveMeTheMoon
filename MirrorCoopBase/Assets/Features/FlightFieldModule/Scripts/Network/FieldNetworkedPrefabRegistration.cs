using Features.FlightFieldModule.Scripts.Structures;
using Mirror;

namespace Features.FlightFieldModule.Scripts.Network {
    // Registers the prefab of every networked structure in the catalog with the Mirror client, so a client can spawn
    // what the server spawns for a field. The catalog is the only list: a debug structure that only a test catalog
    // holds is registered there and never ships in the network manager's spawn prefabs.
    public sealed class FieldNetworkedPrefabRegistration : IFieldNetworkedPrefabRegistration {
        private readonly FieldStructureCatalog _fieldStructureCatalog;
        private readonly IFieldStructureSpawnRule _fieldStructureSpawnRule;

        public FieldNetworkedPrefabRegistration(FieldStructureCatalog fieldStructureCatalog, IFieldStructureSpawnRule fieldStructureSpawnRule) {
            _fieldStructureCatalog = fieldStructureCatalog;
            _fieldStructureSpawnRule = fieldStructureSpawnRule;
        }

        // Mirror clears its prefabs when the client shuts down, so this runs on every client start.
        public void RegisterForClient() {
            foreach (FieldStructureDefinition structure in _fieldStructureCatalog.Structures) {
                if (_fieldStructureSpawnRule.IsBuiltLocally(structure))
                    continue;

                NetworkIdentity identity = structure.Prefab.GetComponent<NetworkIdentity>();
                if (!NetworkClient.prefabs.ContainsKey(identity.assetId))
                    NetworkClient.RegisterPrefab(structure.Prefab);
            }
        }
    }
}
