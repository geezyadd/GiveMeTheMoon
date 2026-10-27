#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Tests.Coop.Shared;
using UnityEngine;

namespace Tests.Coop.Client {
    // The client bot of each coop scenario other than CoopTestClient's leave-and-rejoin, by its -scenario name.
    public static class CoopClientScenarios {
        private static readonly Dictionary<string, Type> _bots = new Dictionary<string, Type> {
            { CoopFlightSteps.SCENARIO_THROTTLE_AND_HELM, typeof(CoopThrottleAndHelmClient) },
            { CoopFlightSteps.SCENARIO_DECK_ITEMS, typeof(CoopDeckItemsClient) },
            { CoopFlightSteps.SCENARIO_NAVIGATION_VIEW, typeof(CoopNavigationViewClient) },
            { CoopFlightSteps.SCENARIO_ROCKS_AND_HULL_LOSS, typeof(CoopRocksAndHullLossClient) }
        };

        public static bool TryStart(GameObject clientObject, CoopTestArguments arguments) {
            if (_bots.TryGetValue(arguments.Scenario, out Type bot) == false)
                return false;

            ((CoopScenarioClientBot)clientObject.AddComponent(bot)).Run(arguments);
            return true;
        }
    }
}
#endif
