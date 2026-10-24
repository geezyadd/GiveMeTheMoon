using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;

namespace Features.ShipModule.Scripts.Impacts {
    // The large rocks the ship touched lately: a rock hits again only after its contact ended and the cooldown
    // passed. A contact that goes on keeps its rock remembered.
    public sealed class ShipRockContactCooldowns {
        private readonly List<FieldRockId> _rocks = new();
        private readonly List<float> _lastContactTimes = new();

        public int Count => _rocks.Count;

        // Records a contact at this time; true when it is a new hit.
        public bool RecordContact(FieldRockId id, float time, float cooldownSeconds) {
            int index = _rocks.IndexOf(id);
            if (index < 0) {
                _rocks.Add(id);
                _lastContactTimes.Add(time);
                return true;
            }

            bool isCooledDown = time - _lastContactTimes[index] > cooldownSeconds;
            _lastContactTimes[index] = time;
            return isCooledDown;
        }

        public void ForgetCooledDown(float time, float cooldownSeconds) {
            for (int i = _rocks.Count - 1; i >= 0; i--) {
                if (time - _lastContactTimes[i] <= cooldownSeconds)
                    continue;

                _rocks.RemoveAt(i);
                _lastContactTimes.RemoveAt(i);
            }
        }

        public void Clear() {
            _rocks.Clear();
            _lastContactTimes.Clear();
        }
    }
}
