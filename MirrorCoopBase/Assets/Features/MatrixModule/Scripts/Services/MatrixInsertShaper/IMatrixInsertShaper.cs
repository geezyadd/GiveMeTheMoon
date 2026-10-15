using System;
using System.Collections.Generic;

namespace Features.MatrixModule.Scripts.Services.MatrixInsertShaper {
    public interface IMatrixInsertShaper {
        /// <summary>
        /// 
        /// </summary>
        /// <returns>All inserted matrix entities</returns>
        List<TMatrixEntity> InsertShapeMatrix<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, IMatrix<TMatrixEntity> shape, int x, int y, Predicate<TMatrixEntity> matrixPredicate = null, Predicate<TMatrixEntity> shapePredicate = null);
        /// <summary>
        /// 
        /// </summary>
        /// <returns>All inserted matrix entities</returns>
        List<TMatrixEntity> SilentInsertShapeMatrix<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, IMatrix<TMatrixEntity> shape, int x, int y, Predicate<TMatrixEntity> matrixPredicate = null, Predicate<TMatrixEntity> shapePredicate = null);
        bool TryInsertShapeMatrix<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, IMatrix<TMatrixEntity> shape, int x, int y, out List<TMatrixEntity> insertedMatrixEntities, Predicate<TMatrixEntity> matrixPredicate = null, Predicate<TMatrixEntity> shapePredicate = null);
        bool TryInsertFullShapeMatrix<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, IMatrix<TMatrixEntity> shape, int x, int y, out List<TMatrixEntity> matrixEntities, Predicate<TMatrixEntity> matrixPredicate = null, Predicate<TMatrixEntity> shapePredicate = null);
    }
}
