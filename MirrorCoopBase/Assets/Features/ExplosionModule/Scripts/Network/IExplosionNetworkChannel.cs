namespace Features.ExplosionModule.Network {
    // Listens for the server's explosions on this client. Mirror clears client handlers when a client stops, so the
    // handler is registered again each time a client starts (GameCore calls this on ClientStarted).
    public interface IExplosionNetworkChannel {
        public void RegisterForClient();
    }
}
