using System;
using Features.MvpModule;
using Zenject;

namespace Features.DebugModule.Scripts.UI {
    public sealed class DebugWindow : FocusableWindowBehaviour, IInitializable, IDisposable {
        public DebugWindow(
            IWindowsFactory windowsFactory,
            IWindowsComponentsFinderService windowsComponentsFinderService,
            IWindowsService windowsService,
            IFocusablesService focusablesService)
            : base(windowsFactory, windowsComponentsFinderService, windowsService, focusablesService) { }
    }
}
