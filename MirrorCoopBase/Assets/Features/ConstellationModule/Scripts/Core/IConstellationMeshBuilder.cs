using Features.ConstellationModule.Scripts.Definitions;
using UnityEngine;

namespace Features.ConstellationModule.Scripts.Core {
    public interface IConstellationMeshBuilder {
        public Mesh Build(ConstellationDefinition constellation);
    }
}
