using Features.ShipModule.Scripts.Hull;
using UnityEngine;

namespace Features.ShipModule.Scripts.Editor.Tests {
    public sealed class FakeShipHullStat : IShipHullStat {
        public float Current { get; private set; }
        public float Max { get; }
        public float Armor { get; }
        public int RepairCount { get; private set; }

        public FakeShipHullStat(float max, float armor) {
            Max = max;
            Armor = armor;
            Current = max;
        }

        public void ServerSubtract(float amount) =>
            Current = Mathf.Max(0f, Current - amount);

        public void ServerAdd(float amount) =>
            Current = Mathf.Min(Max, Current + amount);

        public void ServerRepair() {
            Current = Max;
            RepairCount++;
        }
    }
}
