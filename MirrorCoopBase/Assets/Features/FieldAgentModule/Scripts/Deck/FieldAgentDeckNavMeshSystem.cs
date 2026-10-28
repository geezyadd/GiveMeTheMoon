using System;
using Features.FieldAgentModule.Scripts.Configuration;
using Features.ShipModule.Scripts;
using Mirror;
using UnityEngine;
using UnityEngine.AI;
using Zenject;

namespace Features.FieldAgentModule.Scripts.Deck {
    // Server: puts the bound ship's baked deck NavMesh (the ship prefab's NavMeshSurface) at the anchor in ship space
    // and takes it away when the ship goes. The surface itself stays off, so no peer has a copy in the world.
    public sealed class FieldAgentDeckNavMeshSystem : ITickable, IDisposable {
        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly FieldAgentConfiguration _fieldAgentConfiguration;
        private NavMeshDataInstance _instance;
        private ShipBase _placedFor;

        public FieldAgentDeckNavMeshSystem(IShipRunBindingModel shipRunBindingModel, FieldAgentConfiguration fieldAgentConfiguration) {
            _shipRunBindingModel = shipRunBindingModel;
            _fieldAgentConfiguration = fieldAgentConfiguration;
        }

        public void Dispose() =>
            Remove();

        public void Tick() {
            ShipBase ship = NetworkServer.active ? _shipRunBindingModel.Ship : null;
            if (ship == _placedFor)
                return;

            Remove();
            if (ship == null)
                return;

            _instance = NavMesh.AddNavMeshData(ship.DeckNavMeshData, _fieldAgentConfiguration.DeckNavMeshAnchor, Quaternion.identity);
            _placedFor = ship;
        }

        private void Remove() {
            if (_instance.valid)
                _instance.Remove();

            _placedFor = null;
        }
    }
}
