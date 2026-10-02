namespace Features.PlayerLifeModule.Scripts {
    public sealed class DeadState : PlayerLifeStateBase {
        private readonly IDamageable _damageable;

        public DeadState(IPlayerLifeActor actor, IDamageable damageable) : base(actor) =>
            _damageable = damageable;

        public override PlayerLifeState Id =>
            PlayerLifeState.Dead;

        public override void Enter() {
            Actor.ServerSetLifeState(PlayerLifeState.Dead);
            Actor.ServerApplyDeath();
        }

        // Leaving Dead is always a revive: the health the death took is given back here.
        public override void Exit() =>
            _damageable.ServerRestore();
    }
}
