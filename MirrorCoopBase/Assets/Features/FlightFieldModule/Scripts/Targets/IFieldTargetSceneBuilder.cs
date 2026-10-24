using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Network.Generated;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Targets {
    // Builds this peer's target balloons from the live field under their own root, from their own pool.
    public interface IFieldTargetSceneBuilder {
        public Transform CreateRoot();
        public void PlaceRoot(Transform root, IReadOnlyLiveFieldModel liveFieldModel);
        public FieldTargetView Build(Transform root, FieldTargetEntry target, int index);
        public void Release(FieldTargetView view);
    }
}
