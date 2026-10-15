namespace Features.MatrixModule.Scripts.Services.ExpandMatrix {
    public interface IMatrixExpander {
        public void ExpandMatrix<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, MatrixOperationDirection matrixOperationDirection, int number, TMatrixEntity fillEntity = default);
    }
}
