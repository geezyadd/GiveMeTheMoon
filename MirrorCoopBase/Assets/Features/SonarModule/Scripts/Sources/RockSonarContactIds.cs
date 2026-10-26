using Features.FlightFieldModule.Contracts;
using UnityEngine;

namespace Features.SonarModule.Scripts.Sources {
    // The sonar ids of the field's rocks, the same on every peer: a large rock by its field rock id, a dense patch of
    // small rocks by its debris cell on the world grid.
    public sealed class RockSonarContactIds : IRockSonarContactIds {
        private const string CLUSTER_ROCK_FORMAT = "rock:c:{0}:{1}";
        private const string DEBRIS_ROCK_FORMAT = "rock:d:{0}:{1}:{2}";
        private const string DEBRIS_CELL_FORMAT = "debris:{0}:{1}";
        private const string ROCK_TAG = "rock";
        private const string DEBRIS_TAG = "debris";
        private const string CLUSTER_SOURCE_TAG = "c";
        private const string DEBRIS_SOURCE_TAG = "d";
        private const char SEPARATOR = ':';
        private const int CLUSTER_ROCK_PARTS = 4;
        private const int DEBRIS_ROCK_PARTS = 5;
        private const int DEBRIS_CELL_PARTS = 3;

        public string CreateRockId(FieldRockId rockId) =>
            rockId.Source == FieldRockSource.Cluster
                ? string.Format(CLUSTER_ROCK_FORMAT, rockId.PlacementIndex, rockId.RockIndex)
                : string.Format(DEBRIS_ROCK_FORMAT, rockId.DebrisCell.x, rockId.DebrisCell.y, rockId.RockIndex);

        public string CreateDebrisId(Vector2Int cell) =>
            string.Format(DEBRIS_CELL_FORMAT, cell.x, cell.y);

        public bool TryParseRockId(string contactId, out FieldRockId rockId) {
            rockId = default;
            string[] parts = contactId.Split(SEPARATOR);
            if (parts.Length < CLUSTER_ROCK_PARTS || parts[0] != ROCK_TAG)
                return false;

            if (parts[1] == CLUSTER_SOURCE_TAG)
                return TryParseClusterRock(parts, out rockId);

            return parts[1] == DEBRIS_SOURCE_TAG && TryParseDebrisRock(parts, out rockId);
        }

        public bool TryParseDebrisId(string contactId, out Vector2Int cell) {
            cell = Vector2Int.zero;
            string[] parts = contactId.Split(SEPARATOR);
            if (parts.Length != DEBRIS_CELL_PARTS || parts[0] != DEBRIS_TAG)
                return false;

            if (int.TryParse(parts[1], out int x) == false || int.TryParse(parts[2], out int y) == false)
                return false;

            cell = new Vector2Int(x, y);
            return true;
        }

        private static bool TryParseClusterRock(string[] parts, out FieldRockId rockId) {
            rockId = default;
            if (parts.Length != CLUSTER_ROCK_PARTS || int.TryParse(parts[2], out int placement) == false || int.TryParse(parts[3], out int rock) == false)
                return false;

            rockId = FieldRockId.CreateCluster(placement, rock);
            return true;
        }

        private static bool TryParseDebrisRock(string[] parts, out FieldRockId rockId) {
            rockId = default;
            if (parts.Length != DEBRIS_ROCK_PARTS || int.TryParse(parts[2], out int x) == false || int.TryParse(parts[3], out int y) == false)
                return false;

            if (int.TryParse(parts[4], out int rock) == false)
                return false;

            rockId = FieldRockId.CreateDebris(new Vector2Int(x, y), rock);
            return true;
        }
    }
}
