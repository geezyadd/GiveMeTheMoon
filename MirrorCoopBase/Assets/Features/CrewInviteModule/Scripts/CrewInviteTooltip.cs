using System;
using Features.TooltipModule.Scripts;
using UnityEngine;

namespace Features.CrewInviteModule.Scripts {
    // The terminal's tooltip for each invite status; a description may hold {0} (players) and {1} (slots).
    public sealed class CrewInviteTooltip : MonoBehaviour {
        [SerializeField] private TooltipData _available;
        [SerializeField] private TooltipData _crewFull;
        [SerializeField] private TooltipData _notAtStation;
        [SerializeField] private TooltipData _noSteam;
        [SerializeField] private TooltipData _overlayDisabled;

        public TooltipData Select(CrewInviteStatus status) =>
            status switch {
                CrewInviteStatus.Available => _available,
                CrewInviteStatus.CrewFull => _crewFull,
                CrewInviteStatus.NotAtStation => _notAtStation,
                CrewInviteStatus.NoSteam => _noSteam,
                CrewInviteStatus.OverlayDisabled => _overlayDisabled,
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, "No crew invite tooltip for this status.")
            };
    }
}
