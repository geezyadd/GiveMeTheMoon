using Features.MvpModule;
using Features.SettingsModule.UI;

namespace Features.MenuModule.Scripts {
    // Opens the settings window over the main menu.
    public sealed class MenuSettingsPresenter : PresenterBehaviour<MenuSettingsViewBase> {
        private readonly ISettingsWindowService _settingsWindowService;

        public MenuSettingsPresenter(ISettingsWindowService settingsWindowService) =>
            _settingsWindowService = settingsWindowService;

        protected override void OnViewSet() =>
            View.OnSettingsClicked += OnSettingsClicked;

        protected override void OnDisposed() =>
            View.OnSettingsClicked -= OnSettingsClicked;

        private void OnSettingsClicked() =>
            _settingsWindowService.Open();
    }
}
