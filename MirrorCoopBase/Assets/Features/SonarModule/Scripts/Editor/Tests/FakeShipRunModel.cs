using System;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using UnityEngine;

namespace Features.SonarModule.Editor.Tests {
    // The synced run as a test sets it: a phase and a destination.
    public sealed class FakeShipRunModel : IReadOnlyShipRunModel {
        public ShipRunPhase Phase { get; set; }
        public Vector3 TransitDestination { get; set; }
        public bool IsAvailable { get; set; } = true;
        public int LoopIndex => 0;
        public double CruiseEndNetworkTime => 0d;
        public bool LaunchLocked => false;
        public ShipRunAbortReason LastAbortReason => ShipRunAbortReason.None;
        public float TransitWorkRemaining => 0f;
        public float TransitSpeed => 0f;
        public float TransitAlignment => 0f;
        public float TransitSecondsRemaining => 0f;

        public event Action OnPhaseChanged { add { } remove { } }
        public event Action OnLoopIndexChanged { add { } remove { } }
        public event Action OnCruiseEndNetworkTimeChanged { add { } remove { } }
        public event Action OnLaunchLockedChanged { add { } remove { } }
        public event Action OnLastAbortReasonChanged { add { } remove { } }
        public event Action OnTransitWorkRemainingChanged { add { } remove { } }
        public event Action OnTransitSpeedChanged { add { } remove { } }
        public event Action OnTransitAlignmentChanged { add { } remove { } }
        public event Action OnTransitDestinationChanged { add { } remove { } }
        public event Action OnTransitSecondsRemainingChanged { add { } remove { } }
        public event Action OnChanged { add { } remove { } }
        public event Action OnAvailableChanged { add { } remove { } }
    }
}
