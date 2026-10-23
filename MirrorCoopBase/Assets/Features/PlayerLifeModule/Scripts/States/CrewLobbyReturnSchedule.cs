namespace Features.PlayerLifeModule.Scripts {
    // When the crew goes back to the lobby: after the delay since the whole crew died or since the ship was lost.
    // A crewmate coming back to life cancels only the all-dead return; a lost ship stays lost.
    public sealed class CrewLobbyReturnSchedule {
        private bool _isAllDeadPending;
        private bool _isShipLostPending;
        private float _returnAt;

        public bool IsPending => _isAllDeadPending || _isShipLostPending;

        public void SetAllDead(bool areAllDead, float returnAt) {
            if (areAllDead == false) {
                _isAllDeadPending = false;
                return;
            }

            if (IsPending == false)
                _returnAt = returnAt;

            _isAllDeadPending = true;
        }

        public void RequestShipLost(float returnAt) {
            if (IsPending == false)
                _returnAt = returnAt;

            _isShipLostPending = true;
        }

        // True once per scheduled return, when its time has come.
        public bool TryConsume(float now) {
            if (IsPending == false || now < _returnAt)
                return false;

            Cancel();
            return true;
        }

        public void Cancel() {
            _isAllDeadPending = false;
            _isShipLostPending = false;
        }
    }
}
