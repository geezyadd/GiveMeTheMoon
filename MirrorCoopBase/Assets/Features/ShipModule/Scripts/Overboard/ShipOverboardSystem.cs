using System.Collections.Generic;
using Features.ShipModule.Scripts.Contracts;
using Features.WeaponModule.Contracts;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts.Overboard {
    // Server, every tick: asks the overboard rule about every player against the run's ship and carries the answer
    // out (overboard, back on the deck, past the limit), and lets go of a rope whose holder let go or went too far.
    // Without a ship (the lobby, a map change) nobody drifts.
    public sealed class ShipOverboardSystem : ITickable {
        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly IReadOnlyComponentRegistry<ShipOverboardRider> _overboardRiders;
        private readonly IShipOverboardRule _shipOverboardRule;
        private readonly IShipOverboardEventPublisher _shipOverboardEventPublisher;
        private readonly IWeaponHolderAnchors _weaponHolderAnchors;

        public ShipOverboardSystem(
            IShipRunBindingModel shipRunBindingModel,
            IReadOnlyComponentRegistry<ShipOverboardRider> overboardRiders,
            IShipOverboardRule shipOverboardRule,
            IShipOverboardEventPublisher shipOverboardEventPublisher,
            IWeaponHolderAnchors weaponHolderAnchors) {
            _shipRunBindingModel = shipRunBindingModel;
            _overboardRiders = overboardRiders;
            _shipOverboardRule = shipOverboardRule;
            _shipOverboardEventPublisher = shipOverboardEventPublisher;
            _weaponHolderAnchors = weaponHolderAnchors;
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
                case ShipOverboardDecision.Capture:
                    overboardRider.ServerCapture(ship);
                    break;
                case ShipOverboardDecision.Expire:
                    overboardRider.ServerReset();
                    _shipOverboardEventPublisher.PublishDriftLimitReached(overboardRider.netIdentity);
                    break;
            }

            if (overboardRider.IsHooked && IsRopeHeld(overboardRider) == false)
                overboardRider.ServerUnhook();
        }

        private ShipOverboardState ReadState(ShipBase ship, ShipOverboardRider overboardRider) {
            Vector3 position = overboardRider.Position;
            bool isInShipVolume = _shipOverboardRule.IsInShipVolume(ship.DeckGeometry, ship.ToShipLocal(position));
            float distance = Vector3.Distance(position, ship.transform.position);
            return new ShipOverboardState(ship.IsFlying, overboardRider.IsOverboard, overboardRider.HasRiddenThisFlight, overboardRider.Rider.HasSyncedRide,
                isInShipVolume, overboardRider.DriftSeconds, distance);
        }

        private bool IsRopeHeld(ShipOverboardRider overboardRider) {
            bool hasAnchor = _weaponHolderAnchors.TryGetAnchor(overboardRider.HookerKey, out WeaponHolderAnchor anchor);
            bool hasHolderWeapon = hasAnchor && anchor.WeaponId == overboardRider.HookWeaponId;
            float distance = hasAnchor ? Vector3.Distance(anchor.Origin, overboardRider.Position) : float.MaxValue;
            return _shipOverboardRule.IsRopeHeld(hasHolderWeapon, distance, overboardRider.MaxRopeMeters);
        }
    }
}
