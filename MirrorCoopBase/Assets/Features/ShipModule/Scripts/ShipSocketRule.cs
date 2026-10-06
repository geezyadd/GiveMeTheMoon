namespace Features.ShipModule.Scripts {
    public sealed class ShipSocketRule {
        private readonly ShipModuleType _acceptedType;
        private readonly int _unlockLoop;

        public ShipSocketRule(ShipModuleType acceptedType, int unlockLoop) {
            _acceptedType = acceptedType;
            _unlockLoop = unlockLoop;
        }

        public bool IsUnlocked(ShipSocketRunState run) =>
            run.LoopIndex >= _unlockLoop;

        public bool CanInstall(bool occupied, ShipModuleType type, ShipSocketRunState run) =>
            occupied == false && type == _acceptedType && IsUnlocked(run) && IsBuildPhase(run);

        public bool CanUninstall(bool occupied, ShipSocketRunState run) =>
            occupied && IsBuildPhase(run) && CanRemoveModule(_acceptedType);

        private static bool IsBuildPhase(ShipSocketRunState run) =>
            run.Phase == ShipRunPhase.Build;

        private static bool CanRemoveModule(ShipModuleType type) =>
            type == ShipModuleType.Engine || type == ShipModuleType.Radar;
    }
}
