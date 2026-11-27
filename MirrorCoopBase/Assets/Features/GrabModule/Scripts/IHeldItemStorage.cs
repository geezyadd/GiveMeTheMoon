namespace Features.GrabModule.Scripts {
    // Where a hand keeps the items it carries but does not show (the player's inventory). The hand routes the pickup
    // of a storable item through it and tells it when an item leaves the hand for the world or is gone.
    public interface IHeldItemStorage {
        public bool CanStore(Grabbable item);
        public bool ServerStore(Grabbable item);
        public void ServerForget(uint itemNetId);
        public void ServerDropAll();
    }
}
