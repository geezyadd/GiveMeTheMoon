#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Tests.Coop.Shared {
    // The names both processes use in the exchange files: client steps in the report, host commands in the commands.
    public static class CoopTestSteps {
        public const string SCENARIO_LEAVE_AND_REJOIN = "leave-and-rejoin";

        public const string IN_LOBBY = "in-lobby";
        public const string BOARDED = "boarded";
        public const string IN_CRUISE = "in-cruise";
        public const string SEATED_AT_HELM = "seated-at-helm";
        // The loop after whose landing the client takes the helm; it steers the next loop until it leaves in flight.
        public const int HELM_LOOP = 1;
        public const string LANDED = "landed";
        public const string LEFT_IN_FLIGHT = "left-in-flight";
        public const string REJECTED_IN_FLIGHT = "rejected-in-flight";
        public const string REJOINED = "rejoined";
        public const string DIED = "died";
        public const string REVIVED = "revived";
        public const string SONAR_MARK_SEEN = "sonar-mark-seen";
        // The loop whose cruise the host navigator marks a contact in.
        public const int SONAR_MARK_LOOP = 1;
        public const string FINISHED = "finished";
        public const string FAILED = "failed";

        public const string COMMAND_REJOIN = "rejoin";
        public const string COMMAND_FINISH = "finish";
    }
}
#endif
