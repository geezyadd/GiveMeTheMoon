using System;
using UnityEngine;

namespace Features.NetworkModelModule.Scripts {
    [Serializable]
    public struct NetworkModelSamplePoint : IEquatable<NetworkModelSamplePoint> {
        public float X;
        public float Y;

        public bool Equals(NetworkModelSamplePoint other) =>
            X.Equals(other.X) && Y.Equals(other.Y);

        public override bool Equals(object obj) =>
            obj is NetworkModelSamplePoint other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(X, Y);
    }
}
