#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Tests.Coop.Shared {
    // The connection tests' client takes the host's commands one by one, in order, and answers each with a report whose
    // step is the command's name. A command may carry one argument after the separator: "join:CoopClient2".
    public static class CoopConnectionCommands {
        public const string SCENARIO = "connection-commands";
        public const char ARGUMENT_SEPARATOR = ':';

        // Joins the host through the menu under the argument's name and waits for the lobby or the game at a station.
        public const string JOIN = "join";
        // Joins and waits to be turned away; the report carries the reason and whether the menu shows its text.
        public const string JOIN_EXPECT_REJECT = "join-expect-reject";
        public const string BOARD = "board";
        public const string SIT_AT_HELM = "sit-at-helm";
        public const string SIT_AT_TURRET = "sit-at-turret";
        public const string DIE = "die";
        public const string LEAVE = "leave";
        // Waits until the run phase named by the argument (Cruise, Build).
        public const string WAIT_PHASE = "wait-phase";
        public const string WAIT_MENU = "wait-menu";
        public const string WAIT_LOBBY = "wait-lobby";
        public const string WAIT_STATION = "wait-station";
        // Walks forward under the scripted move input and reports the distance and whether the body is free of the ship.
        public const string WALK = "walk";
        // Hosts its own session through the menu, then leaves it again.
        public const string HOST_AND_LEAVE = "host-and-leave";
        public const string REPORT = "report";
        // Captures this client's screen into the exchange folder as client-<argument>.png.
        public const string SCREENSHOT = "screenshot";
        public const string QUIT = "quit";

        public const string SCREENSHOT_PREFIX = "client-";
        public const string SCREENSHOT_EXTENSION = ".png";

        public static string With(string command, string argument) =>
            command + ARGUMENT_SEPARATOR + argument;
    }
}
#endif
