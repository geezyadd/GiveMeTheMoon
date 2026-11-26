using UnityEngine;

namespace Features.WeaponModule.Scripts.Parts.Delivery {
    // An aim turned by a random angle inside a spread cone.
    public static class WeaponSpread {
        private const float FULL_TURN_RADIANS = 2f * Mathf.PI;
        // Above this |y| the aim is close to vertical and the world up would give a degenerate cross product.
        private const float NEAR_VERTICAL_AIM_Y = 0.9f;

        // The square root spreads the shots evenly over the cone's cross-section instead of crowding its middle.
        public static Vector3 Turn(Vector3 aim, float spreadDegrees, IWeaponRandom random) {
            float angle = spreadDegrees * Mathf.Deg2Rad * Mathf.Sqrt(random.NextUnitValue());
            float azimuth = FULL_TURN_RADIANS * random.NextUnitValue();
            Vector3 side = Vector3.Cross(aim, PickHelperAxis(aim)).normalized;
            Vector3 up = Vector3.Cross(side, aim);
            Vector3 offset = side * Mathf.Cos(azimuth) + up * Mathf.Sin(azimuth);
            return (aim * Mathf.Cos(angle) + offset * Mathf.Sin(angle)).normalized;
        }

        private static Vector3 PickHelperAxis(Vector3 aim) =>
            Mathf.Abs(aim.y) < NEAR_VERTICAL_AIM_Y ? Vector3.up : Vector3.right;
    }
}
