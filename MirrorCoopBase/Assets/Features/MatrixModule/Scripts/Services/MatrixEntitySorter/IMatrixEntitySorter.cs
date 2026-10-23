using System;
using System.Collections.Generic;

namespace Features.MatrixModule.Scripts.Services.MatrixEntitySorter {
    public interface IMatrixEntitySorter {
        public List<TMatrixEntity> GetSortedMatrixEntities<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, Comparison<TMatrixEntity> comparison, bool isAscending);
    }
}
