using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Debris;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Scene;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // Reads rocks from the field data through the same layouts the peers build them with (IFieldPlacementRockLayout,
    // IFieldDebrisLayout), so a rock's id, middle and diameter match the rock every peer shows. Field space: x across
    // the field, y up from the flight plane, z along it; the frame turns it into the world.
    public sealed class FieldRockReader : IFieldRockReader {
        // Reused per thread: the server's queries run every physics step and allocate nothing, and the generation
        // thread reads rocks too (the target balloon step), at the same time as the main thread or another generation.
        [ThreadStatic] private static List<FieldClusterRockPose> _clusterRocks;
        [ThreadStatic] private static List<FieldDebrisSpot> _spots;

        private readonly FieldStructureCatalog _fieldStructureCatalog;
        private readonly IFieldStructureSpawnRule _fieldStructureSpawnRule;
        private readonly IFieldFootprintReader _fieldFootprintReader;
        private readonly IFieldStructureLayout _fieldStructureLayout;
        private readonly IFieldPlacementRockLayout _fieldPlacementRockLayout;
        private readonly IFieldDebrisLayout _fieldDebrisLayout;
        private readonly IFieldRockClassifier _fieldRockClassifier;

        private static List<FieldClusterRockPose> ClusterRocks => _clusterRocks ??= new List<FieldClusterRockPose>();
        private static List<FieldDebrisSpot> Spots => _spots ??= new List<FieldDebrisSpot>();

        public FieldRockReader(FieldStructureCatalog fieldStructureCatalog, IFieldStructureSpawnRule fieldStructureSpawnRule, IFieldFootprintReader fieldFootprintReader,
                               IFieldStructureLayout fieldStructureLayout, IFieldPlacementRockLayout fieldPlacementRockLayout, IFieldDebrisLayout fieldDebrisLayout,
                               IFieldRockClassifier fieldRockClassifier) {
            _fieldStructureCatalog = fieldStructureCatalog;
            _fieldStructureSpawnRule = fieldStructureSpawnRule;
            _fieldFootprintReader = fieldFootprintReader;
            _fieldStructureLayout = fieldStructureLayout;
            _fieldPlacementRockLayout = fieldPlacementRockLayout;
            _fieldDebrisLayout = fieldDebrisLayout;
            _fieldRockClassifier = fieldRockClassifier;
        }

        public void ReadPlacement(FieldRockFieldData fieldData, FieldFrame frame, int placementIndex, List<FieldRockDescription> rocks) {
            rocks.Clear();
            FieldPlacement placement = fieldData.Field.Placements[placementIndex];
            FieldStructureDefinition structure = _fieldStructureCatalog.GetRequired(placement.StructureId);
            if (!_fieldStructureSpawnRule.IsBuiltLocally(structure))
                return;

            FieldFootprint footprint = _fieldFootprintReader.Read(structure, placement.Rotation);
            Vector3 position = _fieldStructureLayout.GetFieldPose(footprint, placement.Anchor, placement.Rotation, frame.CellSize).position;
            List<FieldClusterRockPose> clusterRocks = ClusterRocks;
            _fieldPlacementRockLayout.Fill(fieldData.Field.Seed, placementIndex, structure, footprint, frame.CellSize, clusterRocks);
            for (int rockIndex = 0; rockIndex < clusterRocks.Count; rockIndex++) {
                FieldClusterRockPose rock = clusterRocks[rockIndex];
                FieldClusterRecipe recipe = structure.ClusterRecipe;
                FieldRockSize size = _fieldRockClassifier.GetSize(rock.BoundingDiameter, recipe.IsAlwaysLarge);
                rocks.Add(new FieldRockDescription(FieldRockId.CreateCluster(placementIndex, rockIndex), structure.Id, ToWorld(frame, position + rock.Offset),
                    rock.BoundingDiameter, size, recipe.IsBreakable));
            }
        }

        public void ReadDebrisCell(FieldRockFieldData fieldData, FieldFrame frame, Vector2Int cell, List<FieldRockDescription> rocks) {
            rocks.Clear();
            if (!fieldData.DebrisCells.TryGetDebris(cell, out _, out FieldDebrisRecipe recipe))
                return;

            string debrisId = fieldData.Field.Cells[cell].DebrisId;
            List<FieldDebrisSpot> spots = Spots;
            _fieldDebrisLayout.Fill(fieldData.Field.Seed, cell, frame.CellSize, fieldData.DebrisCells, fieldData.Workspace, spots);
            Vector3 cellCenter = FieldFrame.CellCenterInField(cell, frame.CellSize);
            for (int rockIndex = 0; rockIndex < spots.Count; rockIndex++) {
                FieldDebrisSpot spot = spots[rockIndex];
                FieldRockSize size = _fieldRockClassifier.GetSize(spot.Diameter, false);
                rocks.Add(new FieldRockDescription(FieldRockId.CreateDebris(cell, rockIndex), debrisId, ToWorld(frame, cellCenter + spot.Offset), spot.Diameter, size,
                    recipe.IsBreakable));
            }
        }

        // The field root's pose: at the origin, turned to look along Forward.
        private static Vector3 ToWorld(FieldFrame frame, Vector3 fieldPoint) =>
            frame.Origin + Quaternion.LookRotation(frame.Forward, Vector3.up) * fieldPoint;
    }
}
