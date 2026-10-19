using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Beacons {
    public interface IFieldBeaconSceneBuilder {
        public Transform CreateRoot();
        // Builds one beacon under the root (posed like the field root).
        public void Build(Transform root, int index, FieldBeacon beacon);
        public void Release(int index);
        public void ReleaseAll();
        // Sets the brightness of both colours for the moment of the blink.
        public void ApplyBlink(float time);
    }
}
