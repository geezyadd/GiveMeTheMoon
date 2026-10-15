#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Tests.Coop.Shared {
    // The names the two processes authenticate with, so each side knows which nameplate it must see over the other.
    public static class CoopTestPlayers {
        public const string HOST_NAME = "CoopHost";
        public const string CLIENT_NAME = "CoopClient";
    }
}
#endif
