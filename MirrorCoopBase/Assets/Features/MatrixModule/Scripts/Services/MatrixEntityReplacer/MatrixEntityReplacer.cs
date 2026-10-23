using System;
using Features.MatrixModule.Scripts.Exceptions;

namespace Features.MatrixModule.Scripts.Services.MatrixEntityReplacer {
    public class MatrixEntityReplacer : IMatrixEntityReplacer
    {
        public void ReplaceMatrixEntity<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, TMatrixEntity newEntity, Predicate<TMatrixEntity> predicate) {
            if(!typeof(TMatrixEntity).IsValueType && newEntity is not ICloneable)
                throw new NotValidMatrixEntityTypeException(typeof(TMatrixEntity));
            
            foreach (MatrixSavableContainer<TMatrixEntity> row in matrix.Rows)
                ReplaceMatrixRowEntity(matrix, row, newEntity, predicate);
        }
    
        private void ReplaceMatrixRowEntity<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, MatrixSavableContainer<TMatrixEntity> row, TMatrixEntity newEntity, Predicate<TMatrixEntity> predicate) {
            for (int matrixX = 0; matrixX < matrix.GetColumnCount(); matrixX++) {
                if (predicate(row.Data[matrixX])) {
                    if (typeof(TMatrixEntity).IsValueType)
                        row.Data[matrixX] = newEntity;
                    else
                        row.Data[matrixX] = (TMatrixEntity)((ICloneable)newEntity).Clone();
                }
            }
        }
    }
}
