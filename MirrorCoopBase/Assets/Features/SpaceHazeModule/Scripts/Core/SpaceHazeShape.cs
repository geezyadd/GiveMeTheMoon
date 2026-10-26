namespace Features.SpaceHazeModule.Scripts.Core {
    // The haze this frame: where it starts, where a thing is hidden, where the backdrop starts and ends, in metres
    // from the camera, and how strong it is (0: no haze).
    public readonly struct SpaceHazeShape {
        public float ClearMeters { get; }
        public float HiddenMeters { get; }
        public float BackdropStartMeters { get; }
        public float BackdropEndMeters { get; }
        public float Strength { get; }

        public SpaceHazeShape(float clearMeters, float hiddenMeters, float backdropStartMeters, float backdropEndMeters, float strength) {
            ClearMeters = clearMeters;
            HiddenMeters = hiddenMeters;
            BackdropStartMeters = backdropStartMeters;
            BackdropEndMeters = backdropEndMeters;
            Strength = strength;
        }
    }
}
