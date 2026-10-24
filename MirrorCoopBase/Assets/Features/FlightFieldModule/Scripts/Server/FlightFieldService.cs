using System.Threading.Tasks;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Requests;
using Features.FlightFieldModule.Scripts.Rocks;
using Features.GameCoreModule.Contracts;
using Mirror;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Server {
    // The server's operations on the flight field: generate one for a launch, publish it, take it down; a world shift
    // moves the field's origin and its Mirror objects. FlightFieldLaunchSystem drives them over a flight. The broken
    // rocks belong to one field: a new field or taking it down forgets them.
    // A long flight's field takes over 100 ms to generate, so generation runs on a worker thread (it touches no Unity
    // object).
    public sealed class FlightFieldService : IFlightFieldService, IWorldShiftFollower {
        private readonly IFieldRequestFactory _fieldRequestFactory;
        private readonly IFlightFieldGenerator _flightFieldGenerator;
        private readonly FlightFieldServerModel _flightFieldServerModel;
        private readonly IFlightFieldPublisher _flightFieldPublisher;
        private readonly IFieldNetworkedStructures _fieldNetworkedStructures;
        private readonly IFieldRockReset _fieldRockReset;

        public FlightFieldService(IFieldRequestFactory fieldRequestFactory, IFlightFieldGenerator flightFieldGenerator, FlightFieldServerModel flightFieldServerModel,
                                  IFlightFieldPublisher flightFieldPublisher, IFieldNetworkedStructures fieldNetworkedStructures, IFieldRockReset fieldRockReset) {
            _fieldRequestFactory = fieldRequestFactory;
            _flightFieldGenerator = flightFieldGenerator;
            _flightFieldServerModel = flightFieldServerModel;
            _flightFieldPublisher = flightFieldPublisher;
            _fieldNetworkedStructures = fieldNetworkedStructures;
            _fieldRockReset = fieldRockReset;
        }

        public Task<FlightField> ServerStartGeneration(FlightLaunch launch) {
            int seed = Random.Range(0, int.MaxValue);
            FieldRequest request = _fieldRequestFactory.Create(launch.StationA, launch.StationB, launch.CruiseStart, launch.LoopIndex, seed);
            return Task.Run(() => _flightFieldGenerator.Generate(request));
        }

        public void ServerPublish(FlightField field) {
            _fieldRockReset.ServerReset();
            _flightFieldServerModel.SetField(field);
            _fieldNetworkedStructures.ServerSpawn(_flightFieldServerModel);
            _flightFieldPublisher.Publish(_flightFieldServerModel.Field, _flightFieldServerModel.Number);
        }

        // Also called when the run is reset after the server stopped: Mirror has destroyed the spawned objects and the
        // bridge then, only the server model is left to clear.
        public void ServerClear() {
            if (!_flightFieldServerModel.HasField)
                return;

            if (NetworkServer.active) {
                _fieldNetworkedStructures.ServerDespawn(_flightFieldServerModel);
                _flightFieldPublisher.Clear();
                _fieldRockReset.ServerReset();
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

            _flightFieldServerModel.ShiftFrame(delta);
            _fieldNetworkedStructures.ServerShift(_flightFieldServerModel, delta);
            _flightFieldPublisher.ShiftOrigin(delta);
        }
    }
}
