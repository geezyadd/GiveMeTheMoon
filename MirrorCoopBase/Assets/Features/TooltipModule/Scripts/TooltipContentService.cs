using System.Collections.Generic;
using Features.InputModule.Realization.Scripts.Generated;
using Features.ShipModule.Scripts;
using Features.ShopModule.Scripts.Core;
using Features.ShopModule.Scripts.Data;
using UnityEngine.InputSystem;

namespace Features.TooltipModule.Scripts {
    public sealed class TooltipContentService : ITooltipContentService {
        private const string KEYBOARD_GROUP = "Keyboard and Mouse";
        private const string DEFAULT_USE_KEY = "E";
        private const string HINT_FORMAT = "[{0}] {1}";
        private const char CONFIGURED_HINT_PREFIX = '[';

        private readonly IShopItemStatsService _shopItemStatsService;
        private readonly InputActions _inputActions;
        private string _useKey;

        public TooltipContentService(IShopItemStatsService shopItemStatsService, InputActions inputActions) {
            _shopItemStatsService = shopItemStatsService;
            _inputActions = inputActions;
        }

        public TooltipContent Build(InteractionTooltip tooltip, bool isReady) {
            TooltipData data = tooltip.Select(isReady);
            return new TooltipContent(data.Title, BuildDescription(data, tooltip), BuildActionHint(data));
        }

        private string BuildDescription(TooltipData data, InteractionTooltip tooltip) {
            ShipItem item = tooltip.GetComponent<ShipItem>();
            if (item == null)
                return data.Description;

            IReadOnlyList<ShopItemStat> stats = _shopItemStatsService.GetStats(item);
            if (stats.Count == 0)
                return data.Description;

            return data.Description + "\n" + _shopItemStatsService.FormatStats(item);
        }

        private string BuildActionHint(TooltipData data) {
            string hint = data.ActionHint;
            if (string.IsNullOrEmpty(hint))
                return string.Empty;

            if (hint[0] == CONFIGURED_HINT_PREFIX)
                return hint;

            return string.Format(HINT_FORMAT, ReadUseKey(), hint);
        }

        private string ReadUseKey() {
            if (_useKey != null)
                return _useKey;

            InputAction grab = _inputActions.MovementMap.Grab;
            int bindingIndex = grab.GetBindingIndex(InputBinding.MaskByGroup(KEYBOARD_GROUP));
            if (bindingIndex < 0) {
                _useKey = DEFAULT_USE_KEY;
                return _useKey;
            }

            string path = grab.bindings[bindingIndex].effectivePath;
            if (string.IsNullOrEmpty(path)) {
                _useKey = DEFAULT_USE_KEY;
                return _useKey;
            }

            int slash = path.LastIndexOf('/');
            string key = slash >= 0 && slash < path.Length - 1 ? path.Substring(slash + 1) : string.Empty;
            _useKey = string.IsNullOrEmpty(key) ? DEFAULT_USE_KEY : key.ToUpperInvariant();
            return _useKey;
        }
    }
}
