using System;
using System.Collections.Generic;

namespace Features.MatrixModule.Scripts.Services.MatrixInsertShaper {
    public interface IMatrixInsertShaper {
        public List<TMatrixEntity> InsertShapeMatrix<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, IMatrix<TMatrixEntity> shape, int x, int y, Predicate<TMatrixEntity> matrixPredicate = null, Predicate<TMatrixEntity> shapePredicate = null);
        public List<TMatrixEntity> SilentInsertShapeMatrix<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, IMatrix<TMatrixEntity> shape, int x, int y, Predicate<TMatrixEntity> matrixPredicate = null, Predicate<TMatrixEntity> shapePredicate = null);
        public bool TryInsertShapeMatrix<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, IMatrix<TMatrixEntity> shape, int x, int y, out List<TMatrixEntity> insertedMatrixEntities, Predicate<TMatrixEntity> matrixPredicate = null, Predicate<TMatrixEntity> shapePredicate = null);
        public bool TryInsertFullShapeMatrix<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, IMatrix<TMatrixEntity> shape, int x, int y, out List<TMatrixEntity> matrixEntities, Predicate<TMatrixEntity> matrixPredicate = null, Predicate<TMatrixEntity> shapePredicate = null);
    }
}
