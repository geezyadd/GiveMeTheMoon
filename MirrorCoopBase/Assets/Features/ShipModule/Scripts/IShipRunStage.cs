namespace Features.ShipModule.Scripts {
    // The stage a run is played in (a playtest stage preset, or a normal run): read by the run at its start and by the
    // stations on arrival.
    public interface IShipRunStage {
        public int StartLoopIndex { get; }
        public bool AreStationDropsEnabled { get; }
        // Server: the ship stands reset at the start pad, the run model holds StartLoopIndex in the Build phase.
        public void ServerPrepareShip(ShipBase ship);
        // Every peer: the run is over (back to the lobby or the menu).
        public void OnRunEnded();
    }
}
