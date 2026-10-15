using System;
using Features.GameCoreModule.Contracts;

namespace Game.Connection
{
    // A player may come in while the session is in the lobby or the ship stands at a station, if a slot is free.
    // A map that is loading, or loaded without a run bound yet, is between states: the player is asked to retry.
    public sealed class JoinRule : IJoinRule
    {
        public ConnectionRejectReason Evaluate(JoinRequest request)
        {
            if (request.VersionMatches == false)
                return ConnectionRejectReason.VersionMismatch;

            if (request.HasValidSteamId == false)
                return ConnectionRejectReason.InvalidSteamId;

            if (request.IsChangingMap)
                return ConnectionRejectReason.MapChanging;

            bool isJoinWindowOpen = request.IsInLobby || request.RunWindow == RunJoinWindow.AtStation;
            if (isJoinWindowOpen == false)
                return ClosedWindowReason(request.RunWindow);

            if (request.PlayerCount >= request.PlayerSlots)
                return ConnectionRejectReason.ServerFull;

            return ConnectionRejectReason.Accepted;
        }

        private static ConnectionRejectReason ClosedWindowReason(RunJoinWindow window) =>
            window switch
            {
                RunJoinWindow.InFlight => ConnectionRejectReason.InFlight,
                RunJoinWindow.NoRun => ConnectionRejectReason.MapChanging,
                _ => throw new ArgumentOutOfRangeException(nameof(window), window, "The join gate reported no run state.")
            };
    }
}
