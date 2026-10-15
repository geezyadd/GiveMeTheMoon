namespace Features.CrewInviteModule.Scripts {
    public interface ICrewInviteStatusService {
        public CrewInviteStatus EvaluateForLocalPlayer();

        public CrewInviteStatus EvaluateForRemotePlayer();
    }
}
