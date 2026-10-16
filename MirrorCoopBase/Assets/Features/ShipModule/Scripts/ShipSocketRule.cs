namespace Features.ShipModule.Scripts {
    public sealed class ShipSocketRule {
        private readonly ShipModuleType _acceptedType;
        private readonly int _unlockLoop;
        private readonly string _startingModuleId;

        public ShipSocketRule(ShipModuleType acceptedType, int unlockLoop, string startingModuleId) {
            _acceptedType = acceptedType;
            _unlockLoop = unlockLoop;
            _startingModuleId = startingModuleId;
        }

        public bool IsUnlocked(ShipSocketRunState run) =>
            run.LoopIndex >= _unlockLoop;

        public bool CanInstall(bool occupied, ShipModuleType type, ShipSocketRunState run) =>
            occupied == false && type == _acceptedType && IsUnlocked(run) && IsBuildPhase(run);

        public bool CanUninstall(bool occupied, bool isRemovable, ShipSocketRunState run) =>
            occupied && isRemovable && IsBuildPhase(run);

        // What the socket holds after a ship reset (run start and end, aborted flight): its starting module or nothing.
        public ShipSocketState CreateResetState() =>
            string.IsNullOrEmpty(_startingModuleId)
                ? default
                : new ShipSocketState { Occupied = true, ModuleId = _startingModuleId };

        private static bool IsBuildPhase(ShipSocketRunState run) =>
            run.Phase == ShipRunPhase.Build;
    }
}
