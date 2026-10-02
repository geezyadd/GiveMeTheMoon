namespace Features.PlayerLifeModule.Scripts {
    public interface IPlayerLifeStateMachineFactory {
        public IPlayerLifeStateMachine Create(IPlayerLifeActor actor, IDamageable damageable);
    }
}
