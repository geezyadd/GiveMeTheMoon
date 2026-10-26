using System;
using Features.MvpModule;

namespace Features.DebugModule.Scripts.UI {
    public abstract class DebugToggleViewBase : ViewBehaviour {
        public event Action OnClicked;

        public abstract void ShowPanelOpen(bool isOpen);

        protected void InvokeClicked() =>
            OnClicked?.Invoke();
    }
}
