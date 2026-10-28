using Features.FieldAgentModule.Scripts.Agents;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Deck {
    // Server: the ship's deck NavMesh in ship space (ship-local points in and out).
    public interface IFieldAgentDeckNavigation {
        public bool TrySamplePoint(Vector3 shipLocal, out Vector3 deckPoint);
        public bool TryFindPath(Vector3 fromShipLocal, Vector3 toShipLocal, FieldAgentDeckPath path);
    }
}
