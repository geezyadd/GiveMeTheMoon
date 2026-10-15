using System;
using Features.CrewInviteModule.Data.Generated;
using Features.CrewInviteModule.Scripts.Core;
using Features.TooltipModule.Scripts;
using Game.Connection;
using Zenject;

namespace Features.CrewInviteModule.Scripts.UI {
    public sealed class CrewInviteTooltipProvider : ITooltipStateProvider, IInitializable, IDisposable {
        private readonly ICrewInviteStatusService _crewInviteStatusService;
        private readonly IReadOnlyCrewRosterModel _crewRosterModel;
        private readonly ISteamLobbyService _steamLobbyService;

        public event Action OnStateChanged;

        public CrewInviteTooltipProvider(
            ICrewInviteStatusService crewInviteStatusService,
            IReadOnlyCrewRosterModel crewRosterModel,
            ISteamLobbyService steamLobbyService) {
            _crewInviteStatusService = crewInviteStatusService;
            _crewRosterModel = crewRosterModel;
            _steamLobbyService = steamLobbyService;
        }

        public void Initialize() {
            _crewRosterModel.OnChanged += OnInviteInputChanged;
            _steamLobbyService.InviteStateChanged += OnInviteInputChanged;
        }

        public void Dispose() {
            _crewRosterModel.OnChanged -= OnInviteInputChanged;
            _steamLobbyService.InviteStateChanged -= OnInviteInputChanged;
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

        private void OnInviteInputChanged() =>
            OnStateChanged?.Invoke();
    }
}
