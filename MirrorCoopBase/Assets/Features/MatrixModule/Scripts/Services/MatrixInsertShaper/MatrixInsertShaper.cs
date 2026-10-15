using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.MatrixModule.Scripts.Services.MatrixInsertShaper {
    public class MatrixInsertShaper : IMatrixInsertShaper {
        public List<TMatrixEntity> InsertShapeMatrix<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, IMatrix<TMatrixEntity> shape, int x, int y, Predicate<TMatrixEntity> matrixPredicate = null, Predicate<TMatrixEntity> shapePredicate = null) {
            if (shape.GetRowCount() > matrix.GetRowCount() || shape.GetColumnCount() > matrix.GetColumnCount())
                throw new IndexOutOfRangeException($"Row or column count is out of range.");

            return SilentInsertShapeMatrix(matrix, shape, x, y, matrixPredicate, shapePredicate);
        }

        public List<TMatrixEntity> SilentInsertShapeMatrix<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, IMatrix<TMatrixEntity> shape, int x, int y, Predicate<TMatrixEntity> matrixPredicate = null, Predicate<TMatrixEntity> shapePredicate = null) {
            List<TMatrixEntity> matrixEntities = new();
            int ownY = y;
            for (int shapeY = 0; shapeY < shape.GetRowCount(); shapeY++) {
                if (shapeY == matrix.GetRowCount())
                    return new List<TMatrixEntity>();

                List<TMatrixEntity> row = shape.GetRowById(shapeY);
                int ownX = x;
                foreach (TMatrixEntity entity in row) {
                    if (ownX >= matrix.GetColumnCount())
                        break;

                    if (matrix.ValidateIndex(new Vector2Int(ownX, ownY)) && (shapePredicate == null || shapePredicate(entity)) && (matrixPredicate == null || matrixPredicate(matrix.GetValue(ownX, ownY)))) {
                        matrix.SetValue(ownX, ownY, entity);
                        matrixEntities.Add(entity);
                    }

                    ownX++;
                }

                ownY++;
                if (ownY == matrix.GetRowCount())
                    return new List<TMatrixEntity>();
            }

            return matrixEntities;
        }

        public bool TryInsertShapeMatrix<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, IMatrix<TMatrixEntity> shape, int x, int y, out List<TMatrixEntity> insertedMatrixEntities, Predicate<TMatrixEntity> matrixPredicate = null, Predicate<TMatrixEntity> shapePredicate = null) {
            insertedMatrixEntities = new List<TMatrixEntity>();
            if (shape.GetRowCount() > matrix.GetRowCount() || shape.GetColumnCount() > matrix.GetColumnCount())
                return false;

            insertedMatrixEntities = SilentInsertShapeMatrix(matrix, shape, x, y, matrixPredicate, shapePredicate);
            return true;
        }

        public bool TryInsertFullShapeMatrix<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, IMatrix<TMatrixEntity> shape, int x, int y, out List<TMatrixEntity> matrixEntities, Predicate<TMatrixEntity> matrixPredicate = null, Predicate<TMatrixEntity> shapePredicate = null) {
            matrixEntities = new List<TMatrixEntity>();
            if (!ValidateShape(matrix, shape, x, y, matrixPredicate))
                return false;
            int ownY = y;
            for (int shapeY = 0; shapeY < shape.GetRowCount(); shapeY++) {
                List<TMatrixEntity> row = shape.GetRowById(shapeY);
                int ownX = x;
                foreach (TMatrixEntity entity in row) {
                    if (ownX >= matrix.GetColumnCount())
                        break;

                    if (shapePredicate == null || shapePredicate(entity)) {
                        matrix.SetValue(ownX, ownY, entity);
                        matrixEntities.Add(entity);
                    }

                    ownX++;
                }

                ownY++;
            }

            return true;
        }

        private bool ValidateShape<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, IMatrix<TMatrixEntity> shape, int x, int y, Predicate<TMatrixEntity> matrixPredicate = null) {
            int ownY = y;
            for (int shapeY = 0; shapeY < shape.GetRowCount(); shapeY++) {
                if (shapeY == matrix.GetRowCount())
                    return false;

                List<TMatrixEntity> row = shape.GetRowById(shapeY);
                int ownX = x;
                foreach (TMatrixEntity unused in row) {
                    if (ownX >= matrix.GetColumnCount())
                        break;

                    if (!(matrix.ValidateIndex(new Vector2Int(ownX, ownY)) && (matrixPredicate == null || matrixPredicate(matrix.GetValue(ownX, ownY)))))
                        return false;

                    ownX++;
                }

                ownY++;
                if (ownY == matrix.GetRowCount())
                    return false;
            }

            return true;
        }
    }
}
