using System;
using Features.MvpModule;
using Zenject;

namespace Features.SonarModule.Scripts.UI {
    public sealed class SonarWindow : FocusableWindowBehaviour, IInitializable, IDisposable {
        public SonarWindow(
            IWindowsFactory windowsFactory,
            IWindowsComponentsFinderService windowsComponentsFinderService,
            IWindowsService windowsService,
            IFocusablesService focusablesService)
            : base(windowsFactory, windowsComponentsFinderService, windowsService, focusablesService) { }
    }
}
