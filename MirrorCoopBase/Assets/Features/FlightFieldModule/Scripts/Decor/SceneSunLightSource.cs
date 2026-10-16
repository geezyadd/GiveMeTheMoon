using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Decor {
    // The direction the scene's main light travels in, from the sun of the scene's lighting settings.
    public sealed class SceneSunLightSource : IFieldDecorLightSource {
        public bool TryGetLightTravel(out Vector3 direction) {
            direction = Vector3.zero;
            Light sun = RenderSettings.sun;
            if (sun == null)
                return false;

            direction = sun.transform.forward;
            return true;
        }
    }
}
