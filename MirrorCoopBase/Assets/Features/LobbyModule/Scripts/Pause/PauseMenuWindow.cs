using System;
using Features.MvpModule;
using Features.SettingsModule.UI.Focus;
using Zenject;

namespace Features.LobbyModule.Scripts.Pause {
    // The pause menu over the lobby and the game: Continue, Settings, Leave. The game does not stop (co-op).
    public sealed class PauseMenuWindow : FocusableWindowBehaviour, IInitializable, IDisposable {
        public PauseMenuWindow(
            IWindowsFactory windowsFactory,
            IWindowsComponentsFinderService windowsComponentsFinderService,
            IWindowsService windowsService,
            IFocusablesService focusablesService)
            : base(windowsFactory, windowsComponentsFinderService, windowsService, focusablesService) { }

        protected override void OnBecomeFocusable() =>
            DefaultElementSelection.Select(FocusableElements);
    }
}
