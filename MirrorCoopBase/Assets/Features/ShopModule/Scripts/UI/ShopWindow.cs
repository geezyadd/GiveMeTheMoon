using System;
using Features.MvpModule;
using Zenject;

namespace Features.ShopModule.Scripts.UI {
    public sealed class ShopWindow : FocusableWindowBehaviour, IInitializable, IDisposable {
        public ShopWindow(
            IWindowsFactory windowsFactory,
            IWindowsComponentsFinderService windowsComponentsFinderService,
            IWindowsService windowsService,
            IFocusablesService focusablesService)
            : base(windowsFactory, windowsComponentsFinderService, windowsService, focusablesService) { }
    }
}
