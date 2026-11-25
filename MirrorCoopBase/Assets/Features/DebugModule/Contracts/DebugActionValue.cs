using System;

namespace Features.DebugModule.Contracts {
    // The value next to an action's button, filled with the default when the window opens: a number field (a damage
    // amount...), or, with a range, a slider that runs the action at once on every move and opens at the current value.
    public sealed class DebugActionValue {
        private readonly Func<float> _readCurrent;

        public string Label { get; }
        public float DefaultValue { get; }
        public bool HasRange { get; }
        public float Min { get; }
        public float Max { get; }

        // What the field or slider shows when the window opens: the current value when there is one.
        public float ShownValue => _readCurrent != null ? _readCurrent() : DefaultValue;

        public DebugActionValue(string label, float defaultValue) {
            Label = label;
            DefaultValue = defaultValue;
        }

        public DebugActionValue(string label, float defaultValue, float min, float max, Func<float> readCurrent) {
            Label = label;
            DefaultValue = defaultValue;
            HasRange = true;
            Min = min;
            Max = max;
            _readCurrent = readCurrent;
        }
    }
}
