using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Model;
using Features.MatrixModule.Scripts;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor {
    // Paints a generated field top-down: A -> B points up, the field's right points right. The zone layer shows
    // structures grey with a dark outline per placement, debris cells tinted by their density and the reserved zones;
    // the density layer shows the density map and voids. On top: the main centerline and the stations.
    public sealed class FlightFieldMapPainter {
        private const int MARKER_INSET_DIVIDER = 4;
        private const float DEBRIS_TINT = 0.6f;
        private const float MIN_DEBRIS_TINT = 0.35f;

        public Texture2D Paint(FlightField field, int pixelsPerCell, FlightFieldMapLayer layer) {
            Vector2Int size = field.Frame.Size;
            Texture2D texture = new(size.y * pixelsPerCell, size.x * pixelsPerCell, TextureFormat.RGBA32, false) {
                filterMode = FilterMode.Point,
            };
            for (int x = 0; x < size.x; x++) {
                for (int y = 0; y < size.y; y++)
                    PaintCell(texture, field, new Vector2Int(x, y), pixelsPerCell, layer);
            }

            foreach (FieldRoute route in field.Routes)
                PaintCells(texture, route.Centerline, pixelsPerCell, FlightFieldMapLegend.Centerline, MARKER_INSET_DIVIDER);

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

        // The debris rocks as discs (those off the flight plane dimmer) and the slalom way as a line of dots.
        public void PaintSlalom(Texture2D texture, FieldSlalomReport report, float cellSize, int pixelsPerCell) {
            float pixelsPerMeter = pixelsPerCell / cellSize;
            foreach (FieldSlalomRock rock in report.Rocks)
                PaintDisc(texture, rock.Position * pixelsPerMeter, Mathf.Max(1f, rock.Radius * pixelsPerMeter), rock.IsInPlane ? FlightFieldMapLegend.Rock : FlightFieldMapLegend.OffPlaneRock);

            for (int i = 1; i < report.Path.Count; i++)
                PaintLine(texture, report.Path[i - 1] * pixelsPerMeter, report.Path[i] * pixelsPerMeter, FlightFieldMapLegend.SlalomPath);

            texture.Apply();
        }

        private static void PaintDisc(Texture2D texture, Vector2 center, float radius, Color color) {
            int reach = Mathf.CeilToInt(radius);
            for (int x = -reach; x <= reach; x++) {
                for (int y = -reach; y <= reach; y++) {
                    if (x * x + y * y <= radius * radius)
                        SetPixelInside(texture, Mathf.FloorToInt(center.x) + x, Mathf.FloorToInt(center.y) + y, color);
                }
            }
        }

        private static void PaintLine(Texture2D texture, Vector2 from, Vector2 to, Color color) {
            int samples = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, to)));
            for (int i = 0; i <= samples; i++) {
                Vector2 point = Vector2.Lerp(from, to, i / (float)samples);
                SetPixelInside(texture, Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y), color);
            }
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

            return Color.Lerp(FlightFieldMapLegend.DensityLow, FlightFieldMapLegend.DensityHigh, density.GetDensity(cell));
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
                FieldReserveKind.PointOfInterest => FlightFieldMapLegend.PointOfInterest,
                _ => throw new ArgumentOutOfRangeException(nameof(reserve), reserve, "No map color for this reserve."),
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
