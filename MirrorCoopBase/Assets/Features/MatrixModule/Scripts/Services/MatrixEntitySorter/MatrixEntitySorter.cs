using System;
using System.Collections.Generic;

namespace Features.MatrixModule.Scripts.Services.MatrixEntitySorter {
    public class MatrixEntitySorter : IMatrixEntitySorter
    {
        public List<TMatrixEntity> GetSortedMatrixEntities<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, Comparison<TMatrixEntity> comparison, bool isAscending) {
            List<TMatrixEntity> sorted = new();
            foreach (MatrixSavableContainer<TMatrixEntity> row in matrix.Rows)
                foreach (TMatrixEntity entity in row.Data)
                    sorted.Add(entity);

            sorted.Sort(comparison);
            if (!isAscending)
                sorted.Reverse();

            return sorted;
        }
    }
}
