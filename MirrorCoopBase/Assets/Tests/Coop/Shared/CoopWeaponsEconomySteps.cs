#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Tests.Coop.Shared {
    // The exchange names of the economy and weapons scenarios. The host sends its commands in order, each a name and
    // its arguments separated by spaces; the client takes them one by one and reports each step.
    public static class CoopWeaponsEconomySteps {
        public const string SCENARIO_ECONOMY = "economy";
        public const string SCENARIO_WEAPONS = "weapons";
        public const char ARGUMENT_SEPARATOR = ' ';

        public const string ROCKET_ENGINE_ID = "rocket-engine";
        public const string PROPELLER_ID = "propeller";
        public const string LIGHT_GUN_ID = "light-machine-gun";
        public const string MINIGUN_ID = "minigun";
        public const string HEAVY_GUN_ID = "heavy-gun";
        public const string WRENCH_ID = "wrench";
        public const string PISTOL_ID = "pistol";
        public const string ENGINE_LEFT_SOCKET = "engine-left";
        public const string ENGINE_RIGHT_SOCKET = "engine-right";
        public const string TURRET_SOCKET = "turret-front";
        public const string REPAIRED_BREACH = "breach-04";

        // Client steps.
        public const string BOARDED = "boarded";
        public const string PURCHASE_REFUSED = "purchase-refused";
        public const string BOUGHT = "bought";
        public const string MONEY_GRANTED = "money-granted";
        public const string SHOP_CLOSED = "shop-closed";
        public const string INSTALLED = "installed";
        public const string DAMAGE_SEEN = "damage-seen";
        public const string REPAIR_HOLDING = "repair-holding";
        public const string REPAIRED = "repaired";
        public const string IN_CRUISE = "in-cruise";
        public const string LANDED = "landed";
        public const string PISTOL_FIRED = "pistol-fired";
        public const string PISTOL_DROPPED = "pistol-dropped";
        public const string PISTOL_PICKED_UP = "pistol-picked-up";
        public const string SEATED_AT_TURRET = "seated-at-turret";
        public const string TURRET_FIRING = "turret-firing";
        public const string TURRET_FIRED = "turret-fired";
        public const string STOOD_UP = "stood-up";
        public const string ROCK_BROKEN = "rock-broken";
        public const string BALLOON_POPPED = "balloon-popped";
        // The client could not see the target the host picked: the host picks another one.
        public const string TARGET_BLOCKED = "target-blocked";

        // Host commands. Arguments: an entry or module id, a socket id, a rock id (FieldRockIdText), a balloon index.
        public const string COMMAND_TRY_UNAFFORDABLE = "try-unaffordable";
        public const string COMMAND_BUY = "buy";
        public const string COMMAND_GRANT_MONEY = "grant-money";
        public const string COMMAND_LEAVE_SHOP = "leave-shop";
        public const string COMMAND_INSTALL = "install";
        public const string COMMAND_SEE_DAMAGE = "see-damage";
        public const string COMMAND_REPAIR_MODULE = "repair-module";
        public const string COMMAND_PATCH_BREACH = "patch-breach";
        public const string COMMAND_BOARD = "board";
        public const string COMMAND_RIDE = "ride";
        public const string COMMAND_LAND = "land";
        public const string COMMAND_FIRE_PISTOL_AT_DUMMY = "fire-pistol-at-dummy";
        public const string COMMAND_DROP_PISTOL = "drop-pistol";
        public const string COMMAND_PICK_UP_PISTOL = "pick-up-pistol";
        public const string COMMAND_SIT_AT_TURRET = "sit-at-turret";
        public const string COMMAND_FIRE_TURRET_AT_DUMMY = "fire-turret-at-dummy";
        public const string COMMAND_STOP_FIRE = "stop-fire";
        public const string COMMAND_STAND_UP = "stand-up";
        public const string COMMAND_TURRET_ROCK = "turret-rock";
        public const string COMMAND_TURRET_BALLOON = "turret-balloon";
        public const string COMMAND_PISTOL_ROCK = "pistol-rock";
        public const string COMMAND_PISTOL_BALLOON = "pistol-balloon";
        public const string COMMAND_SEE_BALLOON = "see-balloon";
        public const string COMMAND_FINISH = "finish";
    }
}
#endif
