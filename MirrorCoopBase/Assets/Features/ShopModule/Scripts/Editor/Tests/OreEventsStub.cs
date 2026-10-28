using System;
using Features.FlightFieldModule.Contracts;
using Features.NetworkModelModule.Scripts;
using UnityEngine;

namespace Features.ShopModule.Editor.Tests {
    // Raises ore deliveries by hand, as the server's ore collector would.
    public sealed class OreEventsStub : IFieldOreEvents {
        private const string KIND_ID = "ore";
        private const string HOOKER_ID = "client:hooker";
        private const float RADIUS = 1f;
        private const int FIELD_NUMBER = 1;
        private const int LOOP_INDEX = 0;

        public event Action<FieldOreCollected> OreCollected;

        public void Deliver(int id, int price) {
            FieldOreDescription ore = new(new FieldOreId(id), KIND_ID, Vector3.zero, RADIUS, price, new PlayerKey(HOOKER_ID));
            OreCollected?.Invoke(new FieldOreCollected(ore, FIELD_NUMBER, LOOP_INDEX));
        }
    }
}
