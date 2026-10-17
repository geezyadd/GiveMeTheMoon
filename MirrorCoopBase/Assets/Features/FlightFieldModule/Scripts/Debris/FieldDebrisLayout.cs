using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Debris {
    // Lays out the rocks of passable cells from a jittered grid over the whole field plane: a grid point anywhere in
    // its grid square holds a rock with the density of the cell it lands in, small or big by the cell's recipe, unless
    // it would come closer than the wall clearance to a closed cell or reach into an empty one. A point that comes
    // closer than the ship gap to a kept neighbour with a higher priority gives way (sequential Matern thinning: a
    // neighbour that gave way itself does not push anyone out), so any two rocks of the field leave the ship room and
    // nothing clumps. A point depends only on the field seed, its grid coordinates and the cells around it, so every
    // peer gets the same rocks whatever order the cells are built in.
    public sealed class FieldDebrisLayout : IFieldDebrisLayout {
        private const float HALF = 0.5f;
        private const uint POINT_X_PRIME = 0x8DA6B343u;
        private const uint POINT_Y_PRIME = 0xD8163841u;
        private const uint LOOK_SALT = 0xCB1AB31Fu;
        private const float CELL_CENTER = 0.5f;

        private readonly FlightFieldConfiguration _flightFieldConfiguration;
        private readonly FieldStructureCatalog _fieldStructureCatalog;

        public FieldDebrisLayout(FlightFieldConfiguration flightFieldConfiguration, FieldStructureCatalog fieldStructureCatalog) {
            _flightFieldConfiguration = flightFieldConfiguration;
            _fieldStructureCatalog = fieldStructureCatalog;
        }

        public void Fill(int fieldSeed, Vector2Int cell, float cellSize, IFieldDebrisCells cells, FieldDebrisWorkspace workspace, List<FieldDebrisSpot> spots) {
            spots.Clear();
            float spacing = _flightFieldConfiguration.DebrisGridSpacingMeters;
            Vector2 cellMin = new(cell.y * cellSize, cell.x * cellSize);
            Vector2 cellCenter = cellMin + Vector2.one * (cellSize * CELL_CENTER);
            Vector2Int first = new(Mathf.FloorToInt(cellMin.x / spacing), Mathf.FloorToInt(cellMin.y / spacing));
            Vector2Int last = new(Mathf.FloorToInt((cellMin.x + cellSize) / spacing), Mathf.FloorToInt((cellMin.y + cellSize) / spacing));
            DebrisLayoutFrame frame = new(fieldSeed, cellSize, cells, workspace, LargestRadius());
            for (int across = first.x; across <= last.x; across++) {
                for (int along = first.y; along <= last.y; along++)
                    TryAddSpot(frame, new Vector2Int(across, along), cell, cellCenter, spots);
            }
        }

        private void TryAddSpot(DebrisLayoutFrame frame, Vector2Int point, Vector2Int cell, Vector2 cellCenter, List<FieldDebrisSpot> spots) {
            FieldDebrisCandidate candidate = Get(frame, point);
            bool isHere = candidate.Exists && ToCell(candidate.Position, frame.CellSize) == cell;
            if (!isHere || !IsKept(frame, point, candidate))
                return;

            Vector2 offset = candidate.Position - cellCenter;
            spots.Add(new FieldDebrisSpot(new Vector3(offset.x, candidate.Height, offset.y), candidate.Diameter, candidate.LookSeed));
        }

        // Kept unless a kept neighbour with a higher priority stands closer than the ship gap. The chain of higher
        // priorities ends, so the answer is the same whoever asks first; it is remembered per point. The chain is
        // walked with the workspace's stack, not by recursion: it can be hundreds of points long.
        private bool IsKept(DebrisLayoutFrame frame, Vector2Int point, FieldDebrisCandidate candidate) {
            if (!candidate.Exists)
                return false;

            Stack<Vector2Int> pending = frame.Workspace.Pending;
            pending.Clear();
            pending.Push(point);
            while (pending.Count > 0)
                DecideTop(frame, pending);

            frame.Workspace.TryGetKept(point, out bool isKept);
            return isKept;
        }

        // Decides the top point once its rivals are decided; until then its first undecided rival goes on top.
        private void DecideTop(DebrisLayoutFrame frame, Stack<Vector2Int> pending) {
            Vector2Int point = pending.Peek();
            if (frame.Workspace.TryGetKept(point, out bool _)) {
                pending.Pop();
                return;
            }

            FieldDebrisRivalState state = ReadRivals(frame, point, Get(frame, point), out Vector2Int undecided);
            if (state == FieldDebrisRivalState.Undecided) {
                pending.Push(undecided);
                return;
            }

            frame.Workspace.RememberKept(point, state == FieldDebrisRivalState.Free);
            pending.Pop();
        }

        private FieldDebrisRivalState ReadRivals(DebrisLayoutFrame frame, Vector2Int point, FieldDebrisCandidate candidate, out Vector2Int undecided) {
            float gap = _flightFieldConfiguration.DebrisMinGapMeters;
            int reach = Mathf.CeilToInt((candidate.Radius + frame.LargestRadius + gap) / _flightFieldConfiguration.DebrisGridSpacingMeters);
            FieldDebrisRivalState state = FieldDebrisRivalState.Free;
            undecided = point;
            for (int across = point.x - reach; across <= point.x + reach; across++) {
                for (int along = point.y - reach; along <= point.y + reach; along++) {
                    Vector2Int other = new(across, along);
                    if (other == point || !IsRival(Get(frame, other), candidate, gap))
                        continue;

                    if (!frame.Workspace.TryGetKept(other, out bool isOtherKept)) {
                        state = FieldDebrisRivalState.Undecided;
                        undecided = other;
                    }
                    else if (isOtherKept)
                        return FieldDebrisRivalState.PushedOut;
                }
            }

            return state;
        }

        private static bool IsRival(FieldDebrisCandidate neighbour, FieldDebrisCandidate candidate, float gap) {
            // A strict order (exact priority, then look seed): an approximate tie could make two points rivals of each other.
            bool isHigher = neighbour.Priority > candidate.Priority || neighbour.Priority == candidate.Priority && neighbour.LookSeed > candidate.LookSeed;
            return neighbour.Exists && isHigher && Vector2.Distance(neighbour.Position, candidate.Position) < neighbour.Radius + candidate.Radius + gap;
        }

        private FieldDebrisCandidate Get(DebrisLayoutFrame frame, Vector2Int point) {
            if (frame.Workspace.TryGet(point, out FieldDebrisCandidate cached))
                return cached;

            FieldDebrisCandidate candidate = Draw(frame, point);
            frame.Workspace.Remember(point, candidate);
            return candidate;
        }

        // The draws come in a fixed order, so the same point gives the same candidate whoever asks.
        private FieldDebrisCandidate Draw(DebrisLayoutFrame frame, Vector2Int point) {
            int seed = PointSeed(frame.FieldSeed, point);
            frame.Workspace.Random.Reseed(seed);
            IFieldRandom random = frame.Workspace.Random;
            Vector2 position = (point + new Vector2(random.Value(), random.Value())) * _flightFieldConfiguration.DebrisGridSpacingMeters;
            float presence = random.Value();
            float priority = random.Value();
            float bigRoll = random.Value();
            float sizeRoll = random.Value();
            float offPlaneRoll = random.Value();
            float heightRoll = random.Value();
            int lookSeed = unchecked((int)((uint)seed ^ LOOK_SALT));
            if (!frame.Cells.TryGetDebris(ToCell(position, frame.CellSize), out float density, out FieldDebrisRecipe recipe))
                return new FieldDebrisCandidate(position, 0f, 0f, priority, false, lookSeed);

            Vector2 range = bigRoll < recipe.BigShare ? recipe.BigDiameterMeters : recipe.SmallDiameterMeters;
            float diameter = Mathf.Lerp(range.x, range.y, sizeRoll);
            float height = GetHeight(recipe, diameter * HALF, offPlaneRoll, heightRoll);
            bool exists = presence < density && IsClearOfWalls(frame, position, diameter * HALF);
            return new FieldDebrisCandidate(position, height, diameter, priority, exists, lookSeed);
        }

        // In the plane the middle stays within the cross share of the radius; off the plane the surface stands clear.
        private static float GetHeight(FieldDebrisRecipe recipe, float radius, float offPlaneRoll, float heightRoll) {
            float side = heightRoll < HALF ? -1f : 1f;
            if (offPlaneRoll >= recipe.OffPlaneShare)
                return Mathf.Lerp(-recipe.CrossShare, recipe.CrossShare, heightRoll) * radius;

            float lift = Mathf.Lerp(recipe.OffPlaneHeightMeters.x, recipe.OffPlaneHeightMeters.y, Mathf.Repeat(heightRoll * 2f, 1f));
            return side * (radius + lift);
        }

        // No closed cell within the radius and the wall clearance, no empty cell (station zone, takeoff corridor, open
        // space, outside the field) under the rock.
        private bool IsClearOfWalls(DebrisLayoutFrame frame, Vector2 position, float radius) {
            float wallClearance = radius + _flightFieldConfiguration.DebrisWallClearanceMeters;
            Vector2Int center = ToCell(position, frame.CellSize);
            int reach = Mathf.CeilToInt(Mathf.Max(wallClearance, radius) / frame.CellSize);
            for (int along = center.x - reach; along <= center.x + reach; along++) {
                for (int across = center.y - reach; across <= center.y + reach; across++) {
                    Vector2Int cell = new(along, across);
                    float distance = DistanceToCell(position, cell, frame.CellSize);
                    bool isTooClose = frame.Cells.IsBlocked(cell) ? distance < wallClearance : distance < radius && frame.Cells.IsEmpty(cell);
                    if (isTooClose)
                        return false;
                }
            }

            return true;
        }

        private float LargestRadius() {
            float largest = 0f;
            foreach (FieldDebrisDefinition debris in _fieldStructureCatalog.Debris)
                largest = Mathf.Max(largest, Mathf.Max(debris.Recipe.BigDiameterMeters.y, debris.Recipe.SmallDiameterMeters.y) * HALF);

            return largest;
        }

        // Field plane: x across, y along; the cell is the matrix one (x along, y across).
        private static float DistanceToCell(Vector2 position, Vector2Int cell, float cellSize) {
            Vector2 min = new(cell.y * cellSize, cell.x * cellSize);
            Vector2 nearest = new(Mathf.Clamp(position.x, min.x, min.x + cellSize), Mathf.Clamp(position.y, min.y, min.y + cellSize));
            return Vector2.Distance(position, nearest);
        }

        private static Vector2Int ToCell(Vector2 position, float cellSize) =>
            new(Mathf.FloorToInt(position.y / cellSize), Mathf.FloorToInt(position.x / cellSize));

        private static int PointSeed(int fieldSeed, Vector2Int point) =>
            unchecked((int)((uint)fieldSeed ^ (uint)point.x * POINT_X_PRIME ^ (uint)point.y * POINT_Y_PRIME));
    }
}
