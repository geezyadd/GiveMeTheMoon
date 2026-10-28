namespace Features.ShipImpactModule.Scripts {
    // Server: the hull's sweep of the current physics step, the same for every system that checks the hull against
    // the field in that step (rocks, ore). False outside a checked flight phase.
    public interface IShipHullStepSource {
        public bool TryGetStep(out ShipHullStep step);
    }
}
