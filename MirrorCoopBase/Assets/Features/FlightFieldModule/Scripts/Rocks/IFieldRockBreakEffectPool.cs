using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Rocks {
    public interface IFieldRockBreakEffectPool {
        // Plays a free (or new) break effect under the parent at a world position.
        public void Play(Transform parent, Vector3 position, float diameter, Material rockMaterial);
    }
}
