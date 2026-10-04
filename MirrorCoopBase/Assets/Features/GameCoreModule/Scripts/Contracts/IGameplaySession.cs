namespace Features.GameCoreModule.Contracts {
    public interface IGameplaySession {
        void CleanupGameplay();
        void RestartGameplay();
    }
}
