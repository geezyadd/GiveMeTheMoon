using System;

namespace Features.TooltipModule.Scripts {
    // A tooltip whose text depends on live state: the provider picks the data for an object it knows and fills the
    // description's placeholders; OnStateChanged asks the shown tooltip to refresh.
    public interface ITooltipStateProvider {
        public event Action OnStateChanged;

        public bool TrySelect(InteractionTooltip tooltip, out TooltipData data, out string description);
    }
}
