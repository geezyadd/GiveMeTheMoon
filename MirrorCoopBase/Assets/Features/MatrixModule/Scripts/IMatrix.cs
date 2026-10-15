using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.MatrixModule.Scripts {
    public interface IMatrix<TMatrixEntity> : IEnumerable<TMatrixEntity> {
        public List<Vector2Int> GetIndexes(Predicate<TMatrixEntity> predicate = null); 
        public List<MatrixSavableContainer<TMatrixEntity>> Rows { get; set; }
        public List<TMatrixEntity> GetColumnById(int columnId);
        public List<TMatrixEntity> GetRowById(int rowId);
        public void CropMatrix(MatrixOperationDirection matrixOperationDirection, int number);
        public void CropMatrixByRule(MatrixOperationDirection matrixOperationDirection, Predicate<TMatrixEntity> predicate);
        public void ExpandMatrix(MatrixOperationDirection matrixOperationDirection, int number, TMatrixEntity fillEntity = default);
        public List<TMatrixEntity> SilentInsertShapeMatrix(IMatrix<TMatrixEntity> shape, int x, int y, Predicate<TMatrixEntity> matrixPredicate = null, Predicate<TMatrixEntity> shapePredicate = null);
        public List<TMatrixEntity> InsertShapeMatrix(IMatrix<TMatrixEntity> shape, int x, int y, Predicate<TMatrixEntity> matrixPredicate = null, Predicate<TMatrixEntity> shapePredicate = null);
        public bool TryInsertShapeMatrix(IMatrix<TMatrixEntity> shape, int x, int y, out List<TMatrixEntity> insertedMatrixEntities, Predicate<TMatrixEntity> matrixPredicate = null, Predicate<TMatrixEntity> shapePredicate = null);
        public bool TryInsertFullShapeMatrix(IMatrix<TMatrixEntity> shape, int x, int y, out List<TMatrixEntity> insertedMatrixEntities, Predicate<TMatrixEntity> matrixPredicate = null, Predicate<TMatrixEntity> shapePredicate = null);
        public int GetRowCount();
        public int GetColumnCount();
        public void ReplaceMatrixEntity(TMatrixEntity newEntity, Predicate<TMatrixEntity> predicate);
        public List<Vector2Int> PathFinding(Vector2Int start, Vector2Int end, bool isDiagonalMove = false, Predicate<TMatrixEntity> predicate = null);
        public Node GetNeighborByDirection(Vector2Int position, Vector2Int direction);
        public List<TMatrixEntity> GetSortedMatrixEntities(Comparison<TMatrixEntity> comparison, bool isAscending);
        public void SetValue(int x, int y, TMatrixEntity value);
        public TMatrixEntity GetValue(int x, int y);
        public void RemoveColumn(int columnIndex, int count);
        public void RemoveRow(int rowIndex, int count);
        public bool CheckForExpression(Predicate<TMatrixEntity> predicate, int columnIndex);
        public List<Vector2Int> GetNeighbors(Vector2Int position, bool isDiagonalMove = false, Predicate<TMatrixEntity> predicate = null);
        public bool ValidatePredicate(Vector2Int position, Predicate<TMatrixEntity> predicate = null);
        public bool ValidateIndex(Vector2Int position);
        public bool ValidateIndex(int x, int y);
        public TMatrixEntity this[int i, int j] { get; set; }
        public TMatrixEntity this[Vector2Int index] { get; set; }
    }
}