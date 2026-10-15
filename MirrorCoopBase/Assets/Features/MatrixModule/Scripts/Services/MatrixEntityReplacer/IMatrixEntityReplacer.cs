using System;

namespace Features.MatrixModule.Scripts.Services.MatrixEntityReplacer {
    public interface IMatrixEntityReplacer {
        public void ReplaceMatrixEntity<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, TMatrixEntity newEntity, Predicate<TMatrixEntity> predicate);
    }
}
