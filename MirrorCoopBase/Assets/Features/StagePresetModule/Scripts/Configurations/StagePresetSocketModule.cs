using System;
using Features.ShipModule.Scripts;
using UnityEngine;

namespace Features.StagePresetModule.Scripts.Configurations {
    [Serializable]
    public sealed class StagePresetSocketModule {
        [Tooltip("A ShipSocket id of ShipPlatform, e.g. engine-left.")]
        [SerializeField] private string _socketId;
        [Tooltip("A module of the ModuleCatalog whose slot type the socket accepts.")]
        [SerializeField] private ModuleDefinition _module;

        public string SocketId => _socketId;
        public ModuleDefinition Module => _module;
    }
}
