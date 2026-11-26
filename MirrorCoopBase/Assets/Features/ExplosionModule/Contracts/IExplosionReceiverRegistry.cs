namespace Features.ExplosionModule.Contracts {
    // The receivers on this peer that explosions can reach. A receiver adds itself while it is on; its part list must
    // not change while it is listed.
    public interface IExplosionReceiverRegistry {
        public void Add(IExplosionReceiver receiver);
        public void Remove(IExplosionReceiver receiver);
    }
}
