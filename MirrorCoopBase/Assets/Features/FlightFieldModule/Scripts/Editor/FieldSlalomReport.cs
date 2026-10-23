using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor {
    // The slalom numbers of a field: the way a ship of the configured width (plus the gap margin) flies from the cruise
    // start to B between the debris rocks and the walls, how long it is against the straight line, the smallest gap
    // along it, how dense the rocks of the main channels are, and for an obstacle field the rocks in the flight plane
    // per square kilometer (debris and giants), their sizes and the largest one.
    public sealed class FieldSlalomReport {
        private const float METERS_PER_KILOMETER = 1000f;
        private const float SQUARE_METERS_PER_SQUARE_KILOMETER = 1000000f;

        public bool IsPathFound { get; }
        // Field plane points (x across, y along), from the cruise start to B; empty when no way was found.
        public IReadOnlyList<Vector2> Path { get; }
        public IReadOnlyList<FieldSlalomRock> Rocks { get; }
        public float PathLengthMeters { get; }
        public float StraightLengthMeters { get; }
        // Twice the smallest distance from the path to a rock or a wall: the narrowest gap the ship goes through.
        public float MinGapMeters { get; }
        public int ChannelRocks { get; }
        public float ChannelLengthMeters { get; }
        public FieldSlalomPlane Plane { get; }
        public float PathToStraightRatio => StraightLengthMeters > 0f ? PathLengthMeters / StraightLengthMeters : 0f;
        public float RocksPerKilometer => ChannelLengthMeters > 0f ? ChannelRocks / (ChannelLengthMeters / METERS_PER_KILOMETER) : 0f;

        public FieldSlalomReport(bool isPathFound, IReadOnlyList<Vector2> path, IReadOnlyList<FieldSlalomRock> rocks, float pathLengthMeters, float straightLengthMeters,
                                 float minGapMeters, int channelRocks, float channelLengthMeters, FieldSlalomPlane plane) {
            IsPathFound = isPathFound;
            Path = path;
            Rocks = rocks;
            PathLengthMeters = pathLengthMeters;
            StraightLengthMeters = straightLengthMeters;
            MinGapMeters = minGapMeters;
            ChannelRocks = channelRocks;
            ChannelLengthMeters = channelLengthMeters;
            Plane = plane;
        }

        public float PlaneRocksPerSquareKilometer() {
            int planeRocks = Plane.GiantDiametersMeters.Count;
            foreach (FieldSlalomRock rock in Rocks) {
                if (rock.IsInPlane)
                    planeRocks++;
            }

            return planeRocks / (Plane.AreaSquareMeters / SQUARE_METERS_PER_SQUARE_KILOMETER);
        }

        public float LargestRockMeters() {
            float largest = 0f;
            foreach (FieldSlalomRock rock in Rocks)
                largest = Mathf.Max(largest, rock.Radius * 2f);

            foreach (float diameter in Plane.GiantDiametersMeters)
                largest = Mathf.Max(largest, diameter);

            return largest;
        }

        // Rocks (debris and giants) per diameter class: class i holds the diameters from limit i - 1 up to limit i,
        // the last class everything above the last limit.
        public int[] CountBySize(IReadOnlyList<float> limitsMeters) {
            int[] counts = new int[limitsMeters.Count + 1];
            foreach (FieldSlalomRock rock in Rocks)
                counts[SizeClassOf(rock.Radius * 2f, limitsMeters)]++;

            foreach (float diameter in Plane.GiantDiametersMeters)
                counts[SizeClassOf(diameter, limitsMeters)]++;

            return counts;
        }

        private static int SizeClassOf(float diameter, IReadOnlyList<float> limitsMeters) {
            for (int i = 0; i < limitsMeters.Count; i++) {
                if (diameter <= limitsMeters[i])
                    return i;
            }

            return limitsMeters.Count;
        }
    }
}
