using System;
using Features.MatrixModule.Scripts.MatrixFactory;
using UnityEngine;

namespace Features.MatrixModule.Scripts.Services.ExpandMatrix {
    public class MatrixExpander : IMatrixExpander {
        private const string INVALID_DIRECTION = "Invalid direction";
        
        public void ExpandMatrix<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, MatrixOperationDirection matrixOperationDirection, int number, TMatrixEntity fillEntity) =>
            matrix.Rows = GetExpandMatrix(matrix, matrixOperationDirection, fillEntity, number).Rows;
        
        private IMatrix<TMatrixEntity> GetExpandMatrix<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, MatrixOperationDirection matrixOperationDirection, TMatrixEntity fillEntity, int number) {
            int newMatrixRowCount = matrix.GetRowCount();
            int newMatrixColumnCount = matrix.GetColumnCount();
            int insertY = 0;
            int insertX = 0;
            switch (matrixOperationDirection) {
                case MatrixOperationDirection.Left:
                    insertX = number;
                    newMatrixColumnCount += number;
                    break;
                case MatrixOperationDirection.Right: 
                    newMatrixColumnCount += number;
                    break;
                case MatrixOperationDirection.Top: 
                    insertY = number;
                    newMatrixRowCount += number;
                    break;
                case MatrixOperationDirection.Bottom: 
                    newMatrixRowCount += number;
                    break;
                default: throw new ArgumentException(INVALID_DIRECTION);
            }

            insertX = Mathf.Max(0, insertX);
            
            newMatrixRowCount = Mathf.Max(matrix.GetRowCount(), newMatrixRowCount);
            newMatrixColumnCount = Mathf.Max(matrix.GetColumnCount(), newMatrixColumnCount);
            
            IMatrix<TMatrixEntity> expandMatrix = new MatrixFactory.MatrixFactory().Create(newMatrixColumnCount, newMatrixRowCount, fillEntity);
            expandMatrix.SilentInsertShapeMatrix(matrix, insertX, insertY);
            return expandMatrix;
        }
    }
}
