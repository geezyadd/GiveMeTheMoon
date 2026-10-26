using System;
using System.Collections.Generic;
using Features.DebugModule.Contracts;

namespace Features.DebugModule.Scripts.Models {
    // Every registered debug action in registration order, and why each one cannot run now (null: it can).
    public sealed class DebugActionsModel {
        private readonly List<DebugActionDefinition> _actions = new();
        private readonly Dictionary<DebugActionDefinition, string> _unavailableReasons = new();

        public IReadOnlyList<DebugActionDefinition> Actions => _actions;

        public event Action OnActionsChanged;
        public event Action<DebugActionDefinition> OnAvailabilityChanged;

        public void Add(DebugActionDefinition action) {
            _actions.Add(action);
            _unavailableReasons[action] = null;
            OnActionsChanged?.Invoke();
        }

        // A second dispose of the same registration is harmless.
        public void Remove(DebugActionDefinition action) {
            if (_actions.Remove(action) == false)
                return;

            _unavailableReasons.Remove(action);
            OnActionsChanged?.Invoke();
        }

        public string GetUnavailableReason(DebugActionDefinition action) =>
            _unavailableReasons[action];

        public void SetUnavailableReason(DebugActionDefinition action, string reason) {
            string normalizedReason = string.IsNullOrEmpty(reason) ? null : reason;
            if (_unavailableReasons[action] == normalizedReason)
                return;

            _unavailableReasons[action] = normalizedReason;
            OnAvailabilityChanged?.Invoke(action);
        }

        public bool TryFind(string section, string label, out DebugActionDefinition action) {
            action = null;
            foreach (DebugActionDefinition candidate in _actions) {
                if (candidate.Section != section || candidate.Label != label)
                    continue;

                action = candidate;
                return true;
            }

            return false;
        }
    }
}
