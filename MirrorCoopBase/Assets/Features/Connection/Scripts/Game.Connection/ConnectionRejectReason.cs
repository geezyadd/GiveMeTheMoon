namespace Game.Connection
{
    public enum ConnectionRejectReason : byte
    {
        Accepted = 255,
        VersionMismatch = 0,
        InvalidSteamId = 3,
        InFlight = 4,
        ServerFull = 5,
        MapChanging = 6
    }
}
