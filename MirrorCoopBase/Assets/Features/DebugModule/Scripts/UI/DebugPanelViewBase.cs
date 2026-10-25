using System;
using Features.DebugModule.Contracts;
using Features.MvpModule;

namespace Features.DebugModule.Scripts.UI {
    public abstract class DebugPanelViewBase : ViewBehaviour {
        public event Action<DebugActionDefinition, float> OnActionClicked;

        public abstract void ShowPanel(bool isShown);
        public abstract void ClearActions();
        public abstract void AddSection(string title);
        public abstract void AddAction(DebugActionDefinition action);
        // Null: the action can run.
        public abstract void ShowAvailability(DebugActionDefinition action, string unavailableReason);

        protected void InvokeActionClicked(DebugActionDefinition action, float value) =>
            OnActionClicked?.Invoke(action, value);
    }
}
