namespace Features.PlayerLifeModule.Scripts {
    public sealed class PlayerLifeStateMachineFactory : IPlayerLifeStateMachineFactory {
        public IPlayerLifeStateMachine Create(IPlayerLifeActor actor, IDamageable damageable) =>
            new PlayerLifeStateMachine(actor, damageable);
    }
}
