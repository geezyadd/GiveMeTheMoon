using UnityEngine;

namespace Features.ShipModule.Scripts {
    // The floating-origin state: how many shifts happened and their running total. Anything placed in the world keeps
    // the WorldShift it was placed under; the difference to the current one is the offset to add, however many shifts
    // ago that was. The total is kept in doubles so a long session does not lose precision.
    public struct WorldShift {
        public int Count;
        public double X;
        public double Y;
        public double Z;

        public WorldShift Add(Vector3 delta) =>
            new WorldShift { Count = Count + 1, X = X + delta.x, Y = Y + delta.y, Z = Z + delta.z };

        public Vector3 Since(WorldShift earlier) =>
            new Vector3((float)(X - earlier.X), (float)(Y - earlier.Y), (float)(Z - earlier.Z));
    }
}
