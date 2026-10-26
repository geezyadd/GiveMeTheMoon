using Features.WeaponModule.Scripts.Parts.Delivery;

namespace Features.WeaponModule.Editor.Tests {
    // Returns the given values in a loop.
    public sealed class SequenceWeaponRandom : IWeaponRandom {
        private readonly float[] _values;

        private int _next;

        public SequenceWeaponRandom(params float[] values) =>
            _values = values;

        public float NextUnitValue() {
            float value = _values[_next];
            _next = (_next + 1) % _values.Length;
            return value;
        }
    }
}
