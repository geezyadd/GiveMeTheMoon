using System;
using System.Collections.Generic;
using Features.MatrixModule.Scripts.Exceptions;
using UnityEngine;

namespace Features.MatrixModule.Scripts.Services.MatrixPathFinder {
    public class MatrixPathFinder : IMatrixPathFinder {
        private const float STRAIGHT_STEP_COST = 1f;

        private static readonly float _diagonalStepCost = Mathf.Sqrt(2f);

        public List<Vector2Int> PathFinding<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, Vector2Int startPosition, Vector2Int endPosition, bool isDiagonalMove = false, Predicate<TMatrixEntity> predicate = null) {
            if (!matrix.ValidateIndex(startPosition) || !matrix.ValidateIndex(endPosition))
                return new List<Vector2Int>();

            int cellCount = matrix.GetRowCount() * matrix.GetColumnCount();
            MatrixPathQueue openQueue = new();
            HashSet<Vector2Int> closedPositions = new();
            Dictionary<Vector2Int, float> costsSoFar = new() { [startPosition] = 0f };
            Dictionary<Vector2Int, Vector2Int> parents = new();
            openQueue.Enqueue(startPosition, EstimateCost(startPosition, endPosition, isDiagonalMove));

            while (openQueue.TryDequeue(out Vector2Int currentPosition)) {
                if (!closedPositions.Add(currentPosition))
                    continue;

                if (currentPosition == endPosition)
                    return BuildPath(parents, endPosition);

                // Every cell is closed at most once: more closed cells than the matrix has means a broken search.
                if (closedPositions.Count > cellCount)
                    throw new InfiniteLoopTypeException();

                OpenNeighbors(matrix, currentPosition, endPosition, isDiagonalMove, predicate, openQueue, closedPositions, costsSoFar, parents);
            }

            return new List<Vector2Int>();
        }

        private void OpenNeighbors<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, Vector2Int currentPosition, Vector2Int endPosition, bool isDiagonalMove, Predicate<TMatrixEntity> predicate,
                                                  MatrixPathQueue openQueue, HashSet<Vector2Int> closedPositions, Dictionary<Vector2Int, float> costsSoFar, Dictionary<Vector2Int, Vector2Int> parents) {
            float currentCost = costsSoFar[currentPosition];
            foreach (Vector2Int neighborPosition in matrix.GetNeighbors(currentPosition, isDiagonalMove, predicate)) {
                if (closedPositions.Contains(neighborPosition))
                    continue;

                float neighborCost = currentCost + GetStepCost(currentPosition, neighborPosition);
                bool isKnownPathCheaper = costsSoFar.TryGetValue(neighborPosition, out float knownCost) && knownCost <= neighborCost;
                if (isKnownPathCheaper)
                    continue;

                costsSoFar[neighborPosition] = neighborCost;
                parents[neighborPosition] = currentPosition;
                openQueue.Enqueue(neighborPosition, neighborCost + EstimateCost(neighborPosition, endPosition, isDiagonalMove));
            }
        }

        private List<Vector2Int> BuildPath(Dictionary<Vector2Int, Vector2Int> parents, Vector2Int endPosition) {
            List<Vector2Int> path = new() { endPosition };
            Vector2Int position = endPosition;
            while (parents.TryGetValue(position, out position))
                path.Add(position);

            path.Reverse();
            return path;
        }

        private float GetStepCost(Vector2Int fromPosition, Vector2Int toPosition) =>
            fromPosition.x != toPosition.x && fromPosition.y != toPosition.y
                ? _diagonalStepCost
                : STRAIGHT_STEP_COST;

        // Manhattan distance for side moves, octile distance for diagonal moves: neither overestimates the real cost,
        // so the first time the end cell is closed its path is a shortest one.
        private float EstimateCost(Vector2Int fromPosition, Vector2Int toPosition, bool isDiagonalMove) {
            int deltaX = Mathf.Abs(toPosition.x - fromPosition.x);
            int deltaY = Mathf.Abs(toPosition.y - fromPosition.y);
            if (!isDiagonalMove)
                return (deltaX + deltaY) * STRAIGHT_STEP_COST;

            int diagonalSteps = Mathf.Min(deltaX, deltaY);
            int straightSteps = Mathf.Max(deltaX, deltaY) - diagonalSteps;
            return diagonalSteps * _diagonalStepCost + straightSteps * STRAIGHT_STEP_COST;
        }
    }
}
