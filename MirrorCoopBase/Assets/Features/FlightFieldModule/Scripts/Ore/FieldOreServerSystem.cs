using Mirror;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Ore {
    // Server, every frame: the ropes first (a piece pulled in reaches the ship, a broken rope lets its piece go), then
    // the pieces at their end of life are gone.
    public sealed class FieldOreServerSystem : ITickable {
        private readonly IFieldOreRopeService _fieldOreRopeService;
        private readonly IFieldOreExpiryService _fieldOreExpiryService;

        public FieldOreServerSystem(IFieldOreRopeService fieldOreRopeService, IFieldOreExpiryService fieldOreExpiryService) {
            _fieldOreRopeService = fieldOreRopeService;
            _fieldOreExpiryService = fieldOreExpiryService;
        }

        public void Tick() {
            if (!NetworkServer.active)
                return;

            _fieldOreRopeService.ServerUpdateRopes();
            _fieldOreExpiryService.ServerRemoveExpired();
        }
    }
}
