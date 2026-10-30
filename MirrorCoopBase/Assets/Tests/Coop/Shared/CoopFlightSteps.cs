#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Tests.Coop.Shared {
    // The scenarios, client steps and host commands of the coop flight tests (the ship, flight and navigation played
    // by the client). The common steps (in-lobby, boarded, in-cruise, landed, finished, failed) are CoopTestSteps'.
    public static class CoopFlightSteps {
        public const string SCENARIO_THROTTLE_AND_HELM = "throttle-and-helm";
        public const string SCENARIO_DECK_ITEMS = "deck-items";
        public const string SCENARIO_NAVIGATION_VIEW = "navigation-view";
        public const string SCENARIO_ROCKS_AND_HULL_LOSS = "rocks-and-hull-loss";
        public const string SCENARIO_HARPOON = "harpoon";
        public const string SCENARIO_FIELD_AGENTS = "field-agents";
        public const string SCENARIO_BUGS = "bugs";
        public const string SCENARIO_PARCEL = "parcel";
        public const string SCENARIO_OVERBOARD = "overboard";
        public const string SCENARIO_POINT_OF_INTEREST = "point-of-interest";

        // Throttle and helm.
        public const string COMMAND_REPORT_STOPPED = "report-stopped";
        public const string SHIP_STOPPED = "ship-stopped";
        public const string COMMAND_TAKE_HELM = "take-helm";
        public const string COMMAND_STEER = "steer";
        public const string STEERED = "steered";
        public const string COMMAND_TRY_THROTTLE_SEATED = "try-throttle-seated";
        public const string THROTTLE_REFUSED = "throttle-refused";
        public const string COMMAND_REPORT_POSE = "report-pose";
        public const string POSE = "pose";
        public const string COMMAND_STAND_UP = "stand-up";
        public const string STOOD_UP = "stood-up";

        // Deck items.
        public const string COMMAND_DROP_ITEM = "drop-item";
        public const string ITEM_DROPPED = "item-dropped";
        public const string ITEM_CARRIED = "item-carried";
        public const string COMMAND_RELEASE_ITEM = "release-item";
        public const string ITEM_RELEASED = "item-released";
        public const string COMMAND_REPORT_ITEMS_TURNED = "report-items-turned";
        public const string ITEMS_TURNED = "items-turned";
        public const string COMMAND_REPORT_ITEMS_LANDED = "report-items-landed";
        public const string ITEMS_LANDED = "items-landed";

        // Navigation view.
        public const string COMMAND_STAND_BY_CONSOLE = "stand-by-console";
        public const string BY_CONSOLE = "by-console";
        public const string COMMAND_MARK = "mark";
        public const string SONAR_MARKED = "sonar-marked";
        public const string COMMAND_HAZE = "haze";
        public const string HAZE_ON = "haze-on";
        public const string HAZE_OFF = "haze-off";
        public const string HAZE_BACK_ON = "haze-back-on";
        public const string COMMAND_SHOW_STATION = "show-station";
        public const string STATION_SHOWN = "station-shown";
        public const string STATION_HIDDEN = "station-hidden";

        // Rocks and hull loss.
        public const string COMMAND_WATCH_IMPACTS = "watch-impacts";
        public const string COMMAND_REPORT_IMPACTS = "report-impacts";
        public const string COMMAND_WATCH_NEAR_PASS = "watch-near-pass";
        public const string COMMAND_REPORT_NEAR_PASS = "report-near-pass";
        public const string WATCHING = "watching";
        public const string IMPACTS = "impacts";
        public const string FREE_IN_LOBBY = "free-in-lobby";

        // Ore and the harpoon.
        public const string COMMAND_REPORT_ORE = "report-ore";
        public const string ORE_SEEN = "ore-seen";
        public const string COMMAND_THROW_HARPOON = "throw-harpoon";
        public const string HARPOON_HOOKED = "harpoon-hooked";
        public const string COMMAND_REPORT_DELIVERY = "report-delivery";
        public const string ORE_DELIVERED = "ore-delivered";
        // Field agents: the host's debug flock, its size the debug window's default.
        public const string COMMAND_REPORT_AGENTS = "report-agents";
        public const string AGENTS_SEEN = "agents-seen";
        public const int FIELD_AGENT_COUNT = 20;

        // Bugs: the host's debug group, a gnawing bug the client shoots with the pistol the host gave it.
        public const string COMMAND_SHOOT_BUG = "shoot-bug";
        public const string BUG_SHOT = "bug-shot";
        public const int BUG_COUNT = 3;

        // Points of interest: the client sees the field's points, plays the bug nest's entry cutscene with the host and
        // sees the payout.
        public const string COMMAND_REPORT_POINTS = "report-points";
        public const string POINTS_SEEN = "points-seen";
        public const string COMMAND_WATCH_CUTSCENE = "watch-cutscene";
        public const string CUTSCENE_WATCHED = "cutscene-watched";
        public const string COMMAND_REPORT_PAYOUT = "report-payout";
        public const string PAYOUT_SEEN = "payout-seen";

        // Parcel: the client takes a parcel at the stand and puts it on the deck; a bug the host sends steals it in cruise.
        public const string COMMAND_TAKE_PARCEL = "take-parcel";
        public const string PARCEL_TAKEN = "parcel-taken";
        public const string COMMAND_REPORT_THEFT = "report-theft";
        public const string PARCEL_STOLEN = "parcel-stolen";
        public const string COMMAND_REPORT_DRIFT = "report-drift";
        public const string PARCEL_DRIFTING = "parcel-drifting";

        // Overboard.
        public const string COMMAND_THROW_OVERBOARD = "throw-overboard";
        public const string THROWN_OVERBOARD = "thrown-overboard";
        public const string COMMAND_REPORT_BACK_ON_DECK = "report-back-on-deck";
        public const string BACK_ON_DECK = "back-on-deck";
        public const string COMMAND_THROW_OVERBOARD_AGAIN = "throw-overboard-again";
        public const string THROWN_OVERBOARD_AGAIN = "thrown-overboard-again";
        public const string COMMAND_REPORT_PULLED_ON_DECK = "report-pulled-on-deck";
        public const string PULLED_ON_DECK = "pulled-on-deck";

        public const string CLIENT_SCREENSHOT_PREFIX = "client-";
        public const string SCREENSHOT_EXTENSION = ".png";
    }
}
#endif
