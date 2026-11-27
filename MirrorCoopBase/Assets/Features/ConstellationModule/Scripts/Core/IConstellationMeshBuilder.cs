using System.Collections.Generic;
using Features.ConstellationModule.Scripts.Definitions;
using UnityEngine;

namespace Features.ConstellationModule.Scripts.Core {
    public interface IConstellationMeshBuilder {
        public Mesh Build(IReadOnlyList<ConstellationDefinition> constellations);
    }
}
