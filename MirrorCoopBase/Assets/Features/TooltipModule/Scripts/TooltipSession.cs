using Features.GameCoreModule.Scripts;
using Features.GrabModule.Scripts;

namespace Features.TooltipModule.Scripts {
    public sealed class TooltipSession : IGameplaySession {
        private readonly HoveredInteractableModel _hoveredInteractableModel;

        public TooltipSession(HoveredInteractableModel hoveredInteractableModel) =>
            _hoveredInteractableModel = hoveredInteractableModel;

        public void CleanupGameplay() =>
            _hoveredInteractableModel.Clear();

        public void RestartGameplay() =>
            _hoveredInteractableModel.Clear();
    }
}
