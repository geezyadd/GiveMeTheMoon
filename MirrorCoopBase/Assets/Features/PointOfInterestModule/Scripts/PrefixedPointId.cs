using System;
using System.Globalization;

namespace Features.PointOfInterestModule.Scripts {
    // Ids like "poi:3" or "nest-flock:3": a fixed prefix and a point id, parsed without allocating.
    internal static class PrefixedPointId {
        public static string Create(string prefix, int pointId) =>
            prefix + pointId.ToString(CultureInfo.InvariantCulture);

        public static bool TryParse(string id, string prefix, out int pointId) {
            pointId = 0;
            return id != null && id.StartsWith(prefix, StringComparison.Ordinal)
                && int.TryParse(id.AsSpan(prefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out pointId);
        }
    }
}
