using System.Collections.Generic;
using UnityEngine;

namespace Features.ShipModule.Scripts.Impacts {
    // The ship's hull for the rock collision query: boxes in ship space around the deck and the modules. Data only,
    // no collider: the server asks the field data with these boxes, players still walk on the deck colliders.
    public sealed class ShipHullVolume : MonoBehaviour {
        private static readonly Color _gizmoColor = new(1f, 0.45f, 0.1f, 0.9f);

        [SerializeField] private ShipHullBox[] _boxes;

        public IReadOnlyList<ShipHullBox> Boxes => _boxes;

#if UNITY_EDITOR
        private void OnDrawGizmosSelected() {
            if (_boxes == null)
                return;

            Gizmos.color = _gizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            foreach (ShipHullBox box in _boxes)
                Gizmos.DrawWireCube(box.Center, box.HalfExtents * 2f);
        }
#endif
    }
}
