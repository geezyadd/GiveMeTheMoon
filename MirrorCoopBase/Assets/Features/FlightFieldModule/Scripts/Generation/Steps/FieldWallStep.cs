using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Steps {
    // Wall: a band of the profile's thickness along the border is closed; reserved cells (station zones, takeoff and
    // route corridors) stay open. The giant rocks that close it are laid by FieldGiantPlacementStep.
    public sealed class FieldWallStep : IFieldGenerationStep {
        public bool IsUsedBy(FieldLayoutKind layout) =>
            layout == FieldLayoutKind.Corridors;

        public void Execute(FieldGenerationContext context) {
            Vector2Int size = context.Frame.Size;
            int thickness = context.Profile.WallThicknessCells;
            for (int y = 0; y < size.y; y++) {
                for (int x = 0; x < size.x; x++) {
                    bool isWallCell = DistanceToBorder(x, y, size) < thickness && context.Cells[x, y].IsFree;
                    if (isWallCell)
                        context.Cells[x, y] = context.Cells[x, y].WithClass(FieldCellClass.Wall);
                }
            }
        }

        private static int DistanceToBorder(int x, int y, Vector2Int size) =>
            Mathf.Min(Mathf.Min(x, size.x - 1 - x), Mathf.Min(y, size.y - 1 - y));
    }
}
