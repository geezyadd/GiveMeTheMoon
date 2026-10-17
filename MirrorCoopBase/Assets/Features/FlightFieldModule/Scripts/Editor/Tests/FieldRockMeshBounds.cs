using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The bounds of a rock prefab's mesh turned and stretched as built, around its pivot, from its real vertices.
    public sealed class FieldRockMeshBounds {
        private readonly Dictionary<FieldRockVisual, Vector3[]> _verticesByPrefab = new();

        public Bounds Get(FieldRockVisual prefab, Quaternion rotation, Vector3 scale) {
            Vector3[] vertices = GetVertices(prefab);
            Matrix4x4 transform = Matrix4x4.TRS(Vector3.zero, rotation, scale);
            Bounds bounds = new(transform.MultiplyPoint3x4(vertices[0]), Vector3.zero);
            foreach (Vector3 vertex in vertices)
                bounds.Encapsulate(transform.MultiplyPoint3x4(vertex));

            return bounds;
        }

        private Vector3[] GetVertices(FieldRockVisual prefab) {
            if (_verticesByPrefab.TryGetValue(prefab, out Vector3[] cached))
                return cached;

            List<Vector3> vertices = new();
            foreach (MeshFilter meshFilter in prefab.GetComponentsInChildren<MeshFilter>(true)) {
                Matrix4x4 toRoot = prefab.transform.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix;
                foreach (Vector3 vertex in meshFilter.sharedMesh.vertices)
                    vertices.Add(toRoot.MultiplyPoint3x4(vertex));
            }

            Vector3[] result = vertices.ToArray();
            _verticesByPrefab.Add(prefab, result);
            return result;
        }
    }
}
