using Features.PlayerProfileModule.Data.Generated;

namespace Features.PlayerProfileModule.Data {
    // Lets other modules read a player object's profile without depending on the bridge or the nameplate.
    public interface IPlayerProfileSource {
        // Null until the player object has started on this client.
        public IReadOnlyPlayerProfileModel Model { get; }
    }
}
