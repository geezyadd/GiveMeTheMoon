using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Structures {
    // Turns the footprint rows of a definition into cells and rotates them by quarter turns.
    public sealed class FieldFootprintReader : IFieldFootprintReader {
        public FieldFootprint Read(FieldStructureDefinition structure, FieldRotation rotation) {
            IReadOnlyList<string> rows = structure.FootprintRows;
            int width = CountColumns(rows);
            int height = rows.Count;
            List<Vector2Int> cells = ReadCells(rows);
            for (int turn = 0; turn < (int)rotation; turn++) {
                TurnClockwise(cells, height);
                (width, height) = (height, width);
            }

            return new FieldFootprint(width, height, cells);
        }

        private static int CountColumns(IReadOnlyList<string> rows) {
            int columns = 0;
            for (int y = 0; y < rows.Count; y++)
                columns = Mathf.Max(columns, rows[y].Length);

            return columns;
        }

        private static List<Vector2Int> ReadCells(IReadOnlyList<string> rows) {
            List<Vector2Int> cells = new();
            for (int y = 0; y < rows.Count; y++) {
                for (int x = 0; x < rows[y].Length; x++) {
                    if (rows[y][x] == FieldFootprintSymbols.OCCUPIED_CELL)
                        cells.Add(new Vector2Int(x, y));
                }
            }

            return cells;
        }

        // (x, y) in a box of the given height goes to (height - 1 - y, x): the box turns by 90 degrees clockwise.
        private static void TurnClockwise(List<Vector2Int> cells, int height) {
            for (int i = 0; i < cells.Count; i++)
                cells[i] = new Vector2Int(height - 1 - cells[i].y, cells[i].x);
        }
    }
}
