using System;
using System.Linq;

namespace Features.MatrixModule.Scripts.Services.MatrixCropper {
    public class MatrixCropper : IMatrixCropper
    { 
        private const string INVALID_DIRECTION = "Invalid direction";
    
        public void CropMatrix<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, MatrixOperationDirection matrixOperationDirection, int number) {
            switch (matrixOperationDirection) {
                case MatrixOperationDirection.Left: 
                    CropMatrixLeft(matrix, number);
                    break;
                case MatrixOperationDirection.Right: 
                    CropMatrixRight(matrix, number);
                    break;
                case MatrixOperationDirection.Top: 
                    CropMatrixTop(matrix, number);
                    break;
                case MatrixOperationDirection.Bottom: 
                    CropMatrixBottom(matrix, number);
                    break;
                case MatrixOperationDirection.Center: {
                    CropMatrixLeft(matrix, number);
                    CropMatrixRight(matrix, number);
                    CropMatrixTop(matrix, number);
                    CropMatrixBottom(matrix, number);

                    break;
                }
                default: throw new ArgumentException(INVALID_DIRECTION);
            }
        }
        
        public void CropMatrixByRule<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, MatrixOperationDirection matrixOperationDirection, Predicate<TMatrixEntity> predicate) {
            switch (matrixOperationDirection) {
                case MatrixOperationDirection.Left: 
                    CropMatrixLeftByRule(matrix, predicate);
                    break;
                case MatrixOperationDirection.Right: 
                    CropMatrixRightByRule(matrix, predicate);
                    break;
                case MatrixOperationDirection.Top: 
                    CropMatrixTopByRule(matrix, predicate);
                    break;
                case MatrixOperationDirection.Bottom: 
                    CropMatrixBottomByRule(matrix, predicate);
                    break;
                case MatrixOperationDirection.Center: {
                    CropMatrixLeftByRule(matrix, predicate);
                    CropMatrixRightByRule(matrix, predicate);
                    CropMatrixTopByRule(matrix, predicate);
                    CropMatrixBottomByRule(matrix, predicate);

                    break;
                }
                default: throw new ArgumentException(INVALID_DIRECTION);
            }
        }
        
        private void CropMatrixLeft<TMatrixEntity>(IMatrix<TMatrixEntity> matrix,int number) {
            if (number > matrix.GetColumnCount() || number < 0)
                throw new IndexOutOfRangeException($"Column count {number} is out of range.");

            matrix.RemoveColumn(0 , number);
        }
        
        private void CropMatrixLeftByRule<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, Predicate<TMatrixEntity> predicate = null) {
            if (predicate != null) {
                for(int i = matrix.GetColumnCount(); i > 0; i--){
                    if (!matrix.CheckForExpression(predicate, 0))
                        return;
                    matrix.RemoveColumn(0, 1);
                }
            }
        }
        
        private void CropMatrixRight<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, int number) {
            if (number > matrix.GetColumnCount() || number < 0)
                throw new IndexOutOfRangeException($"Column count {number} is out of range.");
            
            int iteration = 0;
            for (int y = matrix.GetColumnCount() - 1; y >= 0; y--) {
                if(iteration == number)
                    return;
                matrix.RemoveColumn(y, 1);
                iteration++;
            }
        }
        
        private void CropMatrixRightByRule<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, Predicate<TMatrixEntity> predicate ) {
            for (int y = matrix.GetColumnCount() - 1; y >= 0; y--) {
                if (!matrix.CheckForExpression(predicate, y))
                    return;
                matrix.RemoveColumn(y, 1);
            }
        }

        private void CropMatrixTop<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, int number) {
            if (number > matrix.GetRowCount() || number < 0)
                throw new IndexOutOfRangeException($"Column count {number} is out of range.");

            matrix.Rows.RemoveRange(0, matrix.GetRowCount() < number
                ? matrix.GetRowCount()
                : number);
        }

        private void CropMatrixTopByRule<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, Predicate<TMatrixEntity> predicate) {
            for (int i = 0; i < matrix.GetRowCount(); i++) {
                if (matrix.Rows[i].Data.All(value => predicate(value))) {
                    matrix.Rows.RemoveAt(i);
                    i--;
                }
                else
                    return;
            }
        }
        
        private void CropMatrixBottom<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, int number) {
            if (number > matrix.GetRowCount() || number < 0)
                throw new IndexOutOfRangeException($"Column count {number} is out of range.");

            int startIndex = Math.Max(0, matrix.GetRowCount() - number);
            
            matrix.Rows.RemoveRange(startIndex, number);
        }
        
        private void CropMatrixBottomByRule<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, Predicate<TMatrixEntity> predicate) {
            for (int i = matrix.GetRowCount() - 1; i >= 0; i--) {
                if (matrix.Rows[i].Data.All(value => predicate(value)))
                    matrix.Rows.RemoveAt(i);
                else
                    return;
            }
           
        }
    }
}
