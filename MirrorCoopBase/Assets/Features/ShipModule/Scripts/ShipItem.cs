using UnityEngine;

namespace Features.ShipModule.Scripts {
    public sealed class ShipItem : MonoBehaviour {
        [SerializeField] private ModuleDefinition _module;

        public ModuleDefinition Module => _module;
    }
}
