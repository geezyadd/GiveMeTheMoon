using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Targets;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Requests {
    // Creates the request of one flight: the route requests and the difficulty profile come from the configuration
    // entry of the loop, the target balloon count scale from the run's stage preset.
    public sealed class FieldRequestFactory : IFieldRequestFactory {
        private readonly FlightFieldConfiguration _flightFieldConfiguration;
        private readonly IFieldProfileSelector _fieldProfileSelector;
        private readonly IReadOnlyFieldTargetCountScaleModel _fieldTargetCountScaleModel;

        public FieldRequestFactory(FlightFieldConfiguration flightFieldConfiguration, IFieldProfileSelector fieldProfileSelector,
                                   IReadOnlyFieldTargetCountScaleModel fieldTargetCountScaleModel) {
            _flightFieldConfiguration = flightFieldConfiguration;
            _fieldProfileSelector = fieldProfileSelector;
            _fieldTargetCountScaleModel = fieldTargetCountScaleModel;
        }

        public FieldRequest Create(Vector3 stationA, Vector3 stationB, Vector3 cruiseStart, int loopIndex, int seed) {
            FlightFieldConfiguration.LoopRoutes loopRoutes = FindLoopRoutes(loopIndex);
            return new FieldRequest(stationA, stationB, cruiseStart, loopIndex, seed, CreateRouteRequests(loopRoutes), _fieldProfileSelector.Select(loopRoutes, seed),
                _fieldTargetCountScaleModel.CountScale);
        }

        private FlightFieldConfiguration.LoopRoutes FindLoopRoutes(int loopIndex) {
            FlightFieldConfiguration.LoopRoutes found = null;
            foreach (FlightFieldConfiguration.LoopRoutes loopRoutes in _flightFieldConfiguration.RoutesByLoop) {
                bool isLaterMatch = loopRoutes.FromLoop <= loopIndex && (found == null || loopRoutes.FromLoop > found.FromLoop);
                if (isLaterMatch)
                    found = loopRoutes;
            }

            return found ?? throw new InvalidOperationException($"{_flightFieldConfiguration.name}: no routes for loop {loopIndex}.");
        }

        private static List<FieldRouteRequest> CreateRouteRequests(FlightFieldConfiguration.LoopRoutes loopRoutes) {
            List<FieldRouteRequest> routeRequests = new();
            foreach (float difficulty in loopRoutes.RouteDifficulties)
                routeRequests.Add(new FieldRouteRequest(difficulty));

            return routeRequests;
        }
    }
}
