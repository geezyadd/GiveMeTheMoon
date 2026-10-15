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

            if (request.IsInLobby == false && request.RunWindow != RunJoinWindow.AtStation)
                return request.RunWindow == RunJoinWindow.InFlight
                    ? ConnectionRejectReason.InFlight
                    : ConnectionRejectReason.MapChanging;

            if (request.PlayerCount >= request.PlayerSlots)
                return ConnectionRejectReason.ServerFull;

            return ConnectionRejectReason.Accepted;
        }
    }
}
