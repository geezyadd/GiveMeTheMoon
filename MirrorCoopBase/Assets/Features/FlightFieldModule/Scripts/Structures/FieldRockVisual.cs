using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Structures {
    // The root of a visual rock prefab: the cluster builder gives it one of the recipe's shared materials, so the
    // tint changes without a MaterialPropertyBlock and the SRP Batcher still batches every rock.
    public sealed class FieldRockVisual : MonoBehaviour {
        // How far a rock mesh, normalised to about 1 m, reaches from its pivot at most: every layout sizes rocks by it.
        public const float MESH_REACH = 0.7f;

        [SerializeField] private MeshRenderer _renderer;

        public void SetMaterial(Material material) =>
            _renderer.sharedMaterial = material;
    }
}
