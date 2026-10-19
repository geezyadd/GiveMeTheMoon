using System.Threading.Tasks;
using Features.FlightFieldModule.Scripts.Model;
using Features.GameCoreModule.Contracts;
using Mirror;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Server {
    // The flight field's side of the run: at every launch the server starts generating a new field and publishes it
    // on the first tick after it is done, well inside the 9 s takeoff; at the end of a flight it takes the field down
    // and drops a generation still running.
    public sealed class FlightFieldLaunchSystem : IFlightRouteListener, ITickable {
        private readonly IFlightFieldService _flightFieldService;

        private Task<FlightField> _pendingGeneration;

        public FlightFieldLaunchSystem(IFlightFieldService flightFieldService) =>
            _flightFieldService = flightFieldService;

        public void ServerOnFlightLaunched(FlightLaunch launch) {
            ServerOnFlightEnded();
            _pendingGeneration = _flightFieldService.ServerStartGeneration(launch);
        }

        public void ServerOnFlightEnded() {
            _pendingGeneration = null;
            _flightFieldService.ServerClear();
        }

        public void Tick() {
            if (_pendingGeneration == null || !_pendingGeneration.IsCompleted)
                return;

            Task<FlightField> generation = _pendingGeneration;
            _pendingGeneration = null;
            if (!NetworkServer.active)
                return;

            _flightFieldService.ServerPublish(generation.GetAwaiter().GetResult());
        }
    }
}
