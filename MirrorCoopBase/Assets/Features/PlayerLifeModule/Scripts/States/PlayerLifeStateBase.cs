namespace Features.PlayerLifeModule.Scripts {
    public abstract class PlayerLifeStateBase {
        protected PlayerLifeStateBase(IPlayerLifeActor actor) =>
            Actor = actor;

        public abstract PlayerLifeState Id { get; }

        protected IPlayerLifeActor Actor { get; }

        public abstract void Enter();

        public virtual void Exit() {
        }
    }
}
