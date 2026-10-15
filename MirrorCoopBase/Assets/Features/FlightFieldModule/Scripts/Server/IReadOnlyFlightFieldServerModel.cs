using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Model;
using Mirror;

namespace Features.FlightFieldModule.Scripts.Server {
    public interface IReadOnlyFlightFieldServerModel {
        public bool HasField { get; }
        // The generated field with its working grid (station zones, corridors) for the debug map; null without a field.
        public FlightField Field { get; }
        public int Number { get; }
        public IReadOnlyList<NetworkIdentity> NetworkedStructures { get; }

        public event Action OnFieldChanged;
    }
}
