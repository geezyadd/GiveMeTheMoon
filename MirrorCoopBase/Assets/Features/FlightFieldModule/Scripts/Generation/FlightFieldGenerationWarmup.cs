using Features.FlightFieldModule.Scripts.Requests;
using UnityEngine;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Generation {
    // The first generation in a process pays the JIT of every step (about 20 ms against 1.5 ms later): one throwaway
    // field at startup keeps that out of the frame the launch lever is pulled in.
    public sealed class FlightFieldGenerationWarmup : IInitializable {
        private const float WARMUP_STATION_DISTANCE = 600f;
        private const float WARMUP_CRUISE_START_FORWARD = 350f;
        private const float WARMUP_CRUISE_START_HEIGHT = 100f;
        private const int WARMUP_LOOP = 0;
        private const int WARMUP_SEED = 1;

        private readonly IFlightFieldGenerator _flightFieldGenerator;
        private readonly IFieldRequestFactory _fieldRequestFactory;

        public FlightFieldGenerationWarmup(IFlightFieldGenerator flightFieldGenerator, IFieldRequestFactory fieldRequestFactory) {
            _flightFieldGenerator = flightFieldGenerator;
            _fieldRequestFactory = fieldRequestFactory;
        }

        public void Initialize() {
            Vector3 cruiseStart = Vector3.forward * WARMUP_CRUISE_START_FORWARD + Vector3.up * WARMUP_CRUISE_START_HEIGHT;
            _flightFieldGenerator.Generate(_fieldRequestFactory.Create(Vector3.zero, Vector3.forward * WARMUP_STATION_DISTANCE, cruiseStart, WARMUP_LOOP, WARMUP_SEED));
        }
    }
}
