using System;
using Features.InputModule.Realization.Scripts.Generated;
using Zenject;

namespace Features.SettingsModule.UI {
    // Esc asks an open settings window to close (in the main menu and over the pause menu alike).
    public sealed class SettingsWindowEscapeSystem : IInitializable, IDisposable {
        private readonly IInputService _inputService;
        private readonly ISettingsWindowService _settingsWindowService;

        public SettingsWindowEscapeSystem(IInputService inputService, ISettingsWindowService settingsWindowService) {
            _inputService = inputService;
            _settingsWindowService = settingsWindowService;
        }

        public void Initialize() =>
            _inputService.CloseWindow.Performed += OnCloseWindowPressed;

        public void Dispose() =>
            _inputService.CloseWindow.Performed -= OnCloseWindowPressed;

        private void OnCloseWindowPressed() {
            if (_settingsWindowService.IsOpen)
                _settingsWindowService.RequestClose();
        }
    }
}
