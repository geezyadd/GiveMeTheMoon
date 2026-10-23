using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation.Placement;
using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Steps {
    // Reserve: the takeoff corridor from station A past the cruise start stays empty along the takeoff heading. The
    // autopilot keeps that heading after takeoff, so the pilot needs room to find B on the radar and turn.
    public sealed class FieldTakeoffCorridorStep : IFieldGenerationStep {
        private const float DIRECTION_EPSILON = 0.0001f;

        private readonly FlightFieldConfiguration _flightFieldConfiguration;
        private readonly IFieldAreaReserver _fieldAreaReserver;

        public FieldTakeoffCorridorStep(FlightFieldConfiguration flightFieldConfiguration, IFieldAreaReserver fieldAreaReserver) {
            _flightFieldConfiguration = flightFieldConfiguration;
            _fieldAreaReserver = fieldAreaReserver;
        }

        public bool IsUsedBy(FieldLayoutKind layout) =>
            true;

        public void Execute(FieldGenerationContext context) {
            FieldRequest request = context.Request;
            Vector3 corridorEnd = request.CruiseStart + TakeoffForward(request) * _flightFieldConfiguration.TakeoffRunOutLength;
            _fieldAreaReserver.Reserve(context, request.StationA, corridorEnd, _flightFieldConfiguration.TakeoffCorridorHalfWidth,
                FieldReserveKind.TakeoffCorridor);
        }

        // The cruise start lies straight ahead of station A along the ship's nose, so it gives the takeoff heading.
        private static Vector3 TakeoffForward(FieldRequest request) {
            Vector3 forward = request.CruiseStart - request.StationA;
            forward.y = 0f;
            return forward.sqrMagnitude < DIRECTION_EPSILON ? Vector3.zero : forward.normalized;
        }
    }
}
