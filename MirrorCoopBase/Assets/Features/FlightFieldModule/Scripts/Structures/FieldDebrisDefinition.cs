using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Structures {
    // One kind of small debris over passable cells (rocks now; wreckage or mines later): a passable cell stores the
    // id and a density, and every peer builds the pieces locally from the recipe. A new kind is a new asset.
    [CreateAssetMenu(menuName = "Game/Flight Field/Debris Definition", fileName = "FieldDebrisDefinition")]
    public sealed class FieldDebrisDefinition : ScriptableObject {
        [Tooltip("Stable id stored in debris cells and sent over the network: never rename it once it is used.")]
        [SerializeField] private string _id;
        [SerializeField] private FieldDebrisRecipe _recipe;

        public string Id => _id;
        public FieldDebrisRecipe Recipe => _recipe;
    }
}
