using System;
using Features.CrewInviteModule.Scripts.Generated;
using Features.TooltipModule.Scripts;
using Zenject;

namespace Features.CrewInviteModule.Scripts {
    // The invite terminal's tooltip follows the local player's invite status and shows the crew as "N/M".
    public sealed class CrewInviteTooltipProvider : ITooltipStateProvider, IInitializable, IDisposable {
        private readonly ICrewInviteStatusService _crewInviteStatusService;
        private readonly IReadOnlyCrewRosterModel _crewRosterModel;

        public CrewInviteTooltipProvider(ICrewInviteStatusService crewInviteStatusService, IReadOnlyCrewRosterModel crewRosterModel) {
            _crewInviteStatusService = crewInviteStatusService;
            _crewRosterModel = crewRosterModel;
        }

        public event Action OnStateChanged;

        public void Initialize() {
            _crewRosterModel.OnChanged += OnRosterChanged;
            _crewRosterModel.OnAvailableChanged += OnRosterChanged;
        }

        public void Dispose() {
            _crewRosterModel.OnChanged -= OnRosterChanged;
            _crewRosterModel.OnAvailableChanged -= OnRosterChanged;
        }

        public bool TrySelect(InteractionTooltip tooltip, out TooltipData data, out string description) {
            data = null;
            description = null;
            if (tooltip.TryGetComponent(out CrewInviteTooltip crewInviteTooltip) == false)
                return false;

            data = crewInviteTooltip.Select(_crewInviteStatusService.EvaluateForLocalPlayer());
            description = string.Format(data.Description, _crewRosterModel.PlayerCount, _crewRosterModel.PlayerSlots);
            return true;
        }

        private void OnRosterChanged() =>
            OnStateChanged?.Invoke();
    }
}
