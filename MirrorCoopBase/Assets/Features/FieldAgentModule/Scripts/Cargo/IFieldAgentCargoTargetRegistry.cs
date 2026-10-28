namespace Features.FieldAgentModule.Scripts.Cargo {
    // Server: where a cargo target joins while it lies on board and leaves when it is gone.
    public interface IFieldAgentCargoTargetRegistry {
        public void Add(IFieldAgentCargoTarget cargoTarget);
        public void Remove(IFieldAgentCargoTarget cargoTarget);
    }
}
