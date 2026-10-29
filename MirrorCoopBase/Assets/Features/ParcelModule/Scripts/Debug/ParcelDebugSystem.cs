#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Features.DebugModule.Contracts;
using Features.ParcelModule.Scripts.Rules;
using Features.ParcelModule.Scripts.Server;
using Features.ParcelModule.Scripts.Services;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Debug;
using Features.ShipModule.Scripts.Generated;
using UnityEngine;
using Zenject;

namespace Features.ParcelModule.Scripts.Debug {
    // Debug only, on the host: "Give the ship a parcel" puts a parcel for the next station on the deck's stand point,
    // as if the crew had taken one at the stand and carried it aboard; in flight the deck catches it at once.
    public sealed class ParcelDebugSystem : IInitializable, IDisposable {
        private const string GIVE_LABEL = "Give the ship a parcel";
        private const string GIVE_DESCRIPTION = "A parcel for the next station lands on the deck: bugs go for it in flight, delivered it pays at the next station.";
        private const string NO_DECK_POINT_REASON = "The deck has no stand point.";
        private const float DROP_HEIGHT_METERS = 1f;
        private const float REWARD_ROLL = 0.5f;

        private readonly IDebugActionRegistry _debugActionRegistry;
        private readonly IShipDebugActionConditions _shipDebugActionConditions;
        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly IReadOnlyShipRunModel _shipRunModel;
        private readonly IParcelCrateService _parcelCrateService;
        private readonly IParcelOfferRule _parcelOfferRule;

        private IDisposable _giveRegistration;

        public ParcelDebugSystem(
            IDebugActionRegistry debugActionRegistry,
            IShipDebugActionConditions shipDebugActionConditions,
            IShipRunBindingModel shipRunBindingModel,
            IReadOnlyShipRunModel shipRunModel,
            IParcelCrateService parcelCrateService,
            IParcelOfferRule parcelOfferRule) {
            _debugActionRegistry = debugActionRegistry;
            _shipDebugActionConditions = shipDebugActionConditions;
            _shipRunBindingModel = shipRunBindingModel;
            _shipRunModel = shipRunModel;
            _parcelCrateService = parcelCrateService;
            _parcelOfferRule = parcelOfferRule;
        }

        public void Initialize() =>
            _giveRegistration = _debugActionRegistry.Register(new DebugActionDefinition(DebugSections.AGENTS, GIVE_LABEL, GIVE_DESCRIPTION, ReadGiveReason, GiveParcel));

        public void Dispose() =>
            _giveRegistration.Dispose();

        private string ReadGiveReason() {
            string reason = _shipDebugActionConditions.ReadHostWithShipReason();
            if (reason != null)
                return reason;

            return _shipRunBindingModel.Ship.DeckGeometry.TryGetDeckStandPoint(out _) ? null : NO_DECK_POINT_REASON;
        }

        private void GiveParcel() {
            ShipBase ship = _shipRunBindingModel.Ship;
            if (ship.DeckGeometry.TryGetDeckStandPoint(out Vector3 standPoint) == false)
                return;

            Vector3 position = ship.transform.TransformPoint(standPoint + Vector3.up * DROP_HEIGHT_METERS);
            ParcelServerRecord record = _parcelCrateService.ServerSpawn(position, ship.transform.rotation, _parcelOfferRule.CalculateReward(REWARD_ROLL), _shipRunModel.LoopIndex + 1);
            if (ship.IsFlying)
                ship.ServerCatchDeckItem(record.Crate.Grabbable);
        }
    }
}
#endif
