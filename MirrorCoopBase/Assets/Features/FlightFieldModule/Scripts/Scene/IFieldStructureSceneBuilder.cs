using Features.FlightFieldModule.Scripts.Network.Generated;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Scene {
    public interface IFieldStructureSceneBuilder {
        public Transform CreateRoot();
        public void PlaceRoot(Transform root, IReadOnlyLiveFieldModel liveFieldModel);
        public bool TryBuild(Transform root, IReadOnlyLiveFieldModel liveFieldModel, int placementIndex);
        public void ReleaseAll();
    }
}
