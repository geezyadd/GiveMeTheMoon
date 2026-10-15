namespace Features.CrewInviteModule.Scripts.Core {
    public interface ICrewInviteRule {
        public CrewInviteStatus Evaluate(CrewInviteRequest request);
    }
}
