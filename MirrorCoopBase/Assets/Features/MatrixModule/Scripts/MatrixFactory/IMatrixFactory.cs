using System.Collections.Generic;

namespace Features.MatrixModule.Scripts.MatrixFactory {
    public interface IMatrixFactory {
        public IMatrix<TMatrixEntity> Create<TMatrixEntity>(int width, int height, TMatrixEntity fillEntity);
        public IMatrix<TMatrixEntity> Create<TMatrixEntity>(List<MatrixSavableContainer<TMatrixEntity>> savableContainers);
    }
}
