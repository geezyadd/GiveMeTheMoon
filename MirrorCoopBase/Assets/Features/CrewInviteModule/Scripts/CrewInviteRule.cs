namespace Features.CrewInviteModule.Scripts {
    // A friend can be invited when the inviting player has a Steam lobby with the overlay, the session lets players in
    // right now (the join rule's window: lobby or a station) and a slot is free.
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
