using System;
using System.Collections.Generic;
using Features.ShipModule.Scripts.Contracts;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts.Overboard {
    // Server, every tick: asks the overboard rule about every player and carries the answer out.
    // - In flight, against the run's ship: a player is aboard when a seat binds it or the ship carries its body
    //   (IShipRiders.IsAboard; for a client's player that is the server's copy, which rides the carrier its owner's
    //   packets name); past the limit the void takes it.
    // - On the ground (no ship: the lobby; or the ship standing at a station), against the overboard areas and the
    //   standing ship's volume; past the area limit the player is put back at the nearest area's return point, or on
    //   the ship's deck.
    // A takeoff or a landing starts the safe space over, so a player left at a station does not go overboard when the
    // ship leaves.
    public sealed class ShipOverboardSystem : ITickable {
        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly IReadOnlyComponentRegistry<ShipOverboardRider> _overboardRiders;
        private readonly IShipOverboardRule _shipOverboardRule;
        private readonly IShipOverboardEventPublisher _shipOverboardEventPublisher;
        private readonly OverboardAreas _overboardAreas;
        private readonly IShipDeckSpawnPoints _shipDeckSpawnPoints;

        public ShipOverboardSystem(
            IShipRunBindingModel shipRunBindingModel,
            IReadOnlyComponentRegistry<ShipOverboardRider> overboardRiders,
            IShipOverboardRule shipOverboardRule,
            IShipOverboardEventPublisher shipOverboardEventPublisher,
            OverboardAreas overboardAreas,
            IShipDeckSpawnPoints shipDeckSpawnPoints) {
            _shipRunBindingModel = shipRunBindingModel;
            _overboardRiders = overboardRiders;
            _shipOverboardRule = shipOverboardRule;
            _shipOverboardEventPublisher = shipOverboardEventPublisher;
            _overboardAreas = overboardAreas;
            _shipDeckSpawnPoints = shipDeckSpawnPoints;
        }

        public void Tick() {
            if (NetworkServer.active == false)
                return;

            ShipBase ship = _shipRunBindingModel.Ship;
            bool isFlying = ship != null && ship.IsFlying;
            IReadOnlyList<ShipOverboardRider> riders = _overboardRiders.Items;
            for (int i = riders.Count - 1; i >= 0; i--) {
                ShipOverboardRider overboardRider = riders[i];
                if (overboardRider == null || overboardRider.isServer == false)
                    continue;

                if (overboardRider.WasFlying != isFlying) {
                    overboardRider.WasFlying = isFlying;
                    overboardRider.HasBeenInside = false;
                }

                ShipOverboardState state = isFlying ? ReadFlightState(ship, overboardRider) : ReadGroundState(ship, overboardRider);
                Carry(_shipOverboardRule.Decide(state), ship, overboardRider);
            }
        }

        private void Carry(ShipOverboardDecision decision, ShipBase ship, ShipOverboardRider overboardRider) {
            switch (decision) {
                case ShipOverboardDecision.None:
                    break;
                case ShipOverboardDecision.MarkInside:
                    overboardRider.HasBeenInside = true;
                    break;
                case ShipOverboardDecision.GoOverboard:
                    overboardRider.ServerBeginOverboard();
                    break;
                case ShipOverboardDecision.EndDrift:
                    overboardRider.ServerEnd();
                    break;
                case ShipOverboardDecision.Expire:
                    overboardRider.ServerReset();
                    _shipOverboardEventPublisher.PublishDriftLimitReached(overboardRider.netIdentity);
                    break;
                case ShipOverboardDecision.ReturnToArea:
                    ReturnToArea(ship, overboardRider);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(decision), decision, null);
            }
        }

        private ShipOverboardState ReadFlightState(ShipBase ship, ShipOverboardRider overboardRider) {
            Vector3 position = overboardRider.Position;
            bool isInShipVolume = _shipOverboardRule.IsInShipVolume(ship.DeckGeometry, ship.ToShipLocal(position));
            float distance = Vector3.Distance(position, ship.transform.position);
            bool isAboard = ship.Riders.IsAboard(overboardRider.Rider);
            return new ShipOverboardState(true, overboardRider.IsOverboard, overboardRider.HasBeenInside, isAboard,
                isInShipVolume, overboardRider.DriftSeconds, distance);
        }

        // The safe space on the ground: any overboard area, or the volume of the ship standing at a station.
        private ShipOverboardState ReadGroundState(ShipBase ship, ShipOverboardRider overboardRider) {
            Vector3 position = overboardRider.Position;
            bool isInShipVolume = ship != null && _shipOverboardRule.IsInShipVolume(ship.DeckGeometry, ship.ToShipLocal(position));
            bool isInside = isInShipVolume || _overboardAreas.Contains(position);
            bool isOverFloor = (ship != null && _shipOverboardRule.IsOverShipDeck(ship.DeckGeometry, ship.ToShipLocal(position)))
                || _overboardAreas.IsOverFloor(position);
            float distance = _overboardAreas.TryFindNearest(position, false, out _, out float areaDistance) ? areaDistance : 0f;
            return new ShipOverboardState(false, overboardRider.IsOverboard, overboardRider.HasBeenInside, false,
                isInside, overboardRider.DriftSeconds, distance, isOverFloor);
        }

        // Back at the return point of the nearest area that has one (the lobby's); without one (a station) the player
        // goes onto the standing ship's deck.
        private void ReturnToArea(ShipBase ship, ShipOverboardRider overboardRider) {
            Vector3 position = overboardRider.Position;
            if (_overboardAreas.TryFindNearest(position, true, out OverboardArea area, out _)) {
                overboardRider.ServerReturn(area.ReturnPose);
                return;
            }

            if (ship != null && _shipDeckSpawnPoints.HasSpawnPoint) {
                overboardRider.ServerReturn(_shipDeckSpawnPoints.TakeSpawnPose());
                return;
            }

            throw new InvalidOperationException("Overboard: " + overboardRider.name + " drifted too far and there is no return point to put it at.");
        }
    }
}
