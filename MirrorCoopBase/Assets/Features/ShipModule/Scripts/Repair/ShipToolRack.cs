using System;
using Mirror;
using UnityEngine;

namespace Features.ShipModule.Scripts.Repair {
    // The rack on the deck the run's starting tools lie on: the server puts one of each listed tool at its point.
    public sealed class ShipToolRack : MonoBehaviour {
        [Serializable]
        public sealed class RackSlot {
            public RepairToolDefinition Tool;
            public Transform Point;
        }

        [SerializeField] private RackSlot[] _slots = Array.Empty<RackSlot>();

        // Server only: the run calls it once, at its start. The run starts while the game scene may not be the active
        // one yet, so the tools are made under the rack: they stay in the ship's scene and go away with it.
        public void ServerStock() {
            foreach (RackSlot slot in _slots) {
                GameObject item = Instantiate(slot.Tool.ItemPrefab.gameObject, slot.Point.position, slot.Point.rotation, transform);
                item.transform.SetParent(null, true);
                NetworkServer.Spawn(item);
            }
        }
    }
}
