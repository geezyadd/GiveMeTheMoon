using System;
using Features.ShipModule.Scripts.Contracts;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ShipDamageModule.Scripts.Repair {
    // The rack on the deck the run's starting tools lie on: the server puts one of each listed tool at its point.
    public sealed class ShipToolRack : MonoBehaviour {
        [SerializeField] private ShipToolRackSlot[] _slots = Array.Empty<ShipToolRackSlot>();

        private IComponentRegistry<ShipToolRack> _shipToolRackRegistry;

        [Inject]
        private void InjectDependencies(IComponentRegistry<ShipToolRack> shipToolRackRegistry) =>
            _shipToolRackRegistry = shipToolRackRegistry;

        private void OnEnable() =>
            _shipToolRackRegistry.Add(this);

        private void OnDisable() =>
            _shipToolRackRegistry.Remove(this);

        // Server only: the run calls it once, at its start. The run starts while the game scene may not be the active
        // one yet, so the tools are made under the rack: they stay in the ship's scene and go away with it.
        public void ServerStock() {
            foreach (ShipToolRackSlot slot in _slots) {
                GameObject item = Instantiate(slot.Tool.ItemPrefab.gameObject, slot.Point.position, slot.Point.rotation, transform);
                item.transform.SetParent(null, true);
                NetworkServer.Spawn(item);
            }
        }
    }
}
