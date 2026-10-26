using System.Collections.Generic;
using Features.DebugModule.Contracts;
using Features.DebugModule.Scripts.Models;
using Features.DebugModule.Scripts.Services;
using Features.MvpModule;

namespace Features.DebugModule.Scripts.UI {
    // The panel lists the registered actions by section, in the order the sections were first registered.
    public sealed class DebugPanelPresenter : PresenterBehaviour<DebugPanelViewBase> {
        private readonly DebugActionsModel _debugActionsModel;
        private readonly DebugPanelModel _debugPanelModel;
        private readonly IDebugActionRunService _debugActionRunService;
        private readonly List<string> _sections = new();

        public DebugPanelPresenter(DebugActionsModel debugActionsModel, DebugPanelModel debugPanelModel, IDebugActionRunService debugActionRunService) {
            _debugActionsModel = debugActionsModel;
            _debugPanelModel = debugPanelModel;
            _debugActionRunService = debugActionRunService;
        }

        protected override void OnViewSet() {
            View.OnActionClicked += OnActionClicked;
            _debugActionsModel.OnActionsChanged += OnActionsChanged;
            _debugActionsModel.OnAvailabilityChanged += OnAvailabilityChanged;
            _debugPanelModel.OnOpenChanged += OnPanelOpenChanged;
            OnActionsChanged();
            OnPanelOpenChanged();
        }

        protected override void OnDisposed() {
            View.OnActionClicked -= OnActionClicked;
            _debugActionsModel.OnActionsChanged -= OnActionsChanged;
            _debugActionsModel.OnAvailabilityChanged -= OnAvailabilityChanged;
            _debugPanelModel.OnOpenChanged -= OnPanelOpenChanged;
        }

        private void CollectSections() {
            _sections.Clear();
            foreach (DebugActionDefinition action in _debugActionsModel.Actions) {
                if (_sections.Contains(action.Section) == false)
                    _sections.Add(action.Section);
            }
        }

        private void ShowSection(string section) {
            View.AddSection(section);
            foreach (DebugActionDefinition action in _debugActionsModel.Actions) {
                if (action.Section != section)
                    continue;

                View.AddAction(action);
                View.ShowAvailability(action, _debugActionsModel.GetUnavailableReason(action));
            }
        }

        private void OnActionsChanged() {
            View.ClearActions();
            CollectSections();
            foreach (string section in _sections)
                ShowSection(section);
        }

        private void OnAvailabilityChanged(DebugActionDefinition action) =>
            View.ShowAvailability(action, _debugActionsModel.GetUnavailableReason(action));

        private void OnPanelOpenChanged() =>
            View.ShowPanel(_debugPanelModel.IsOpen);

        private void OnActionClicked(DebugActionDefinition action, float value) =>
            _debugActionRunService.Run(action, value);
    }
}
