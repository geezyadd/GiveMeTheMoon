using UnityEngine;

namespace Features.ConstellationModule.Scripts.Configurations {
    // How a skybox's painted sky lies in the Unity world. A catalogue position (RA a, Dec d) is first written in Unity's
    // axis order (x = cos d cos a, y = sin d, z = cos d sin a; this swap turns the right-handed celestial frame into
    // Unity's left-handed one), then turned by this rotation into a world direction.
    [CreateAssetMenu(fileName = nameof(CelestialSkyFrameConfiguration), menuName = "Configurations/Constellation/" + nameof(CelestialSkyFrameConfiguration))]
    public sealed class CelestialSkyFrameConfiguration : ScriptableObject {
        [Tooltip("Euler angles (degrees) of the rotation from the celestial frame, in Unity's axis order, to the world. Found by plate solving renders of the skybox.")]
        [SerializeField] private Vector3 _celestialToWorldEuler;

        public Quaternion CelestialToWorld => Quaternion.Euler(_celestialToWorldEuler);
    }
}
