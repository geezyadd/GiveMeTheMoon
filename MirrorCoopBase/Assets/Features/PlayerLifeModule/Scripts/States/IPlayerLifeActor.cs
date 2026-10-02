namespace Features.PlayerLifeModule.Scripts {
    // What the server-side life state machine drives on one player.
    public interface IPlayerLifeActor {
        PlayerLifeState LifeState { get; }
        void ServerSetLifeState(PlayerLifeState state);
        void ServerApplyDeath();
    }
}
