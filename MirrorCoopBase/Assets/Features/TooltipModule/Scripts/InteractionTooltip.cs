using UnityEngine;

namespace Features.TooltipModule.Scripts {
    public sealed class InteractionTooltip : MonoBehaviour {
        [SerializeField] private TooltipData _data;
        [SerializeField] private TooltipData _readyData;
        [SerializeField] private Component _source;

        public bool HasSource => _source != null;

        public bool Matches(Component source) =>
            _source == null || _source == source;

        public TooltipData Select(bool isReady) =>
            isReady && _readyData != null ? _readyData : _data;

        public static InteractionTooltip FindFor(Component source) {
            if (source == null)
                return null;

            InteractionTooltip[] tooltips = source.GetComponents<InteractionTooltip>();
            InteractionTooltip fallback = null;
            for (int i = 0; i < tooltips.Length; i++) {
                InteractionTooltip tooltip = tooltips[i];
                if (tooltip.Matches(source) == false)
                    continue;

                if (tooltip.HasSource)
                    return tooltip;

                if (fallback == null)
                    fallback = tooltip;
            }

            return fallback;
        }
    }
}
