using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.MatrixModule.Scripts.Services.MatrixPathFinder {
    public interface IMatrixPathFinder {
        public List<Vector2Int> PathFinding<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, Vector2Int startPosition, Vector2Int endPosition, bool isDiagonalMove = false,  Predicate<TMatrixEntity> predicate = null);
    }
}
