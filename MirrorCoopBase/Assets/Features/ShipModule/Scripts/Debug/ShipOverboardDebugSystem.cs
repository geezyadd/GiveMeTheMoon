#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Features.CharacterMovableModule.Scripts;
using Features.CharacterMovableModule.Scripts.Models;
using Features.DebugModule.Contracts;
using Features.ShipModule.Scripts.Contracts;
using Features.ShipModule.Scripts.Overboard;
using Zenject;

namespace Features.ShipModule.Scripts.Debug {
    // Debug only: the debug window's Player -> "Throw me overboard" (any player riding in flight: its own peer lets go
    // of the deck and puts the body beside it, so the server sees it leave the ship's volume as after a walk off the
    // edge) and Ship -> "Bring the ship to the drifting player" (host: the ship is moved like a world shift until the
    // first drifting player hangs over its deck, inside the volume that takes it back).
    public sealed class ShipOverboardDebugSystem : IInitializable, IDisposable, IShipOverboardDebug {
        private const string THROW_LABEL = "Throw me overboard";
        private const string THROW_DESCRIPTION = "Lets go of the deck and puts you beside the ship in flight: you drift until the ship or a rope takes you back.";
        private const string BRING_LABEL = "Bring the ship to the drifting player";
        private const string BRING_DESCRIPTION = "Moves the ship until the first drifting player hangs over its deck, so the ship takes that player back.";
        private const string NOT_RIDING_REASON = "Only while you ride the ship's deck.";
        private const string NOBODY_OVERBOARD_REASON = "Nobody drifts overboard.";

        private readonly IShipClientBindingModel _shipClientBindingModel;
        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly CharacterMovableModel _characterMovableModel;
        private readonly IReadOnlyComponentRegistry<ShipOverboardRider> _overboardRiders;
        private readonly IShipOverboardDebugPlacement _shipOverboardDebugPlacement;
        private readonly IDebugActionRegistry _debugActionRegistry;
        private readonly IShipDebugActionConditions _shipDebugActionConditions;

        private IDisposable _throwRegistration;
        private IDisposable _bringRegistration;

        public ShipOverboardDebugSystem(
            IShipClientBindingModel shipClientBindingModel,
            IShipRunBindingModel shipRunBindingModel,
            CharacterMovableModel characterMovableModel,
            IReadOnlyComponentRegistry<ShipOverboardRider> overboardRiders,
            IShipOverboardDebugPlacement shipOverboardDebugPlacement,
            IDebugActionRegistry debugActionRegistry,
            IShipDebugActionConditions shipDebugActionConditions) {
            _shipClientBindingModel = shipClientBindingModel;
            _shipRunBindingModel = shipRunBindingModel;
            _characterMovableModel = characterMovableModel;
            _overboardRiders = overboardRiders;
            _shipOverboardDebugPlacement = shipOverboardDebugPlacement;
            _debugActionRegistry = debugActionRegistry;
            _shipDebugActionConditions = shipDebugActionConditions;
        }

        public void Initialize() {
            _throwRegistration = _debugActionRegistry.Register(new DebugActionDefinition(DebugSections.PLAYER, THROW_LABEL, THROW_DESCRIPTION, ReadThrowReason, DebugThrowLocalPlayer));
            _bringRegistration = _debugActionRegistry.Register(new DebugActionDefinition(DebugSections.SHIP, BRING_LABEL, BRING_DESCRIPTION, ReadBringReason, DebugBringShipToDrifter));
        }

        public void Dispose() {
            _throwRegistration.Dispose();
            _bringRegistration.Dispose();
        }

        public void DebugThrowLocalPlayer() {
            if (TryGetLocalRider(out CharacterMovableBase movable, out ShipRider rider))
                _shipOverboardDebugPlacement.ThrowOverboard(_shipClientBindingModel.Ship, rider, movable.Body);
        }

        public void DebugBringShipToDrifter() {
            if (TryFindDrifter(out ShipOverboardRider drifter))
                _shipOverboardDebugPlacement.BringShipTo(_shipRunBindingModel.Ship, drifter.Position);
        }

        private string ReadThrowReason() {
            if (_characterMovableModel.LocalMovable == null)
                return DebugActionReasons.NO_LOCAL_PLAYER;

            if (_shipClientBindingModel.Ship == null || _shipClientBindingModel.Ship.IsFlying == false)
                return DebugActionReasons.NOT_FLYING;

            return TryGetLocalRider(out _, out _) ? null : NOT_RIDING_REASON;
        }

        private string ReadBringReason() {
            string reason = _shipDebugActionConditions.ReadHostInFlightReason();
            if (reason != null)
                return reason;

            return TryFindDrifter(out _) ? null : NOBODY_OVERBOARD_REASON;
        }

        private bool TryGetLocalRider(out CharacterMovableBase movable, out ShipRider rider) {
            rider = null;
            movable = _characterMovableModel.LocalMovable;
            return movable != null && movable.TryGetComponent(out rider) && rider.IsRiding && rider.IsSeated == false;
        }

        private bool TryFindDrifter(out ShipOverboardRider drifter) {
            IReadOnlyList<ShipOverboardRider> riders = _overboardRiders.Items;
            for (int i = 0; i < riders.Count; i++) {
                drifter = riders[i];
                if (drifter != null && drifter.IsOverboard)
                    return true;
            }

            drifter = null;
            return false;
        }
    }
}
#endif
