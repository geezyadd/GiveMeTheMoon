#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Globalization;
using Features.FlightFieldModule.Contracts;
using UnityEngine;

namespace Tests.Coop.Shared {
    // A field rock's id in a host command: source, placement index, debris cell x and y, rock index, separated by spaces.
    // The id is the same on every peer, so the client finds the rock the host picked in its own published field.
    public static class FieldRockIdText {
        public const int PART_COUNT = 5;
        private const string FORMAT = "{0} {1} {2} {3} {4}";

        public static string Format(FieldRockId id) =>
            string.Format(CultureInfo.InvariantCulture, FORMAT, (int)id.Source, id.PlacementIndex, id.DebrisCell.x, id.DebrisCell.y, id.RockIndex);

        public static FieldRockId Parse(string[] parts, int first) =>
            new FieldRockId(
                (FieldRockSource)ReadInt(parts[first]),
                ReadInt(parts[first + 1]),
                new Vector2Int(ReadInt(parts[first + 2]), ReadInt(parts[first + 3])),
                ReadInt(parts[first + 4]));

        private static int ReadInt(string text) =>
            int.Parse(text, CultureInfo.InvariantCulture);
    }
}
#endif
