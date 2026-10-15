using System;
using Features.FlightFieldModule.Scripts.Model;
using Features.MatrixModule.Scripts;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor {
    // Paints a generated field top-down: A -> B points up, the field's right points right. Structures are grey with
    // a dark outline per placement, reserved zones are tinted, route centerlines and stations are marked.
    public sealed class FlightFieldMapPainter {
        private const int MARKER_INSET_DIVIDER = 4;

        public Texture2D Paint(FlightField field, int pixelsPerCell) {
            int side = field.Frame.SideCells;
            Texture2D texture = new(side * pixelsPerCell, side * pixelsPerCell, TextureFormat.RGBA32, false) {
                filterMode = FilterMode.Point,
            };
            for (int x = 0; x < side; x++) {
                for (int y = 0; y < side; y++)
                    PaintCell(texture, field.Cells, new Vector2Int(x, y), pixelsPerCell);
            }

            foreach (FieldRoute route in field.Routes) {
                foreach (Vector2Int cell in route.Centerline)
                    PaintMarker(texture, cell, pixelsPerCell, FlightFieldMapLegend.Centerline);
            }

            PaintMarker(texture, field.StationACell, pixelsPerCell, FlightFieldMapLegend.StationA);
            PaintMarker(texture, field.StationBCell, pixelsPerCell, FlightFieldMapLegend.StationB);
            PaintMarker(texture, field.CruiseStartCell, pixelsPerCell, FlightFieldMapLegend.CruiseStart);
            texture.Apply();
            return texture;
        }

        // The ship may be outside the field: then it is not shown.
        public void PaintShip(Texture2D texture, Vector2Int cell, int pixelsPerCell) {
            PaintMarker(texture, cell, pixelsPerCell, FlightFieldMapLegend.Ship);
            texture.Apply();
        }

        private static void PaintCell(Texture2D texture, IMatrix<FieldCell> cells, Vector2Int cell, int pixelsPerCell) {
            FieldCell fieldCell = cells[cell];
            Color fill = GetCellColor(fieldCell);
            for (int pixelX = 0; pixelX < pixelsPerCell; pixelX++) {
                for (int pixelY = 0; pixelY < pixelsPerCell; pixelY++) {
                    bool isOutline = fieldCell.IsOccupied && IsPlacementEdge(cells, cell, pixelX, pixelY, pixelsPerCell);
                    texture.SetPixel(cell.y * pixelsPerCell + pixelX, cell.x * pixelsPerCell + pixelY, isOutline ? FlightFieldMapLegend.StructureOutline : fill);
                }
            }
        }

        // Texture x is the field's y (right) and texture y is the field's x (forward).
        private static bool IsPlacementEdge(IMatrix<FieldCell> cells, Vector2Int cell, int pixelX, int pixelY, int pixelsPerCell) {
            int last = pixelsPerCell - 1;
            return pixelX == 0 && IsOtherPlacement(cells, cell, cell + Vector2Int.down)
                || pixelX == last && IsOtherPlacement(cells, cell, cell + Vector2Int.up)
                || pixelY == 0 && IsOtherPlacement(cells, cell, cell + Vector2Int.left)
                || pixelY == last && IsOtherPlacement(cells, cell, cell + Vector2Int.right);
        }

        private static bool IsOtherPlacement(IMatrix<FieldCell> cells, Vector2Int cell, Vector2Int neighbor) =>
            !cells.ValidateIndex(neighbor) || cells[neighbor].PlacementId != cells[cell].PlacementId;

        private static Color GetCellColor(FieldCell cell) {
            if (cell.IsOccupied)
                return FlightFieldMapLegend.Structure;

            return cell.Reserve switch {
                FieldReserveKind.None => FlightFieldMapLegend.Free,
                FieldReserveKind.StationZone => FlightFieldMapLegend.StationZone,
                FieldReserveKind.TakeoffCorridor => FlightFieldMapLegend.TakeoffCorridor,
                FieldReserveKind.RouteCorridor => FlightFieldMapLegend.RouteCorridor,
                _ => throw new ArgumentOutOfRangeException(nameof(cell), cell.Reserve, "No map color for this reserve."),
            };
        }

        private static void PaintMarker(Texture2D texture, Vector2Int cell, int pixelsPerCell, Color color) {
            int inset = pixelsPerCell / MARKER_INSET_DIVIDER;
            for (int pixelX = inset; pixelX < pixelsPerCell - inset; pixelX++) {
                for (int pixelY = inset; pixelY < pixelsPerCell - inset; pixelY++)
                    SetPixelInside(texture, cell.y * pixelsPerCell + pixelX, cell.x * pixelsPerCell + pixelY, color);
            }
        }

        private static void SetPixelInside(Texture2D texture, int x, int y, Color color) {
            if (IsInsideTexture(texture, x, y))
                texture.SetPixel(x, y, color);
        }

        private static bool IsInsideTexture(Texture2D texture, int x, int y) =>
            x >= 0 && x < texture.width && y >= 0 && y < texture.height;
    }
}
