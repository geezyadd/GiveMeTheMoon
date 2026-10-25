using Features.GrabModule.Scripts;
using Features.ShipDamageModule.Scripts.Damage;

namespace Features.ShipDamageModule.Scripts.Repair {
    // The repair tool in a hand, if it repairs the asked kind. Remembers the last held item, so asking every frame
    // does not look the component up again.
    public sealed class HeldRepairTool {
        private Grabbable _cachedHeld;
        private RepairTool _cachedTool;

        public bool TryGet(GrabController grab, ShipRepairKind kind, out RepairToolDefinition tool) {
            tool = null;
            RepairTool held = Resolve(grab);
            if (held == null || held.Definition.Repairs != kind)
                return false;

            tool = held.Definition;
            return true;
        }

        private RepairTool Resolve(GrabController grab) {
            Grabbable held = grab.Held;
            if (held == _cachedHeld)
                return _cachedTool;

            _cachedHeld = held;
            _cachedTool = held != null ? held.GetComponent<RepairTool>() : null;
            return _cachedTool;
        }
    }
}
