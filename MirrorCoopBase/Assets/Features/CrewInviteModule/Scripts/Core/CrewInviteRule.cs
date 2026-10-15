namespace Features.CrewInviteModule.Scripts.Core {
    // The reasons are checked in the order the player can fix them: Steam first, then the station, then the crew.
    public sealed class CrewInviteRule : ICrewInviteRule {
        public CrewInviteStatus Evaluate(CrewInviteRequest request) {
            if (request.HasSteamLobby == false)
                return CrewInviteStatus.NoSteam;

            if (request.IsOverlayEnabled == false)
                return CrewInviteStatus.OverlayDisabled;

            if (request.IsJoinWindowOpen == false)
                return CrewInviteStatus.NotAtStation;

            if (request.PlayerCount >= request.PlayerSlots)
                return CrewInviteStatus.CrewFull;

            return CrewInviteStatus.Available;
        }
    }
}
