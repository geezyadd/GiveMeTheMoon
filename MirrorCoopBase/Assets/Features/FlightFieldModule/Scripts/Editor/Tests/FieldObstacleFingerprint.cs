using System;
using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // A stable FNV-1a hash of everything a generated field holds: the seed and profile, every cell, the placements,
    // the routes and the target balloons. Floats go in by their bits, so any change of the generation changes it.
    public static class FieldObstacleFingerprint {
        private const ulong OFFSET_BASIS = 14695981039346656037UL;
        private const ulong PRIME = 1099511628211UL;
        // Stands for a missing string (no debris kind): no real string adds a negative length.
        private const int NULL_STRING_MARK = -1;

        public static ulong Create(FlightField field) {
            ulong hash = OFFSET_BASIS;
            hash = Add(hash, field.Seed);
            hash = Add(hash, field.ProfileId);
            hash = AddCells(hash, field);
            hash = AddPlacements(hash, field);
            hash = AddRoutes(hash, field);
            return AddTargets(hash, field);
        }

        private static ulong AddCells(ulong hash, FlightField field) {
            foreach (Vector2Int index in field.Cells.GetIndexes()) {
                hash = Add(hash, index);
                hash = AddCell(hash, field.Cells[index]);
            }

            return hash;
        }

        private static ulong AddCell(ulong hash, FieldCell cell) {
            hash = Add(hash, (int)cell.Reserve);
            hash = Add(hash, (int)cell.Class);
            hash = Add(hash, cell.PlacementId);
            hash = Add(hash, cell.DebrisDensity);
            return Add(hash, cell.DebrisId);
        }

        private static ulong AddPlacements(ulong hash, FlightField field) {
            hash = Add(hash, field.Placements.Count);
            foreach (FieldPlacement placement in field.Placements) {
                hash = Add(hash, placement.Id);
                hash = Add(hash, placement.StructureId);
                hash = Add(hash, placement.Anchor);
                hash = Add(hash, (int)placement.Rotation);
            }

            return hash;
        }

        private static ulong AddRoutes(ulong hash, FlightField field) {
            hash = Add(hash, field.Routes.Count);
            foreach (FieldRoute route in field.Routes) {
                hash = Add(hash, (int)route.Kind);
                hash = Add(hash, route.Difficulty);
                foreach (Vector2Int cell in route.Centerline)
                    hash = Add(hash, cell);
                foreach (int halfWidth in route.HalfWidths)
                    hash = Add(hash, halfWidth);
            }

            return hash;
        }

        private static ulong AddTargets(ulong hash, FlightField field) {
            hash = Add(hash, field.Targets.Count);
            foreach (FieldTargetPlacement target in field.Targets) {
                hash = Add(hash, target.KindId);
                hash = Add(hash, target.FieldPosition.x);
                hash = Add(hash, target.FieldPosition.y);
                hash = Add(hash, target.FieldPosition.z);
            }

            return hash;
        }

        private static ulong Add(ulong hash, Vector2Int cell) =>
            Add(Add(hash, cell.x), cell.y);

        private static ulong Add(ulong hash, float value) =>
            Add(hash, BitConverter.SingleToInt32Bits(value));

        private static ulong Add(ulong hash, string value) {
            if (value == null)
                return Add(hash, NULL_STRING_MARK);

            foreach (char character in value)
                hash = Add(hash, character);

            return Add(hash, value.Length);
        }

        private static ulong Add(ulong hash, int value) {
            for (int shift = 0; shift < 32; shift += 8)
                hash = unchecked((hash ^ (byte)(value >> shift)) * PRIME);

            return hash;
        }
    }
}
