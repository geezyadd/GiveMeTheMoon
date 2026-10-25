using System;

namespace Features.DebugModule.Contracts {
    // One button of the debug window. The availability check returns why the action cannot run now (host only, not in
    // flight...) or null when it can; it is asked every frame while the window is open, so it must not allocate.
    public sealed class DebugActionDefinition {
        private readonly Func<string> _readUnavailableReason;
        private readonly Action _run;
        private readonly Action<float> _runWithValue;

        public string Section { get; }
        public string Label { get; }
        public string Description { get; }
        public DebugActionValue Value { get; }
        public bool HasValue => Value != null;

        public DebugActionDefinition(string section, string label, string description, Func<string> readUnavailableReason, Action run) {
            Section = section;
            Label = label;
            Description = description;
            _readUnavailableReason = readUnavailableReason;
            _run = run;
        }

        public DebugActionDefinition(string section, string label, string description, Func<string> readUnavailableReason, DebugActionValue value, Action<float> run) {
            Section = section;
            Label = label;
            Description = description;
            _readUnavailableReason = readUnavailableReason;
            Value = value;
            _runWithValue = run;
        }

        public string ReadUnavailableReason() =>
            _readUnavailableReason();

        public void Run(float value) {
            if (HasValue)
                _runWithValue(value);
            else
                _run();
        }
    }
}
