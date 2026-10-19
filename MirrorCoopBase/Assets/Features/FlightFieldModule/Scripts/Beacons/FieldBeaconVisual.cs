using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Beacons {
    // The root of the beacon prefab: its light parts take the colour material of their lane, so all beacons of one
    // colour share one material, blink together and batch.
    public sealed class FieldBeaconVisual : MonoBehaviour {
        [SerializeField] private Renderer[] _lights;

        // The material the colour materials are made from.
        public Material LightMaterial => _lights[0].sharedMaterial;

        public void SetMaterial(Material material) {
            foreach (Renderer lightRenderer in _lights)
                lightRenderer.sharedMaterial = material;
        }
    }
}
