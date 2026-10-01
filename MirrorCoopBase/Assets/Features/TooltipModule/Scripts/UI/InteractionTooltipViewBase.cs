using Features.MvpModule;

namespace Features.TooltipModule.Scripts.UI {
    public abstract class InteractionTooltipViewBase : ViewBehaviour {
        public abstract void SetContent(string title, string description, string actionHint);
    }
}
