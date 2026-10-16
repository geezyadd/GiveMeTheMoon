using System.Threading.Tasks;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Requests;
using Features.GameCoreModule.Contracts;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Server {
    // The flight field's side of the run: at every launch the server generates a new field and publishes it, at the end
    // of a flight it takes the field down; a world shift moves the field's origin and its Mirror objects.
    // A long flight's field takes over 100 ms to generate, so generation runs on a worker thread (it touches no Unity
    // object) and the field is published on the first tick after it is done, well inside the 9 s takeoff.
    public sealed class FlightFieldService : IFlightRouteListener, IWorldShiftFollower, ITickable {
        private readonly IFieldRequestFactory _fieldRequestFactory;
        private readonly IFlightFieldGenerator _flightFieldGenerator;
        private readonly FlightFieldServerModel _flightFieldServerModel;
        private readonly IFlightFieldPublisher _flightFieldPublisher;
        private readonly IFieldNetworkedStructures _fieldNetworkedStructures;

        private Task<FlightField> _pendingGeneration;

        public FlightFieldService(IFieldRequestFactory fieldRequestFactory, IFlightFieldGenerator flightFieldGenerator, FlightFieldServerModel flightFieldServerModel,
                                  IFlightFieldPublisher flightFieldPublisher, IFieldNetworkedStructures fieldNetworkedStructures) {
            _fieldRequestFactory = fieldRequestFactory;
            _flightFieldGenerator = flightFieldGenerator;
            _flightFieldServerModel = flightFieldServerModel;
            _flightFieldPublisher = flightFieldPublisher;
            _fieldNetworkedStructures = fieldNetworkedStructures;
        }

        public void ServerOnFlightLaunched(FlightLaunch launch) {
            ServerOnFlightEnded();
            int seed = Random.Range(0, int.MaxValue);
            FieldRequest request = _fieldRequestFactory.Create(launch.StationA, launch.StationB, launch.CruiseStart, launch.LoopIndex, seed);
            _pendingGeneration = Task.Run(() => _flightFieldGenerator.Generate(request));
        }

        public void Tick() {
            if (_pendingGeneration == null || !_pendingGeneration.IsCompleted)
                return;

            Task<FlightField> generation = _pendingGeneration;
            _pendingGeneration = null;
            if (!NetworkServer.active)
                return;

            _flightFieldServerModel.SetField(generation.GetAwaiter().GetResult());
            _fieldNetworkedStructures.ServerSpawn(_flightFieldServerModel);
            _flightFieldPublisher.Publish(_flightFieldServerModel.Field, _flightFieldServerModel.Number);
        }

        // Also called when the run is reset after the server stopped: Mirror has destroyed the spawned objects and the
        // bridge then, only the server model is left to clear.
        public void ServerOnFlightEnded() {
            _pendingGeneration = null;
            if (!_flightFieldServerModel.HasField)
                return;

            if (NetworkServer.active) {
                _fieldNetworkedStructures.ServerDespawn(_flightFieldServerModel);
                _flightFieldPublisher.Clear();
            }

            _flightFieldServerModel.ClearField();
        }

        // The local structures follow the origin through the live field model (LiveFieldBuildSystem), on every peer.
        public void FollowWorldShift(Vector3 delta) {
            if (!NetworkServer.active) {
                _fieldNetworkedStructures.ClientShift(delta);
                return;
            }

            if (!_flightFieldServerModel.HasField)
                return;

            _fieldNetworkedStructures.ServerShift(_flightFieldServerModel, delta);
            _flightFieldPublisher.ShiftOrigin(delta);
        }
    }
}
