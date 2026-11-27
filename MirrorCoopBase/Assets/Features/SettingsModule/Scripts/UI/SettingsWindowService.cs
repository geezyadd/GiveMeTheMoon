using Features.MvpModule;

namespace Features.SettingsModule.UI {
    public sealed class SettingsWindowService : ISettingsWindowService {
        private readonly IWindowsService _windowsService;

        public SettingsWindowService(IWindowsService windowsService) =>
            _windowsService = windowsService;

        public bool IsOpen =>
            _windowsService.GetWindow(typeof(SettingsWindow)) is { WindowStatus: not WindowStatus.Closed };

        public void Open() {
            if (IsOpen == false)
                _windowsService.OpenWindow<SettingsWindow>();
        }

        public void RequestClose() {
            if (_windowsService.GetWindow(typeof(SettingsWindow)) is not WindowBehaviour window
                || window.WindowStatus == WindowStatus.Closed)
                return;

            if (window.TryGetPresenter(out SettingsWindowPresenter presenter))
                presenter.RequestClose();
            else
                _windowsService.CloseWindow<SettingsWindow>();
        }
    }
}
