using System.Globalization;

namespace Features.PointOfInterestModule.Scripts.Content {
    public sealed class BugNestFlockTargetIds : IBugNestFlockTargetIds {
        private const string PREFIX = "nest-flock:";

        public string GetTargetId(int pointId) =>
            PREFIX + pointId.ToString(CultureInfo.InvariantCulture);

        public bool TryGetPointId(string targetId, out int pointId) {
            pointId = 0;
            return targetId != null && targetId.StartsWith(PREFIX)
                && int.TryParse(targetId.Substring(PREFIX.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out pointId);
        }
    }
}
