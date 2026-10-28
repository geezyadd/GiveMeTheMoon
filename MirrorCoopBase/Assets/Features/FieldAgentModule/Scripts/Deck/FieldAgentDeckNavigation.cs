using Features.FieldAgentModule.Scripts.Agents;
using Features.FieldAgentModule.Scripts.Configuration;
using UnityEngine;
using UnityEngine.AI;

namespace Features.FieldAgentModule.Scripts.Deck {
    // The deck NavMesh lies still at the configuration's anchor in ship space (FieldAgentDeckNavMeshSystem puts it
    // there), so a ship-local point plus the anchor is a NavMesh point however the ship flies or turns.
    public sealed class FieldAgentDeckNavigation : IFieldAgentDeckNavigation {
        private readonly FieldAgentConfiguration _fieldAgentConfiguration;

        public FieldAgentDeckNavigation(FieldAgentConfiguration fieldAgentConfiguration) =>
            _fieldAgentConfiguration = fieldAgentConfiguration;

        public bool TrySamplePoint(Vector3 shipLocal, out Vector3 deckPoint) {
            deckPoint = shipLocal;
            Vector3 anchor = _fieldAgentConfiguration.DeckNavMeshAnchor;
            if (NavMesh.SamplePosition(anchor + shipLocal, out NavMeshHit hit, _fieldAgentConfiguration.DeckSampleMeters, NavMesh.AllAreas) == false)
                return false;

            deckPoint = hit.position - anchor;
            return true;
        }

        public bool TryFindPath(Vector3 fromShipLocal, Vector3 toShipLocal, FieldAgentDeckPath path) {
            path.Clear();
            if (TrySamplePoint(fromShipLocal, out Vector3 from) == false || TrySamplePoint(toShipLocal, out Vector3 to) == false)
                return false;

            Vector3 anchor = _fieldAgentConfiguration.DeckNavMeshAnchor;
            if (NavMesh.CalculatePath(anchor + from, anchor + to, NavMesh.AllAreas, path.NavMeshPath) == false || path.NavMeshPath.status == NavMeshPathStatus.PathInvalid)
                return false;

            int count = path.NavMeshPath.GetCornersNonAlloc(path.Corners);
            for (int i = 0; i < count; i++)
                path.Corners[i] -= anchor;

            path.SetCorners(count, toShipLocal);
            return path.HasPath;
        }
    }
}
