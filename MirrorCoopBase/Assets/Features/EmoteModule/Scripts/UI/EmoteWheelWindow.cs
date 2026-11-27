using System;
using Features.MvpModule;
using Zenject;

namespace Features.EmoteModule.UI {
    public sealed class EmoteWheelWindow : FocusableWindowBehaviour, IInitializable, IDisposable {
        public EmoteWheelWindow(
            IWindowsFactory windowsFactory,
            IWindowsComponentsFinderService windowsComponentsFinderService,
            IWindowsService windowsService,
            IFocusablesService focusablesService)
            : base(windowsFactory, windowsComponentsFinderService, windowsService, focusablesService) { }
    }
}
