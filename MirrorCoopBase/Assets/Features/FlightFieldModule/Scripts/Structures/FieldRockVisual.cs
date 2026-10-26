using Features.FlightFieldModule.Scripts.Rocks;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Structures {
    // The root of a visual rock prefab: the cluster builder gives it one of the recipe's shared materials, so the
    // tint changes without a MaterialPropertyBlock and the SRP Batcher still batches every rock. Its weapon target sits
    // on the mesh, unbound until a field builder binds it.
    public sealed class FieldRockVisual : MonoBehaviour {
        // How far a rock mesh, normalised to about 1 m, reaches from its pivot at most: every layout sizes rocks by it.
        public const float MESH_REACH = 0.7f;

        [SerializeField] private MeshRenderer _renderer;
        [SerializeField] private FieldRockTarget _target;

        public Material Material => _renderer.sharedMaterial;
        public FieldRockTarget Target => _target;
        // The diameter the rock's mesh stays within as it stands in the world.
        public float BoundingDiameter => 2f * MESH_REACH * Mathf.Max(transform.lossyScale.x, Mathf.Max(transform.lossyScale.y, transform.lossyScale.z));

        public void SetMaterial(Material material) =>
            _renderer.sharedMaterial = material;
    }
}
