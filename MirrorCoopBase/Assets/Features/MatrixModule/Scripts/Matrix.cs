using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Features.MatrixModule.Scripts.Exceptions;
using Features.MatrixModule.Scripts.Services.ExpandMatrix;
using Features.MatrixModule.Scripts.Services.MatrixCropper;
using Features.MatrixModule.Scripts.Services.MatrixEntityReplacer;
using Features.MatrixModule.Scripts.Services.MatrixEntitySorter;
using Features.MatrixModule.Scripts.Services.MatrixInsertShaper;
using Features.MatrixModule.Scripts.Services.MatrixPathFinder;
using UnityEngine;

namespace Features.MatrixModule.Scripts {
    public class Matrix<TMatrixEntity> : IMatrix<TMatrixEntity> {
        private readonly IMatrixCropper _matrixCropper;
        private readonly IMatrixExpander _matrixExpander;
        private readonly IMatrixInsertShaper _matrixInsertShaper;
        private readonly IMatrixEntityReplacer _matrixEntityReplacer;
        private readonly IMatrixPathFinder _matrixPathFinder;
        private readonly IMatrixEntitySorter _matrixEntitySorter;

        public List<MatrixSavableContainer<TMatrixEntity>> Rows { get; set; }

        public TMatrixEntity this[int i, int j] { get => GetValue(i,j); set => SetValue(i,j,value); }
        public TMatrixEntity this[Vector2Int index] { get => GetValue(index.x, index.y); set => SetValue(index.x, index.y, value); }

        public Matrix(int width, int height, IMatrixCropper matrixCropper, IMatrixExpander matrixExpander, IMatrixInsertShaper matrixInsertShaper, IMatrixEntityReplacer matrixEntityReplacer, IMatrixPathFinder matrixPathFinder, IMatrixEntitySorter matrixEntitySorter, TMatrixEntity fillEntity) {
            _matrixEntityReplacer = matrixEntityReplacer;
            _matrixInsertShaper = matrixInsertShaper;
            _matrixCropper = matrixCropper;
            _matrixExpander = matrixExpander;
            _matrixPathFinder = matrixPathFinder;
            _matrixEntitySorter = matrixEntitySorter;
            Rows = new List<MatrixSavableContainer<TMatrixEntity>>();
            CreateMatrix(width, height, fillEntity);
        }

        public Matrix(List<MatrixSavableContainer<TMatrixEntity>> savableContainers, IMatrixCropper matrixCropper,
                      IMatrixExpander matrixExpander, IMatrixInsertShaper matrixInsertShaper, IMatrixEntityReplacer matrixEntityReplacer,
                      IMatrixPathFinder matrixPathFinder, IMatrixEntitySorter matrixEntitySorter) {
            _matrixEntityReplacer = matrixEntityReplacer;
            _matrixInsertShaper = matrixInsertShaper;
            _matrixCropper = matrixCropper;
            _matrixExpander = matrixExpander;
            _matrixPathFinder = matrixPathFinder;
            _matrixEntitySorter = matrixEntitySorter;
            Rows = savableContainers;
        }

        public List<TMatrixEntity> GetColumnById(int columnId) {
            if (GetRowCount() == 0 || columnId < 0 || columnId >= GetColumnCount())
                throw new IndexOutOfRangeException($"Column index {columnId} is out of range.");

            return Rows.Select(row => row.Data[columnId]).ToList();
        }

        public List<TMatrixEntity> GetRowById(int rowId) {
            if (rowId < 0 || rowId >= GetRowCount())
                throw new IndexOutOfRangeException($"Row index {rowId} is out of range.");

            return Rows[rowId].Data;
        }

        public void CropMatrix(MatrixOperationDirection matrixOperationDirection, int number) =>
            _matrixCropper.CropMatrix(this, matrixOperationDirection, number);

        public void CropMatrixByRule(MatrixOperationDirection matrixOperationDirection, Predicate<TMatrixEntity> predicate) =>
            _matrixCropper.CropMatrixByRule(this, matrixOperationDirection, predicate);

        public int GetRowCount() =>
            Rows.Count;

        public int GetColumnCount() =>
            Rows[0].Data.Count;

        public List<Vector2Int> GetIndexes(Predicate<TMatrixEntity> predicate = null) {
            List<Vector2Int> indexes = new();
            for (int y = 0; y < GetRowCount(); y++) {
                for (int x = 0; x < GetColumnCount(); x++) {
                    if(ValidatePredicate(new Vector2Int(x, y), predicate))
                        indexes.Add(new Vector2Int(x, y));
                }
            }

            return indexes;
        }

        public void ExpandMatrix(MatrixOperationDirection matrixOperationDirection, int number, TMatrixEntity fillEntity = default) =>
            _matrixExpander.ExpandMatrix(this, matrixOperationDirection, number, fillEntity);

        public List<TMatrixEntity> InsertShapeMatrix(IMatrix<TMatrixEntity> shape, int x, int y, Predicate<TMatrixEntity> matrixPredicate = null, Predicate<TMatrixEntity> shapePredicate = null) =>
            _matrixInsertShaper.InsertShapeMatrix(this, shape, x, y, matrixPredicate, shapePredicate);

        public bool TryInsertShapeMatrix(IMatrix<TMatrixEntity> shape, int x, int y, out List<TMatrixEntity> insertedMatrixEntities, Predicate<TMatrixEntity> matrixPredicate = null, Predicate<TMatrixEntity> shapePredicate = null) =>
            _matrixInsertShaper.TryInsertShapeMatrix(this, shape, x, y, out insertedMatrixEntities, matrixPredicate, shapePredicate);

        public bool TryInsertFullShapeMatrix(IMatrix<TMatrixEntity> shape, int x, int y, out List<TMatrixEntity> insertedMatrixEntities, Predicate<TMatrixEntity> matrixPredicate = null, Predicate<TMatrixEntity> shapePredicate = null) =>
            _matrixInsertShaper.TryInsertFullShapeMatrix(this, shape, x, y, out insertedMatrixEntities, matrixPredicate, shapePredicate);

        public List<TMatrixEntity> SilentInsertShapeMatrix(IMatrix<TMatrixEntity> shape, int x, int y, Predicate<TMatrixEntity> matrixPredicate = null, Predicate<TMatrixEntity> shapePredicate = null) =>
            _matrixInsertShaper.SilentInsertShapeMatrix(this, shape, x, y, matrixPredicate, shapePredicate);

        public void ReplaceMatrixEntity(TMatrixEntity newEntity, Predicate<TMatrixEntity> predicate) =>
            _matrixEntityReplacer.ReplaceMatrixEntity(this, newEntity, predicate);

        public List<Vector2Int> PathFinding(Vector2Int start, Vector2Int end, bool isDiagonalMove = false,  Predicate<TMatrixEntity> predicate = null) =>
            _matrixPathFinder.PathFinding(this, start, end, isDiagonalMove, predicate);

        public Node GetNeighborByDirection(Vector2Int position, Vector2Int direction) =>
            ValidateIndex(new Vector2Int(position.x  + direction.x, position.y + direction.y))
                ? new Node(position.x + direction.x, position.y - direction.y)
                : null;

        public List<TMatrixEntity> GetSortedMatrixEntities(Comparison<TMatrixEntity> comparison, bool isAscending) =>
            _matrixEntitySorter.GetSortedMatrixEntities(this, comparison, isAscending);

        public void SetValue(int x, int y, TMatrixEntity value) =>
            Rows[y].Data[x] = value;

        public TMatrixEntity GetValue(int x, int y) =>
            Rows[y].Data[x];

        public void RemoveColumn(int columnIndex, int count) {
            foreach (MatrixSavableContainer<TMatrixEntity> row in Rows)
                row.Data.RemoveRange(columnIndex, count);
        }

        public void RemoveRow(int rowIndex, int count) =>
            Rows.RemoveRange(rowIndex, count);

        public bool CheckForExpression(Predicate<TMatrixEntity> predicate, int columnIndex) {
            foreach (MatrixSavableContainer<TMatrixEntity> row in Rows)
                if(!predicate(row.Data[columnIndex]))
                    return false;

            return true;
        }

        public List<Vector2Int> GetNeighbors(Vector2Int position,  bool isDiagonalMove = false, Predicate<TMatrixEntity> predicate = null) {
            List<Vector2Int> neighbors = new();
            AddSideNeighbors(position, predicate, neighbors);

            if (!isDiagonalMove)
                return neighbors;

            AddDiagonalNeighbors(position, predicate, neighbors);

            return neighbors;
        }

        public bool ValidatePredicate(Vector2Int position, Predicate<TMatrixEntity> predicate = null) =>
            predicate == null || predicate(Rows[position.y].Data[position.x]);

        public bool ValidateIndex(Vector2Int position) =>
            position.y >= 0 && position.y < GetRowCount() && position.x >= 0 && position.x < GetColumnCount();

        public bool ValidateIndex(int x, int y) =>
            y >= 0 && y < GetRowCount() && x >= 0 && x < GetColumnCount();

        IEnumerator<TMatrixEntity> IEnumerable<TMatrixEntity>.GetEnumerator() =>
            GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        private void CreateMatrix(int width, int height, TMatrixEntity fillEntity) {
            if(!typeof(TMatrixEntity).IsValueType && fillEntity is not ICloneable)
                throw new NotValidMatrixEntityTypeException(typeof(TMatrixEntity));

            for (int y = 0; y < height; y++) {
                List<TMatrixEntity> row = new();
                for (int x = 0; x < width; x++)
                    if (typeof(TMatrixEntity).IsValueType)
                        row.Add(fillEntity);
                    else
                        row.Add((TMatrixEntity)((ICloneable)fillEntity).Clone());

                Rows.Add(new MatrixSavableContainer<TMatrixEntity> {
                    Data = row
                });
            }
        }

        private void AddDiagonalNeighbors(Vector2Int position, Predicate<TMatrixEntity> predicate, List<Vector2Int> neighbors) {
            if (ValidateIndex(new Vector2Int(position.x + 1, position.y + 1)) &&
                ValidatePredicate(new Vector2Int(position.x + 1, position.y + 1), predicate) &&
                ValidateDiagonalNeighbor(position, new Vector2Int(position.x + 1, position.y + 1), predicate))
                neighbors.Add(new Vector2Int(position.x + 1, position.y + 1));

            if (ValidateIndex(new Vector2Int(position.x - 1, position.y + 1)) &&
                ValidatePredicate(new Vector2Int(position.x - 1, position.y + 1), predicate) &&
                ValidateDiagonalNeighbor(position, new Vector2Int(position.x - 1, position.y + 1), predicate))
                neighbors.Add(new Vector2Int(position.x - 1, position.y + 1));

            if (ValidateIndex(new Vector2Int(position.x - 1, position.y - 1)) &&
                ValidatePredicate(new Vector2Int(position.x - 1, position.y - 1), predicate) &&
                ValidateDiagonalNeighbor(position, new Vector2Int(position.x - 1, position.y - 1), predicate))
                neighbors.Add(new Vector2Int(position.x - 1, position.y - 1));

            if (ValidateIndex(new Vector2Int(position.x + 1, position.y - 1)) &&
                ValidatePredicate(new Vector2Int(position.x + 1, position.y - 1), predicate) &&
                ValidateDiagonalNeighbor(position, new Vector2Int(position.x + 1, position.y - 1), predicate))
                neighbors.Add(new Vector2Int(position.x + 1, position.y - 1));
        }

        private void AddSideNeighbors(Vector2Int position, Predicate<TMatrixEntity> predicate, List<Vector2Int> neighbors) {
            if (ValidateIndex(new Vector2Int(position.x, position.y - 1)) && ValidatePredicate(new Vector2Int(position.x, position.y - 1), predicate))
                neighbors.Add(new Vector2Int(position.x, (position.y - 1)));

            if (ValidateIndex(new Vector2Int(position.x, position.y + 1)) && ValidatePredicate(new Vector2Int(position.x, position.y + 1), predicate))
                neighbors.Add(new Vector2Int(position.x, (position.y + 1)));

            if (ValidateIndex(new Vector2Int(position.x - 1, position.y)) && ValidatePredicate(new Vector2Int(position.x - 1, position.y), predicate))
                neighbors.Add(new Vector2Int(position.x - 1, position.y));

            if (ValidateIndex(new Vector2Int(position.x + 1, position.y)) && ValidatePredicate(new Vector2Int(position.x + 1, position.y), predicate))
                neighbors.Add(new Vector2Int(position.x + 1, position.y));
        }

        private bool ValidateDiagonalNeighbor(Vector2Int position, Vector2Int diagonalNeighborPosition, Predicate<TMatrixEntity> predicate) =>
            ValidatePredicate(new Vector2Int(position.x, diagonalNeighborPosition.y), predicate) &&
            ValidatePredicate(new Vector2Int(diagonalNeighborPosition.x, position.y), predicate);

        private IEnumerator<TMatrixEntity> GetEnumerator() {
            foreach (MatrixSavableContainer<TMatrixEntity> row in Rows)
                foreach (TMatrixEntity entity in row.Data)
                    yield return entity;
        }
    }
}
