namespace Features.PlayerLifeModule.Scripts {
    public sealed class AliveState : PlayerLifeStateBase {
        public AliveState(IPlayerLifeActor actor) : base(actor) {
        }

        public override PlayerLifeState Id =>
            PlayerLifeState.Alive;

        public override void Enter() =>
            Actor.ServerSetLifeState(PlayerLifeState.Alive);
    }
}
