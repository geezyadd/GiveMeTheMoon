using UnityEngine;

namespace Features.EmoteModule {
    // The wheel's geometry, pure: the circle is cut into equal sectors, the first one centred at the top, the next ones
    // clockwise; a pointer inside the dead zone around the centre picks none (a cancel).
    public static class EmoteWheelRule {
        public const int NoSector = -1;
        private const float FullTurnDegrees = 360f;

        // The sector the pointer (an offset from the wheel's centre, y up) is in; NoSector inside the dead zone or
        // without sectors.
        public static int PickSector(Vector2 pointer, int sectorCount, float deadZoneRadius) {
            if (sectorCount <= 0 || pointer.magnitude < deadZoneRadius || pointer == Vector2.zero)
                return NoSector;

            float sectorDegrees = FullTurnDegrees / sectorCount;
            float clockwiseFromTop = Mathf.Repeat(Mathf.Atan2(pointer.x, pointer.y) * Mathf.Rad2Deg, FullTurnDegrees);
            int index = Mathf.FloorToInt(Mathf.Repeat(clockwiseFromTop + sectorDegrees * 0.5f, FullTurnDegrees) / sectorDegrees);
            return Mathf.Min(index, sectorCount - 1);
        }

        // The angle of a sector's middle, degrees clockwise from the top.
        public static float SectorCenterDegrees(int index, int sectorCount) =>
            sectorCount > 0 ? index * FullTurnDegrees / sectorCount : 0f;

        // The direction of a sector's middle from the centre, y up.
        public static Vector2 SectorDirection(int index, int sectorCount) {
            float radians = SectorCenterDegrees(index, sectorCount) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
        }

        // The pointer after a mouse move, kept within the wheel's radius so a long move does not have to be undone.
        public static Vector2 MovePointer(Vector2 pointer, Vector2 delta, float radius) =>
            Vector2.ClampMagnitude(pointer + delta, radius);
    }
}
