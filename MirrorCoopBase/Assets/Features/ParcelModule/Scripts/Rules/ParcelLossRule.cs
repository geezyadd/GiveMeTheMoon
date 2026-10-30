using System;
using Features.ParcelModule.Data;
using Features.ParcelModule.Scripts.Configuration;

namespace Features.ParcelModule.Scripts.Rules {
    // A thief that got too far from the ship or flew too long takes the parcel for good; a drifting parcel is lost the
    // same way after its own time; a loose crate that fell far below the deck went overboard. A parcel on a rope is
    // never lost by itself: the rope breaking puts it back adrift.
    public sealed class ParcelLossRule : IParcelLossRule {
        private readonly ParcelConfiguration _parcelConfiguration;

        public ParcelLossRule(ParcelConfiguration parcelConfiguration) =>
            _parcelConfiguration = parcelConfiguration;

        public bool IsLost(ParcelState state, float distanceToShip, float secondsInState, float heightAboveDeck) =>
            state switch {
                ParcelState.Loose => heightAboveDeck < -_parcelConfiguration.FallLoseMeters,
                ParcelState.Carried => IsTooFar(distanceToShip) || secondsInState > _parcelConfiguration.LoseCarrySeconds,
                ParcelState.Drifting => IsTooFar(distanceToShip) || secondsInState > _parcelConfiguration.LoseDriftSeconds,
                ParcelState.Pulled => false,
                _ => throw new ArgumentOutOfRangeException(nameof(state), state, "A parcel in the model has no state.")
            };

        private bool IsTooFar(float distanceToShip) =>
            distanceToShip > _parcelConfiguration.LoseDistanceMeters;
    }
}
