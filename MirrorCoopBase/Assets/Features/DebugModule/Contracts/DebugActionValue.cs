namespace Features.DebugModule.Contracts {
    // The number field next to an action's button (a damage amount...), filled with the default when the window opens.
    public sealed class DebugActionValue {
        public string Label { get; }
        public float DefaultValue { get; }

        public DebugActionValue(string label, float defaultValue) {
            Label = label;
            DefaultValue = defaultValue;
        }
    }
}
