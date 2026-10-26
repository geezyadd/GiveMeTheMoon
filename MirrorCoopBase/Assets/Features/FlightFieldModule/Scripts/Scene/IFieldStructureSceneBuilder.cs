using Features.FlightFieldModule.Scripts.Network.Generated;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Scene {
    public interface IFieldStructureSceneBuilder {
        public Transform CreateRoot();
        public void PlaceRoot(Transform root, IReadOnlyLiveFieldModel liveFieldModel);
        // False when the placement is not built locally (a networked structure comes through Mirror).
        public bool BuildLocalStructure(Transform root, IReadOnlyLiveFieldModel liveFieldModel, int placementIndex);
        // A rock of a placement built on this peer (hidden when broken); false when the placement is not built here.
        public bool TryGetBuiltRock(int placementIndex, int rockIndex, out FieldRockVisual rock);
        // Returns the objects built for the placement to the pool; nothing happens when none were built.
        public void ReleaseLocalStructure(int placementIndex);
        public void ReleaseAll();
    }
}
