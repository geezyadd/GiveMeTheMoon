using Features.FlightFieldModule.Scripts.Network;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Ore {
    public interface IFieldOreSceneBuilder {
        public Transform CreateRoot();
        public FieldOreView Build(Transform root, in FieldOreEntry entry);
        public void Release(FieldOreView view);
    }
}
