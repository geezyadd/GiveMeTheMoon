using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Model;
using Features.MatrixModule.Scripts;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor {
    // Paints a generated field top-down: A -> B points up, the field's right points right. The zone layer shows
    // structures grey with a dark outline per placement, debris cells tinted by their density and the reserved zones;
    // the density layer shows the density map, glades and voids. On top: every route centerline by kind, the
    // shortest way along the route and the shortest way outside all corridors, and the stations.
    public sealed class FlightFieldMapPainter {
        private const int MARKER_INSET_DIVIDER = 4;
        private const int PATH_INSET_DIVIDER = 3;
        private const float DEBRIS_TINT = 0.6f;
        private const float GLADE_TINT = 0.45f;
        private const float MIN_DEBRIS_TINT = 0.35f;

        public Texture2D Paint(FlightField field, int pixelsPerCell, FlightFieldMapLayer layer, bool isShowingPaths) {
            Vector2Int size = field.Frame.Size;
            Texture2D texture = new(size.y * pixelsPerCell, size.x * pixelsPerCell, TextureFormat.RGBA32, false) {
                filterMode = FilterMode.Point,
            };
            for (int x = 0; x < size.x; x++) {
                for (int y = 0; y < size.y; y++)
                    PaintCell(texture, field, new Vector2Int(x, y), pixelsPerCell, layer);
            }

            foreach (FieldRoute route in field.Routes)
                PaintCells(texture, route.Centerline, pixelsPerCell, GetCenterlineColor(route), MARKER_INSET_DIVIDER);

            if (isShowingPaths) {
                PaintCells(texture, field.Metrics.FreePath, pixelsPerCell, FlightFieldMapLegend.FreePath, PATH_INSET_DIVIDER);
                PaintCells(texture, field.Metrics.RoutePath, pixelsPerCell, FlightFieldMapLegend.RoutePath, PATH_INSET_DIVIDER);
            }

            PaintMarker(texture, field.StationACell, pixelsPerCell, FlightFieldMapLegend.StationA, MARKER_INSET_DIVIDER);
            PaintMarker(texture, field.StationBCell, pixelsPerCell, FlightFieldMapLegend.StationB, MARKER_INSET_DIVIDER);
            PaintMarker(texture, field.CruiseStartCell, pixelsPerCell, FlightFieldMapLegend.CruiseStart, MARKER_INSET_DIVIDER);
            texture.Apply();
            return texture;
        }

        // The ship may be outside the field: then it is not shown.
        public void PaintShip(Texture2D texture, Vector2Int cell, int pixelsPerCell) {
            PaintMarker(texture, cell, pixelsPerCell, FlightFieldMapLegend.Ship, MARKER_INSET_DIVIDER);
            texture.Apply();
        }

        private static void PaintCell(Texture2D texture, FlightField field, Vector2Int cell, int pixelsPerCell, FlightFieldMapLayer layer) {
            FieldCell fieldCell = field.Cells[cell];
            Color fill = layer == FlightFieldMapLayer.Density ? GetDensityColor(field.Density, cell) : GetZoneColor(fieldCell);
            bool isOutlined = layer == FlightFieldMapLayer.Zones && fieldCell.IsOccupied;
            for (int pixelX = 0; pixelX < pixelsPerCell; pixelX++) {
                for (int pixelY = 0; pixelY < pixelsPerCell; pixelY++) {
                    bool isOutline = isOutlined && IsPlacementEdge(field.Cells, cell, pixelX, pixelY, pixelsPerCell);
                    texture.SetPixel(cell.y * pixelsPerCell + pixelX, cell.x * pixelsPerCell + pixelY, isOutline ? FlightFieldMapLegend.StructureOutline : fill);
                }
            }
        }

        private static Color GetDensityColor(FieldDensityMap density, Vector2Int cell) {
            if (density.IsVoid(cell))
                return FlightFieldMapLegend.Void;

            Color color = Color.Lerp(FlightFieldMapLegend.DensityLow, FlightFieldMapLegend.DensityHigh, density.GetDensity(cell));
            return density.IsGlade(cell) ? Color.Lerp(color, FlightFieldMapLegend.Glade, GLADE_TINT) : color;
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

        private static Color GetZoneColor(FieldCell cell) {
            if (cell.IsOccupied)
                return FlightFieldMapLegend.Structure;

            Color reserveColor = GetReserveColor(cell.Reserve);
            return cell.HasDebris ? Color.Lerp(reserveColor, FlightFieldMapLegend.Debris, DEBRIS_TINT * Mathf.Max(MIN_DEBRIS_TINT, cell.DebrisDensity)) : reserveColor;
        }

        private static Color GetReserveColor(FieldReserveKind reserve) =>
            reserve switch {
                FieldReserveKind.None => FlightFieldMapLegend.Free,
                FieldReserveKind.StationZone => FlightFieldMapLegend.StationZone,
                FieldReserveKind.TakeoffCorridor => FlightFieldMapLegend.TakeoffCorridor,
                FieldReserveKind.RouteCorridor => FlightFieldMapLegend.RouteCorridor,
                FieldReserveKind.AlternativeCorridor => FlightFieldMapLegend.AlternativeCorridor,
                FieldReserveKind.DeadEndCorridor => FlightFieldMapLegend.DeadEndCorridor,
                _ => throw new ArgumentOutOfRangeException(nameof(reserve), reserve, "No map color for this reserve."),
            };

        private static Color GetCenterlineColor(FieldRoute route) =>
            route.Kind switch {
                FieldRouteKind.Main => FlightFieldMapLegend.Centerline,
                FieldRouteKind.Alternative => route.IsClosed ? FlightFieldMapLegend.ClosedAlternativeCenterline : FlightFieldMapLegend.AlternativeCenterline,
                FieldRouteKind.DeadEnd => FlightFieldMapLegend.DeadEndCenterline,
                _ => throw new ArgumentOutOfRangeException(nameof(route), route.Kind, "No map color for this route kind."),
            };

        private static void PaintCells(Texture2D texture, IReadOnlyList<Vector2Int> cells, int pixelsPerCell, Color color, int insetDivider) {
            foreach (Vector2Int cell in cells)
                PaintMarker(texture, cell, pixelsPerCell, color, insetDivider);
        }

        private static void PaintMarker(Texture2D texture, Vector2Int cell, int pixelsPerCell, Color color, int insetDivider) {
            int inset = pixelsPerCell / insetDivider;
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
