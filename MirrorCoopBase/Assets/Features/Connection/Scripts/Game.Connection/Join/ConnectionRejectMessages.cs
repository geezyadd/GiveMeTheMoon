using System;

namespace Game.Connection
{
    // The text a turned-away player reads in the menu.
    public sealed class ConnectionRejectMessages : IConnectionRejectMessages
    {
        private const string VERSION_MISMATCH = "Your game version does not match the host's. Update the game and try again.";
        private const string INVALID_STEAM_ID = "Steam is not signed in. Start Steam and try again.";
        private const string IN_FLIGHT = "The ship is in flight. You can join when it lands at a station.";
        private const string SERVER_FULL = "The crew is full. You can join when a player leaves.";
        private const string MAP_CHANGING = "The host is changing the map. Try again in a moment.";

        public string Describe(ConnectionRejectReason reason) =>
            reason switch
            {
                ConnectionRejectReason.VersionMismatch => VERSION_MISMATCH,
                ConnectionRejectReason.InvalidSteamId => INVALID_STEAM_ID,
                ConnectionRejectReason.InFlight => IN_FLIGHT,
                ConnectionRejectReason.ServerFull => SERVER_FULL,
                ConnectionRejectReason.MapChanging => MAP_CHANGING,
                _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "No message for this rejection.")
            };
    }
}
