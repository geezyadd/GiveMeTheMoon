using System;
using System.Collections.Generic;

namespace Features.MatrixModule.Scripts {
    [Serializable]
    public class MatrixSavableContainer<TMatrixEntity> {
        public List<TMatrixEntity> Data;
        
        public IEnumerator<TMatrixEntity> GetEnumerator() =>
            Data.GetEnumerator();
    }
}
