using System;

namespace Features.MatrixModule.Scripts.Services.MatrixCropper {
    public interface IMatrixCropper {
        public void CropMatrix<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, MatrixOperationDirection matrixOperationDirection, int number);
        public void CropMatrixByRule<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, MatrixOperationDirection matrixOperationDirection, Predicate<TMatrixEntity> predicate);
    }
}
