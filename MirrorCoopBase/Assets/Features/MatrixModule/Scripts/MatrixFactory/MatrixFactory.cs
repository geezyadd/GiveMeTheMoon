using System.Collections.Generic;
using Features.MatrixModule.Scripts.Services.ExpandMatrix;
using Features.MatrixModule.Scripts.Services.MatrixCropper;
using Features.MatrixModule.Scripts.Services.MatrixEntityReplacer;
using Features.MatrixModule.Scripts.Services.MatrixEntitySorter;
using Features.MatrixModule.Scripts.Services.MatrixInsertShaper;
using Features.MatrixModule.Scripts.Services.MatrixPathFinder;

namespace Features.MatrixModule.Scripts.MatrixFactory {
    public class MatrixFactory : IMatrixFactory {
        public IMatrix<TMatrixEntity> Create<TMatrixEntity>(int width, int height, TMatrixEntity fillEntity) =>
            new Matrix<TMatrixEntity>(width, height, new MatrixCropper(), new MatrixExpander(this), new MatrixInsertShaper(), new MatrixEntityReplacer(), new MatrixPathFinder(), new MatrixEntitySorter(), fillEntity);
        
        public IMatrix<TMatrixEntity> Create<TMatrixEntity>(List<MatrixSavableContainer<TMatrixEntity>> savableContainers) =>
            new Matrix<TMatrixEntity>(savableContainers, new MatrixCropper(), new MatrixExpander(this), new MatrixInsertShaper(), new MatrixEntityReplacer(), new MatrixPathFinder(), new MatrixEntitySorter());
    }
}
