using System;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Features.ShipModule.Scripts.Throttle;
using UnityEngine;

namespace Features.TooltipModule.Scripts {
    // The throttle handle's tooltip names its current mode and share of the max speed; at a station it adds how to
    // take off, or why the ship cannot (the handle then stays at Stop).
    public sealed class ShipThrottleTooltipProvider : ITooltipStateProvider {
        private const string MODE_FORMAT = "{0} ({1}%)";
        private const string LINE_FORMAT = "{0}\n{1}";
        private const float PERCENT = 100f;

        private readonly IReadOnlyShipThrottleModel _shipThrottleModel;
        private readonly IReadOnlyShipRunModel _shipRunModel;
        private readonly IReadOnlyShipSocketsModel _shipSocketsModel;
        private readonly ShipThrottleConfiguration _shipThrottleConfiguration;
        private readonly IShipThrottleDetentRule _shipThrottleDetentRule;

        public ShipThrottleTooltipProvider(
            IReadOnlyShipThrottleModel shipThrottleModel,
            IReadOnlyShipRunModel shipRunModel,
            IReadOnlyShipSocketsModel shipSocketsModel,
            ShipThrottleConfiguration shipThrottleConfiguration,
            IShipThrottleDetentRule shipThrottleDetentRule) {
            _shipThrottleModel = shipThrottleModel;
            _shipRunModel = shipRunModel;
            _shipSocketsModel = shipSocketsModel;
            _shipThrottleConfiguration = shipThrottleConfiguration;
            _shipThrottleDetentRule = shipThrottleDetentRule;
        }

        public event Action OnStateChanged {
            add {
                _shipThrottleModel.OnChanged += value;
                _shipRunModel.OnChanged += value;
                _shipSocketsModel.OnStatesChanged += value;
            }
            remove {
                _shipThrottleModel.OnChanged -= value;
                _shipRunModel.OnChanged -= value;
                _shipSocketsModel.OnStatesChanged -= value;
            }
        }

        public bool TrySelect(InteractionTooltip tooltip, out TooltipData data, out string description) {
            data = null;
            description = null;
            if (tooltip.TryGetComponent(out ShipThrottleInteractable throttle) == false)
                return false;

            data = tooltip.Select(true);
            string mode = DescribeMode(_shipThrottleModel.Position);
            string station = DescribeStation(throttle.LaunchReadiness);
            description = string.IsNullOrEmpty(station) ? mode : string.Format(LINE_FORMAT, mode, station);
            return true;
        }

        private string DescribeMode(float position) =>
            string.Format(MODE_FORMAT, _shipThrottleDetentRule.FindNearestName(position), Mathf.RoundToInt(position * PERCENT));

        private string DescribeStation(ShipLaunchReadiness readiness) {
            switch (readiness) {
                case ShipLaunchReadiness.Ready:
                    return _shipThrottleConfiguration.LaunchHint;
                case ShipLaunchReadiness.MissingRequiredModule:
                    return _shipThrottleConfiguration.MissingModuleReason;
                case ShipLaunchReadiness.RouteEnded:
                    return _shipThrottleConfiguration.RouteEndedReason;
                case ShipLaunchReadiness.NotAtStation:
                    return string.Empty;
                default:
                    throw new ArgumentOutOfRangeException(nameof(readiness), readiness, "A launch readiness the throttle tooltip does not describe.");
            }
        }
    }
}
