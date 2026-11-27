using System;
using Features.MvpModule;
using Features.SettingsModule.UI.Focus;
using Zenject;

namespace Features.SettingsModule.UI {
    // The settings window: tabs of settings, Apply / Cancel / Reset tab. Opened from the main menu and the pause menu
    // through ISettingsWindowService.
    public sealed class SettingsWindow : FocusableWindowBehaviour, IInitializable, IDisposable {
        public SettingsWindow(
            IWindowsFactory windowsFactory,
            IWindowsComponentsFinderService windowsComponentsFinderService,
            IWindowsService windowsService,
            IFocusablesService focusablesService)
            : base(windowsFactory, windowsComponentsFinderService, windowsService, focusablesService) { }

        protected override void OnBecomeFocusable() =>
            DefaultElementSelection.Select(FocusableElements);
    }
}
