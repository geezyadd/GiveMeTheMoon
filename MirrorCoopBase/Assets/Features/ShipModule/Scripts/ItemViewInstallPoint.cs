using System;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    [Serializable]
    public sealed class ItemViewInstallPoint {
        [SerializeField] private ModuleDefinition _module;
        [SerializeField] private Transform _point;

        public ModuleDefinition Module => _module;
        public Transform Point => _point;
    }
}
