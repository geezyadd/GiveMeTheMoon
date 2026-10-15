using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Structures;
using Mirror;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Server {
    // The field's Mirror objects: the server spawns them at their placements and destroys them with the field; a world
    // shift moves them on every peer like the station pads (no transform sync: a late joiner gets the shifted pose in
    // the spawn message).
    public sealed class FieldNetworkedStructures : IFieldNetworkedStructures {
        private readonly FieldStructureCatalog _fieldStructureCatalog;
        private readonly IFieldStructureSpawnRule _fieldStructureSpawnRule;
        private readonly IFieldStructureLayout _fieldStructureLayout;

        public FieldNetworkedStructures(FieldStructureCatalog fieldStructureCatalog, IFieldStructureSpawnRule fieldStructureSpawnRule, IFieldStructureLayout fieldStructureLayout) {
            _fieldStructureCatalog = fieldStructureCatalog;
            _fieldStructureSpawnRule = fieldStructureSpawnRule;
            _fieldStructureLayout = fieldStructureLayout;
        }

        public void ServerSpawn(FlightFieldServerModel flightFieldServerModel) {
            FieldFrame frame = flightFieldServerModel.Field.Frame;
            foreach (FieldPlacement placement in flightFieldServerModel.Field.Placements) {
                FieldStructureDefinition structure = _fieldStructureCatalog.GetRequired(placement.StructureId);
                if (_fieldStructureSpawnRule.IsBuiltLocally(structure))
                    continue;

                Pose fieldPose = _fieldStructureLayout.GetFieldPose(structure, placement.Anchor, placement.Rotation, frame.CellSize);
                Pose worldPose = _fieldStructureLayout.ToWorld(fieldPose, frame.Origin, frame.Forward);
                GameObject instance = Object.Instantiate(structure.Prefab, worldPose.position, worldPose.rotation);
                NetworkServer.Spawn(instance);
                flightFieldServerModel.AddNetworkedStructure(instance.GetComponent<NetworkIdentity>());
            }
        }

        // A scene change or a server stop may have destroyed them already.
        public void ServerDespawn(IReadOnlyFlightFieldServerModel flightFieldServerModel) {
            foreach (NetworkIdentity structure in flightFieldServerModel.NetworkedStructures) {
                if (structure != null)
                    NetworkServer.Destroy(structure.gameObject);
            }
        }

        public void ServerShift(IReadOnlyFlightFieldServerModel flightFieldServerModel, Vector3 delta) {
            foreach (NetworkIdentity structure in flightFieldServerModel.NetworkedStructures) {
                if (structure != null)
                    structure.transform.position += delta;
            }
        }

        public void ClientShift(Vector3 delta) {
            foreach (NetworkIdentity identity in NetworkClient.spawned.Values) {
                if (identity != null && identity.TryGetComponent(out FieldNetworkedStructure _))
                    identity.transform.position += delta;
            }
        }
    }
}
