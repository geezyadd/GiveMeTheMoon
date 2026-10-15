namespace Features.CrewInviteModule.Scripts.Core {
    public interface ICrewInviteStatusService {
        public CrewInviteStatus EvaluateForLocalPlayer();

        public CrewInviteStatus EvaluateForRemotePlayer();
    }
}
