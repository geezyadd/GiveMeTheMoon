using System.Collections.Generic;
using Features.GrabModule.Scripts;
using Features.MvpModule;
using Features.ShopModule.Scripts.Data;
using UnityEngine;

namespace Features.TooltipModule.Scripts.UI {
    public sealed class InteractionTooltipPresenter : PresenterBehaviour<InteractionTooltipViewBase> {
        private readonly HoveredInteractableModel _hoveredInteractableModel;
        private readonly ShopModel _shopModel;
        private readonly ITooltipContentService _tooltipContentService;
        private readonly List<ITooltipStateProvider> _tooltipStateProviders;

        public InteractionTooltipPresenter(
            HoveredInteractableModel hoveredInteractableModel,
            ShopModel shopModel,
            ITooltipContentService tooltipContentService,
            List<ITooltipStateProvider> tooltipStateProviders) {
            _hoveredInteractableModel = hoveredInteractableModel;
            _shopModel = shopModel;
            _tooltipContentService = tooltipContentService;
            _tooltipStateProviders = tooltipStateProviders;
        }

        protected override void OnViewSet() {
            _hoveredInteractableModel.OnHoverChanged += Refresh;
            _shopModel.OnOpenChanged += Refresh;
            for (int i = 0; i < _tooltipStateProviders.Count; i++)
                _tooltipStateProviders[i].OnStateChanged += Refresh;

            Refresh();
        }

        protected override void OnDisposed() {
            _hoveredInteractableModel.OnHoverChanged -= Refresh;
            _shopModel.OnOpenChanged -= Refresh;
            for (int i = 0; i < _tooltipStateProviders.Count; i++)
                _tooltipStateProviders[i].OnStateChanged -= Refresh;
        }

        private void Refresh() {
            if (TryGetTooltip(out InteractionTooltip tooltip, out bool isReady) == false) {
                if (View.IsShown)
                    View.HideView();
                return;
            }

            TooltipContent content = _tooltipContentService.Build(tooltip, isReady);
            View.SetContent(content.Title, content.Description, content.ActionHint);
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
    }
}
