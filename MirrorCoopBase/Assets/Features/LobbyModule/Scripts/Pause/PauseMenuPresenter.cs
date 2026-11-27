using Features.MvpModule;
using Features.SettingsModule.UI;

namespace Features.LobbyModule.Scripts.Pause {
    // Continue closes the pause menu; Settings opens the settings window over it.
    public sealed class PauseMenuPresenter : PresenterBehaviour<PauseMenuViewBase> {
        private readonly IPauseMenuService _pauseMenuService;
        private readonly ISettingsWindowService _settingsWindowService;

        public PauseMenuPresenter(IPauseMenuService pauseMenuService, ISettingsWindowService settingsWindowService) {
            _pauseMenuService = pauseMenuService;
            _settingsWindowService = settingsWindowService;
        }

        protected override void OnViewSet() {
            View.OnContinueClicked += OnContinueClicked;
            View.OnSettingsClicked += OnSettingsClicked;
        }

        protected override void OnDisposed() {
            View.OnContinueClicked -= OnContinueClicked;
            View.OnSettingsClicked -= OnSettingsClicked;
        }

        private void OnContinueClicked() =>
            _pauseMenuService.Close();

        private void OnSettingsClicked() =>
            _settingsWindowService.Open();
    }
}
