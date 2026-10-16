using UnityEngine;

namespace Features.FloatingControllerModule {
    // The one hover spring formula: the station float (FloatingController) and the ship ride both run it.
    public static class HoverSpring {
        // A support this far below the probe start holds the body.
        public static bool Reaches(in HoverSpringSettings settings, float supportDistance) =>
            supportDistance <= settings.SupportReach;

        // A body falling faster than this lands on its collider first, the spring only catches it afterwards.
        public static bool Holds(in HoverSpringSettings settings, float verticalSpeed) =>
            verticalSpeed >= settings.SkipIfFallingFasterThan;

        // Upward acceleration on top of the gravity compensation. closingSpeed is the vertical speed relative to the support.
        public static float Acceleration(in HoverSpringSettings settings, float supportDistance, float closingSpeed) {
            float heightError = settings.Clearance - supportDistance;
            float magnitude = heightError * settings.Stiffness - closingSpeed * settings.Damping;
            return Mathf.Clamp(magnitude, -settings.MaxAcceleration, settings.MaxAcceleration);
        }
    }
}
