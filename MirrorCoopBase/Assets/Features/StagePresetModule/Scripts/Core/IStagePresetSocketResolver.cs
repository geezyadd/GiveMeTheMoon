using System.Collections.Generic;
using Features.ShipModule.Scripts;
using Features.StagePresetModule.Scripts.Configurations;

namespace Features.StagePresetModule.Scripts.Core {
    public interface IStagePresetSocketResolver {
        // The ship's socket of every preset module, in the preset's order. Throws when a socket of the preset is not
        // on the ship or does not take the preset's module.
        public IReadOnlyList<ShipSocket> ResolveSockets(StagePreset preset, IReadOnlyList<ShipSocket> sockets);
    }
}
