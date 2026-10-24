using UnityEngine;

namespace Features.SonarModule.Scripts.Sources {
    // The small rocks of one debris cell counted so far: how many and the sum of their middles.
    public readonly struct DebrisCellSum {
        public Vector3 PositionSum { get; }
        public int Count { get; }

        public DebrisCellSum(Vector3 positionSum, int count) {
            PositionSum = positionSum;
            Count = count;
        }

        public DebrisCellSum Add(Vector3 position) =>
            new(PositionSum + position, Count + 1);
    }
}
