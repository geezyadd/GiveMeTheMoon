using System.Collections.Generic;
using UnityEngine;

namespace Features.MatrixModule.Scripts.Services.MatrixPathFinder {
    internal sealed class MatrixPathQueue {
        private readonly List<MatrixPathQueueEntry> _entries = new();

        private long _enqueuedCount;

        public void Enqueue(Vector2Int position, float priority) {
            _entries.Add(new MatrixPathQueueEntry(position, priority, _enqueuedCount));
            _enqueuedCount++;
            SiftUp(_entries.Count - 1);
        }

        public bool TryDequeue(out Vector2Int position) {
            position = default;
            if (_entries.Count == 0)
                return false;

            position = _entries[0].Position;
            int lastIndex = _entries.Count - 1;
            _entries[0] = _entries[lastIndex];
            _entries.RemoveAt(lastIndex);
            SiftDown(0);
            return true;
        }

        private void SiftUp(int index) {
            while (index > 0) {
                int parentIndex = (index - 1) / 2;
                if (!_entries[index].IsBefore(_entries[parentIndex]))
                    return;

                Swap(index, parentIndex);
                index = parentIndex;
            }
        }

        private void SiftDown(int index) {
            int firstIndex = GetFirstOfFamily(index);
            while (firstIndex != index) {
                Swap(index, firstIndex);
                index = firstIndex;
                firstIndex = GetFirstOfFamily(index);
            }
        }

        private int GetFirstOfFamily(int parentIndex) {
            int firstIndex = GetEarlier(parentIndex, 2 * parentIndex + 1);
            return GetEarlier(firstIndex, 2 * parentIndex + 2);
        }

        private int GetEarlier(int index, int candidateIndex) =>
            candidateIndex < _entries.Count && _entries[candidateIndex].IsBefore(_entries[index])
                ? candidateIndex
                : index;

        private void Swap(int firstIndex, int secondIndex) =>
            (_entries[firstIndex], _entries[secondIndex]) = (_entries[secondIndex], _entries[firstIndex]);
    }
}
