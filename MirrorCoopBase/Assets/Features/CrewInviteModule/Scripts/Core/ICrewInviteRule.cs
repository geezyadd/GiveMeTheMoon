namespace Features.CrewInviteModule.Scripts {
    public interface ICrewInviteRule {
        public CrewInviteStatus Evaluate(CrewInviteRequest request);
    }
}
