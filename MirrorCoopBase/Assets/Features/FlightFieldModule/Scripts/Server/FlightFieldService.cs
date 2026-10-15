using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Requests;
using Features.GameCoreModule.Contracts;
using Mirror;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Server {
    // The flight field's side of the run: at every launch the server generates a new field and publishes it, at the end
    // of a flight it takes the field down; a world shift moves the field's origin and its Mirror objects.
    public sealed class FlightFieldService : IFlightRouteListener, IWorldShiftFollower {
        private readonly IFieldRequestFactory _fieldRequestFactory;
        private readonly IFlightFieldGenerator _flightFieldGenerator;
        private readonly FlightFieldServerModel _flightFieldServerModel;
        private readonly IFlightFieldPublisher _flightFieldPublisher;
        private readonly IFieldNetworkedStructures _fieldNetworkedStructures;

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
            _flightFieldServerModel.SetField(_flightFieldGenerator.Generate(request));
            _fieldNetworkedStructures.ServerSpawn(_flightFieldServerModel);
            _flightFieldPublisher.Publish(_flightFieldServerModel.Field, _flightFieldServerModel.Number);
        }

        // Also called when the run is reset after the server stopped: Mirror has destroyed the spawned objects and the
        // bridge then, only the server model is left to clear.
        public void ServerOnFlightEnded() {
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
