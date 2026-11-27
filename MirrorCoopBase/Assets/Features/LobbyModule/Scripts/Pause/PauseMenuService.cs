using Features.CameraModule.Scripts.Models;
using Features.InputModule.Realization.Scripts.Generated;
using Features.MvpModule;

namespace Features.LobbyModule.Scripts.Pause {
    public sealed class PauseMenuService : IPauseMenuService {
        private readonly IWindowsService _windowsService;
        private readonly IInputService _inputService;
        private readonly CursorModel _cursorModel;

        public PauseMenuService(IWindowsService windowsService, IInputService inputService, CursorModel cursorModel) {
            _windowsService = windowsService;
            _inputService = inputService;
            _cursorModel = cursorModel;
        }

        public bool IsOpen { get; private set; }

        public void Open() {
            if (IsOpen)
                return;

            IsOpen = true;
            _windowsService.OpenWindow<PauseMenuWindow>();
            _inputService.DisableMovementMap();
            _cursorModel.RequestFreeCursor();
        }

        public void Close() {
            if (IsOpen == false)
                return;

            IsOpen = false;
            IWindow window = _windowsService.GetWindow(typeof(PauseMenuWindow));
            if (window != null && window.WindowStatus != WindowStatus.Closed)
                _windowsService.CloseWindow<PauseMenuWindow>();

            _inputService.EnableMovementMap();
            _cursorModel.ReleaseFreeCursor();
        }
    }
}
