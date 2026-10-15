#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Tests.Coop.Shared {
    // The names both processes use in the exchange files: client steps in the report, host commands in the commands.
    public static class CoopTestSteps {
        public const string SCENARIO_LEAVE_AND_REJOIN = "leave-and-rejoin";

        public const string IN_LOBBY = "in-lobby";
        public const string BOARDED = "boarded";
        public const string IN_CRUISE = "in-cruise";
        public const string LANDED = "landed";
        public const string LEFT_IN_FLIGHT = "left-in-flight";
        public const string REJECTED_IN_FLIGHT = "rejected-in-flight";
        public const string REJOINED = "rejoined";
        public const string DIED = "died";
        public const string REVIVED = "revived";
        public const string FINISHED = "finished";
        public const string FAILED = "failed";

        public const string COMMAND_REJOIN = "rejoin";
        public const string COMMAND_FINISH = "finish";
    }
}
#endif
