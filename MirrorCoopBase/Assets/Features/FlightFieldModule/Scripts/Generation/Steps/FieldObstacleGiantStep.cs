using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation.Obstacles;
using Features.FlightFieldModule.Scripts.Generation.Placement;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Steps {
    // The giants of an obstacle field: the profile's giants per square kilometer, each drawn from a size class by
    // weight and an obstacle of that class by weight, laid biggest first at random spots on cells nothing keeps empty.
    // A spot is kept by the density map's chance and only with the profile's gap cells free of any other giant around
    // the footprint, so two giants never leave a gap the ship almost fits; a giant without a spot after its tries is
    // dropped. They are placements: synced, the same on every peer.
    public sealed class FieldObstacleGiantStep : IFieldGenerationStep {
        private const float SQUARE_METERS_PER_SQUARE_KILOMETER = 1000000f;

        private readonly IFieldStructurePlacer _fieldStructurePlacer;
        private readonly IFieldFootprintReader _fieldFootprintReader;
        private readonly IFieldObstacleDensity _fieldObstacleDensity;

        public FieldObstacleGiantStep(IFieldStructurePlacer fieldStructurePlacer, IFieldFootprintReader fieldFootprintReader, IFieldObstacleDensity fieldObstacleDensity) {
            _fieldStructurePlacer = fieldStructurePlacer;
            _fieldFootprintReader = fieldFootprintReader;
            _fieldObstacleDensity = fieldObstacleDensity;
        }

        public void Execute(FieldGenerationContext context) {
            if (context.Profile.GiantClasses.Count == 0)
                return;

            foreach ((FieldStructureDefinition structure, FieldFootprint footprint) in DrawGiants(context))
                TryLay(context, structure, footprint);
        }

        // Drawn before any is laid, so the count and the picks do not depend on where the earlier ones landed. Only the
        // giants that fit the frame are drawn: a short field (a test's short flight) would drop every massive one.
        private List<(FieldStructureDefinition structure, FieldFootprint footprint)> DrawGiants(FieldGenerationContext context) {
            FieldFrame frame = context.Frame;
            float squareKilometers = frame.LengthCells * frame.WidthCells * frame.CellSize * frame.CellSize / SQUARE_METERS_PER_SQUARE_KILOMETER;
            int count = Mathf.RoundToInt(context.Profile.GiantsPerSquareKilometer.Evaluate(context.Difficulty) * squareKilometers);
            List<FieldGiantClass> fittingClasses = new();
            Dictionary<FieldGiantClass, List<FieldObstacleOption>> fittingObstacles = new();
            CollectFittingGiants(context, fittingClasses, fittingObstacles);
            if (!HasWeight(fittingClasses))
                return new List<(FieldStructureDefinition, FieldFootprint)>();

            List<(FieldStructureDefinition structure, FieldFootprint footprint, int order)> giants = new(count);
            for (int i = 0; i < count; i++) {
                FieldGiantClass giantClass = FieldWeightedPick.Pick(fittingClasses, context.Random);
                FieldStructureDefinition structure = FieldWeightedPick.Pick(fittingObstacles[giantClass], context.Random).Structure;
                giants.Add((structure, _fieldFootprintReader.Read(structure, FieldRotation.None), i));
            }

            giants.Sort(CompareBiggestFirst);
            List<(FieldStructureDefinition, FieldFootprint)> ordered = new(count);
            foreach ((FieldStructureDefinition structure, FieldFootprint footprint, int _) in giants)
                ordered.Add((structure, footprint));

            return ordered;
        }

        // The classes and obstacles of the profile in their order, without the obstacles whose footprint is longer or
        // wider than the frame and the classes left without one: when everything fits, the picks are the profile's.
        private void CollectFittingGiants(FieldGenerationContext context, List<FieldGiantClass> fittingClasses,
                                          Dictionary<FieldGiantClass, List<FieldObstacleOption>> fittingObstacles) {
            foreach (FieldGiantClass giantClass in context.Profile.GiantClasses) {
                List<FieldObstacleOption> obstacles = CollectFittingObstacles(giantClass, context.Frame.Size);
                if (!HasWeight(obstacles))
                    continue;

                fittingClasses.Add(giantClass);
                fittingObstacles.Add(giantClass, obstacles);
            }
        }

        private List<FieldObstacleOption> CollectFittingObstacles(FieldGiantClass giantClass, Vector2Int frameSize) {
            List<FieldObstacleOption> obstacles = new();
            foreach (FieldObstacleOption obstacle in giantClass.Obstacles) {
                if (Fits(_fieldFootprintReader.Read(obstacle.Structure, FieldRotation.None), frameSize))
                    obstacles.Add(obstacle);
            }

            return obstacles;
        }

        private static bool Fits(FieldFootprint footprint, Vector2Int frameSize) =>
            footprint.Width <= frameSize.x && footprint.Height <= frameSize.y;

        private static bool HasWeight<T>(List<T> options) where T : IFieldWeighted {
            foreach (T option in options) {
                if (option.Weight > 0f)
                    return true;
            }

            return false;
        }

        // The draw order breaks ties, so the order is the same on every machine.
        private static int CompareBiggestFirst((FieldStructureDefinition structure, FieldFootprint footprint, int order) first,
                                               (FieldStructureDefinition structure, FieldFootprint footprint, int order) second) {
            int bySize = second.footprint.Cells.Count.CompareTo(first.footprint.Cells.Count);
            return bySize != 0 ? bySize : first.order.CompareTo(second.order);
        }

        private void TryLay(FieldGenerationContext context, FieldStructureDefinition structure, FieldFootprint footprint) {
            Vector2Int size = context.Frame.Size;
            Vector2Int room = new(size.x - footprint.Width, size.y - footprint.Height);
            for (int attempt = 0; attempt < context.Profile.GiantPlacementTries; attempt++) {
                Vector2Int anchor = new(context.Random.Range(0, room.x + 1), context.Random.Range(0, room.y + 1));
                Vector2Int middle = anchor + new Vector2Int(footprint.Width / 2, footprint.Height / 2);
                bool isWanted = context.Random.Value() < _fieldObstacleDensity.GetGiantChance(context, middle);
                if (!isWanted || !IsGapFree(context, footprint, anchor))
                    continue;

                if (_fieldStructurePlacer.TryPlace(context, structure, anchor, FieldRotation.None, FieldReserveKind.None, FieldCellClass.Ridge))
                    return;
            }
        }

        // No other giant within the gap cells of any footprint cell.
        private static bool IsGapFree(FieldGenerationContext context, FieldFootprint footprint, Vector2Int anchor) {
            foreach (Vector2Int cell in footprint.Cells) {
                if (IsGiantAround(context, anchor + cell, context.Profile.GiantGapCells))
                    return false;
            }

            return true;
        }

        private static bool IsGiantAround(FieldGenerationContext context, Vector2Int center, int gap) {
            for (int x = center.x - gap; x <= center.x + gap; x++) {
                for (int y = center.y - gap; y <= center.y + gap; y++) {
                    Vector2Int cell = new(x, y);
                    if (context.Cells.ValidateIndex(cell) && context.Cells[cell].IsOccupied)
                        return true;
                }
            }

            return false;
        }
    }
}
