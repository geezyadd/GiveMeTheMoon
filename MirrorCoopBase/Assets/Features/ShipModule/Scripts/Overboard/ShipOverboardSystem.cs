using System;
using System.Collections.Generic;
using Features.ShipModule.Scripts.Contracts;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts.Overboard {
    // Server, every tick: asks the overboard rule about every player against the run's ship and carries the answer
    // out (overboard, back aboard, past the limit). A player is aboard when a seat binds it or the ship carries its
    // body (IShipRiders.IsAboard; for a client's player that is the server's copy, which rides the carrier its owner's
    // packets name). Without a ship (the lobby, a map change) nobody drifts.
    public sealed class ShipOverboardSystem : ITickable {
        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly IReadOnlyComponentRegistry<ShipOverboardRider> _overboardRiders;
        private readonly IShipOverboardRule _shipOverboardRule;
        private readonly IShipOverboardEventPublisher _shipOverboardEventPublisher;

        public ShipOverboardSystem(
            IShipRunBindingModel shipRunBindingModel,
            IReadOnlyComponentRegistry<ShipOverboardRider> overboardRiders,
            IShipOverboardRule shipOverboardRule,
            IShipOverboardEventPublisher shipOverboardEventPublisher) {
            _shipRunBindingModel = shipRunBindingModel;
            _overboardRiders = overboardRiders;
            _shipOverboardRule = shipOverboardRule;
            _shipOverboardEventPublisher = shipOverboardEventPublisher;
        }

        public void Tick() {
            if (NetworkServer.active == false)
                return;

            ShipBase ship = _shipRunBindingModel.Ship;
            IReadOnlyList<ShipOverboardRider> riders = _overboardRiders.Items;
            for (int i = riders.Count - 1; i >= 0; i--) {
                ShipOverboardRider overboardRider = riders[i];
                if (overboardRider == null || overboardRider.isServer == false)
                    continue;

                if (ship == null)
                    overboardRider.ServerReset();
                else
                    Step(ship, overboardRider);
            }
        }

        private void Step(ShipBase ship, ShipOverboardRider overboardRider) {
            ShipOverboardDecision decision = _shipOverboardRule.Decide(ReadState(ship, overboardRider));
            switch (decision) {
                case ShipOverboardDecision.None:
                    break;
                case ShipOverboardDecision.MarkRidden:
                    overboardRider.HasRiddenThisFlight = true;
                    break;
                case ShipOverboardDecision.ForgetRide:
                    overboardRider.HasRiddenThisFlight = false;
                    break;
                case ShipOverboardDecision.GoOverboard:
                    overboardRider.ServerBeginOverboard();
                    break;
                case ShipOverboardDecision.EndOnDeck:
                    overboardRider.ServerEnd();
                    break;
                case ShipOverboardDecision.Expire:
                    overboardRider.ServerReset();
                    _shipOverboardEventPublisher.PublishDriftLimitReached(overboardRider.netIdentity);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(decision), decision, null);
            }
        }

        private ShipOverboardState ReadState(ShipBase ship, ShipOverboardRider overboardRider) {
            Vector3 position = overboardRider.Position;
            bool isInShipVolume = _shipOverboardRule.IsInShipVolume(ship.DeckGeometry, ship.ToShipLocal(position));
            float distance = Vector3.Distance(position, ship.transform.position);
            bool isAboard = ship.Riders.IsAboard(overboardRider.Rider);
            return new ShipOverboardState(ship.IsFlying, overboardRider.IsOverboard, overboardRider.HasRiddenThisFlight, isAboard,
                isInShipVolume, overboardRider.DriftSeconds, distance);
        }
    }
}
