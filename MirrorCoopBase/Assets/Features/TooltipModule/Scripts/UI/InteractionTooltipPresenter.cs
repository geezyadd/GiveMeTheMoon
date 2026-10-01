using System;
using System.Collections.Generic;
using System.Text;
using Features.GrabModule.Scripts;
using Features.InputModule.Realization.Scripts.Generated;
using Features.MvpModule;
using Features.ShipModule.Scripts;
using Features.ShopModule.Scripts.Core;
using Features.ShopModule.Scripts.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Features.TooltipModule.Scripts.UI {
    public sealed class InteractionTooltipPresenter : PresenterBehaviour<InteractionTooltipViewBase> {
        private const string KEYBOARD_GROUP = "Keyboard and Mouse";
        private const string DEFAULT_USE_KEY = "E";
        private const string HINT_FORMAT = "[{0}] {1}";
        private const string STAT_FORMAT = "{0}: {1:0.##}";
        private const string STAT_SEPARATOR = "   ";
        private const char CONFIGURED_HINT_PREFIX = '[';

        private readonly HoveredInteractableModel _hoveredInteractableModel;
        private readonly ShopModel _shopModel;
        private readonly IShopItemStatsService _shopItemStatsService;
        private readonly InputActions _inputActions;
        private string _useKey;

        public InteractionTooltipPresenter(
            HoveredInteractableModel hoveredInteractableModel,
            ShopModel shopModel,
            IShopItemStatsService shopItemStatsService,
            InputActions inputActions) {
            _hoveredInteractableModel = hoveredInteractableModel;
            _shopModel = shopModel;
            _shopItemStatsService = shopItemStatsService;
            _inputActions = inputActions;
        }

        protected override void OnViewSet() {
            _hoveredInteractableModel.OnChanged += Refresh;
            _shopModel.OnOpenChanged += Refresh;
            Refresh();
        }

        protected override void OnDisposed() {
            _hoveredInteractableModel.OnChanged -= Refresh;
            _shopModel.OnOpenChanged -= Refresh;
        }

        private void Refresh() {
            if (View == null || View.IsViewDisposed)
                return;

            if (TryGetTooltip(out InteractionTooltip tooltip, out bool isReady) == false) {
                if (View.IsShown)
                    View.HideView();
                return;
            }

            TooltipData data = tooltip.Select(isReady);
            View.SetContent(data.Title, BuildDescription(data, tooltip), BuildActionHint(data));
            if (View.IsShown == false)
                View.ShowView();
        }

        private bool TryGetTooltip(out InteractionTooltip tooltip, out bool isReady) {
            tooltip = null;
            isReady = false;
            if (_shopModel.IsOpen)
                return false;

            if (_hoveredInteractableModel.TryGetCurrent(out Component source, out isReady) == false)
                return false;

            tooltip = InteractionTooltip.FindFor(source);
            return tooltip != null;
        }

        private string BuildDescription(TooltipData data, InteractionTooltip tooltip) {
            ShipItem item = tooltip.GetComponent<ShipItem>();
            if (item == null)
                return data.Description;

            IReadOnlyList<ShopItemStat> stats = _shopItemStatsService.GetStats(item);
            if (stats.Count == 0)
                return data.Description;

            return data.Description + "\n" + FormatStats(stats);
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

        private static string FormatStats(IReadOnlyList<ShopItemStat> stats) {
            StringBuilder builder = new();
            for (int i = 0; i < stats.Count; i++) {
                if (builder.Length > 0)
                    builder.Append(STAT_SEPARATOR);

                ShopItemStat stat = stats[i];
                builder.AppendFormat(STAT_FORMAT, StatLabel(stat.Type), stat.Value);
            }

            return builder.ToString();
        }

        private static string StatLabel(ShipStatType type) {
            switch (type) {
                case ShipStatType.Thrust:
                    return "Thrust";
                case ShipStatType.FlightSpeed:
                    return "Flight speed";
                case ShipStatType.DodgeRange:
                    return "Dodge";
                case ShipStatType.Handling:
                    return "Handling";
                case ShipStatType.Armor:
                    return "Armor";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
