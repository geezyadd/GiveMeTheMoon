using System;
using System.Collections.Generic;
using Features.CameraModule.Contracts;
using UnityEngine;

namespace Features.ShipModule.Scripts.Throttle {
    // The throttle lever: its detents, the launch mark, how heavy the handle feels, how often the holder sends it and
    // the texts its tooltip shows.
    [CreateAssetMenu(
        fileName = nameof(ShipThrottleConfiguration) + "_Default",
        menuName = "Configurations/ShipModule/" + nameof(ShipThrottleConfiguration))]
    public sealed class ShipThrottleConfiguration : ScriptableObject {
        [Serializable]
        public sealed class Detent {
            [Tooltip("Where the detent sits: share of the max cruise speed, 0..1.")]
            [Range(0f, 1f)] public float Position;
            [Tooltip("The mode the tooltip names at this detent.")]
            public string Name;
        }

        [Header("Detents and launch")]
        [Tooltip("Soft stops along the arc, from Stop (0) to Full (1).")]
        [SerializeField] private List<Detent> _detents = new();
        [Tooltip("At a station, pushing the handle to this position takes off.")]
        [SerializeField, Range(0f, 1f)] private float _launchMark = 0.15f;

        [Header("Handle feel")]
        [Tooltip("Throttle moved per unit of mouse look input (pixels).")]
        [SerializeField, Min(0f)] private float _throttlePerLookUnit = 0.0025f;
        [Tooltip("How long the heavy handle takes to catch up with the hand, seconds.")]
        [SerializeField, Min(0f)] private float _handleSmoothSeconds = 0.12f;
        [Tooltip("The fastest the handle moves, throttle per second.")]
        [SerializeField, Min(0.01f)] private float _handleMaxSpeed = 2.5f;
        [Tooltip("Within this distance of a detent the handle is pulled towards it.")]
        [SerializeField, Range(0f, 0.5f)] private float _detentPullRadius = 0.06f;
        [Tooltip("How strongly a detent pulls the handle: 0 = not at all, 1 = snaps onto it.")]
        [SerializeField, Range(0f, 1f)] private float _detentPullStrength = 0.6f;
        [Tooltip("The handle clicks into a detent when it comes this close to it.")]
        [SerializeField, Range(0f, 0.1f)] private float _detentClickRadius = 0.015f;
        [Tooltip("Shake of the holder's view on a detent click.")]
        [SerializeField] private CameraShakeProfile _detentClickShake;
        [SerializeField, Min(0f)] private float _detentClickShakeStrength = 1f;
        [Tooltip("Handle angle at Stop and at Full around its pivot, degrees.")]
        [SerializeField] private float _stopAngle = -35f;
        [SerializeField] private float _fullAngle = 35f;

        [Header("Network")]
        [Tooltip("The holder sends the handle position at most this often, seconds.")]
        [SerializeField, Min(0f)] private float _sendIntervalSeconds = 0.05f;
        [Tooltip("A take the server has not confirmed within this time ends the local hold, seconds.")]
        [SerializeField, Min(0.1f)] private float _takeTimeoutSeconds = 1f;
        [Tooltip("Other players see the handle ease to the synced position over this time, seconds.")]
        [SerializeField, Min(0f)] private float _remoteSmoothSeconds = 0.12f;

        [Header("Tooltip")]
        [Tooltip("Shown at a station when the ship is ready to leave.")]
        [SerializeField] private string _launchHint = "Push past the launch mark to take off.";
        [Tooltip("Shown at a station when a module the flight needs is not installed.")]
        [SerializeField] private string _missingModuleReason = "Install both engines to take off.";
        [Tooltip("Shown at the last station of a run: the ship flies no further.")]
        [SerializeField] private string _routeEndedReason = "The route ends here: the ship flies no further.";

        public IReadOnlyList<Detent> Detents => _detents;
        public float LaunchMark => _launchMark;
        public float ThrottlePerLookUnit => _throttlePerLookUnit;
        public float HandleSmoothSeconds => _handleSmoothSeconds;
        public float HandleMaxSpeed => _handleMaxSpeed;
        public float DetentPullRadius => _detentPullRadius;
        public float DetentPullStrength => _detentPullStrength;
        public float DetentClickRadius => _detentClickRadius;
        public CameraShakeProfile DetentClickShake => _detentClickShake;
        public float DetentClickShakeStrength => _detentClickShakeStrength;
        public float StopAngle => _stopAngle;
        public float FullAngle => _fullAngle;
        public float SendIntervalSeconds => _sendIntervalSeconds;
        public float TakeTimeoutSeconds => _takeTimeoutSeconds;
        public float RemoteSmoothSeconds => _remoteSmoothSeconds;
        public string LaunchHint => _launchHint;
        public string MissingModuleReason => _missingModuleReason;
        public string RouteEndedReason => _routeEndedReason;
    }
}
