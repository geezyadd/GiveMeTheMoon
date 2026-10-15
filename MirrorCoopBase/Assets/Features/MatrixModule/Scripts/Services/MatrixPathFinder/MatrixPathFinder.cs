using System;
using System.Collections.Generic;
using System.Linq;
using Features.MatrixModule.Scripts.Exceptions;
using UnityEngine;

namespace Features.MatrixModule.Scripts.Services.MatrixPathFinder {
    public class MatrixPathFinder : IMatrixPathFinder {
        private const int ITERATION_CYCLE_STOP = 200;

        public List<Vector2Int> PathFinding<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, Vector2Int startPosition, Vector2Int endPosition, bool isDiagonalMove = false, Predicate<TMatrixEntity> predicate = null) {
            Node start = new(startPosition.x, startPosition.y);
            Node end = new(endPosition.x, endPosition.y);
            List<Node> openList = new();
            List<Node> closedList = new();
            List<Vector2Int> path = new();

            openList.Add(start);
            int cycleCount = 0;
            while (openList.Count > 0) {
                if (cycleCount > ITERATION_CYCLE_STOP)
                    throw new InfiniteLoopTypeException();

                Node currentNode = openList.OrderBy(n => n.TotalCost)
                    .First();
                closedList.Add(currentNode);
                openList.Remove(currentNode);

                if (currentNode.Equals(end)) {
                    while (currentNode != null) {
                        path.Add(new Vector2Int(currentNode.X, currentNode.Y));
                        currentNode = currentNode.Parent;
                    }

                    path.Reverse();
                    return path;
                }

                if (!NeighborValidator(matrix, currentNode, predicate, openList, closedList, end, isDiagonalMove))
                    return new List<Vector2Int>();

                cycleCount++;
            }

            return path;
        }

        private bool NeighborValidator<TMatrixEntity>(IMatrix<TMatrixEntity> matrix, Node currentNode, Predicate<TMatrixEntity> predicate, List<Node> openList, List<Node> closedList,Node end, bool isDiagonalMove) {
            List<Vector2Int> neighborsVectorPosition = matrix.GetNeighbors(new Vector2Int(currentNode.X, currentNode.Y), isDiagonalMove, predicate);
            List<Node> neighbors = new();
            foreach (Vector2Int position in neighborsVectorPosition)
                neighbors.Add(new Node(position.x, position.y));

            bool isCantFindPath = false;
            foreach (Node neighbor in neighbors) {
                if (openList.Any(n => n.Equals(neighbor)) && currentNode.CostSoFar > neighbor.CostSoFar) {
                    isCantFindPath = false;
                    continue;
                }
                   
                neighbor.CostSoFar = currentNode.CostSoFar;
                neighbor.HeuristicCost = (float)Math.Sqrt(Math.Pow(end.X - neighbor.X, 2) + Math.Pow(end.Y - neighbor.Y, 2));
                neighbor.TotalCost = neighbor.CostSoFar + neighbor.HeuristicCost;
                neighbor.Parent = currentNode;

                if (!openList.Any(n => n.Equals(neighbor)) && !closedList.Any(n => n.Equals(neighbor)))
                    openList.Add(neighbor);

                isCantFindPath = true;
            }

            return isCantFindPath;
        }
    }
}
