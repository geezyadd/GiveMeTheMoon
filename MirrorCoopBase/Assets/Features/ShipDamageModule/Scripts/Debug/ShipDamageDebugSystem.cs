#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Features.DebugModule.Contracts;
using Features.ShipDamageModule.Scripts.Damage;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Contracts;
using Features.ShipModule.Scripts.Debug;
using Zenject;
using Random = UnityEngine.Random;

namespace Features.ShipDamageModule.Scripts.Debug {
    // Debug only, on the host: the debug window's "Break a module + open a breach" action breaks a random working
    // module and opens a random free breach, through the same condition service a rock impact uses, so the stats,
    // the synced model and the events all see it.
    public sealed class ShipDamageDebugSystem : IInitializable, IDisposable, IShipDamageDebug {
        private const string BREAK_LABEL = "Break a module + open a breach";
        private const string BREAK_DESCRIPTION = "Breaks a random working module and opens a random hull breach, as a rock impact can: repair them with the wrench and the hammer.";

        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly IShipConditionService _shipConditionService;
        private readonly IShipModuleCondition _shipModuleCondition;
        private readonly IShipBreachCondition _shipBreachCondition;
        private readonly IDebugActionRegistry _debugActionRegistry;
        private readonly IShipDebugActionConditions _shipDebugActionConditions;
        private readonly IReadOnlyComponentRegistry<ShipHullDamagePoint> _shipHullDamagePointRegistry;
        private readonly List<string> _choices = new List<string>();

        private IDisposable _registration;

        public ShipDamageDebugSystem(
            IShipRunBindingModel shipRunBindingModel,
            IShipConditionService shipConditionService,
            IShipModuleCondition shipModuleCondition,
            IShipBreachCondition shipBreachCondition,
            IDebugActionRegistry debugActionRegistry,
            IShipDebugActionConditions shipDebugActionConditions,
            IReadOnlyComponentRegistry<ShipHullDamagePoint> shipHullDamagePointRegistry) {
            _shipRunBindingModel = shipRunBindingModel;
            _shipConditionService = shipConditionService;
            _shipModuleCondition = shipModuleCondition;
            _shipBreachCondition = shipBreachCondition;
            _debugActionRegistry = debugActionRegistry;
            _shipDebugActionConditions = shipDebugActionConditions;
            _shipHullDamagePointRegistry = shipHullDamagePointRegistry;
        }

        public void Initialize() =>
            _registration = _debugActionRegistry.Register(new DebugActionDefinition(DebugSections.SHIP, BREAK_LABEL, BREAK_DESCRIPTION, _shipDebugActionConditions.ReadHostWithShipReason, DebugBreakRandom));

        public void Dispose() =>
            _registration.Dispose();

        public bool DebugBreakModule(string socketId) =>
            _shipConditionService.ServerBreakModule(socketId);

        public bool DebugOpenBreach(string pointId) =>
            _shipConditionService.ServerOpenBreach(pointId);

        public void DebugBreakRandom() {
            ShipBase ship = _shipRunBindingModel.Ship;
            if (ship == null)
                return;

            if (TryPickWorkingModule(ship, out string socketId))
                DebugBreakModule(socketId);

            if (TryPickFreePoint(out string pointId))
                DebugOpenBreach(pointId);
        }

        private bool TryPickWorkingModule(ShipBase ship, out string socketId) {
            _choices.Clear();
            foreach (ShipSocket socket in ship.Sockets) {
                if (socket.TryGetInstalledModule(out ModuleDefinition module) && module.CanBreak && _shipModuleCondition.IsWorking(socket.SocketId))
                    _choices.Add(socket.SocketId);
            }

            return TryPickChoice(out socketId);
        }

        private bool TryPickFreePoint(out string pointId) {
            _choices.Clear();
            foreach (ShipHullDamagePoint point in _shipHullDamagePointRegistry.Items) {
                if (_shipBreachCondition.IsOpen(point.PointId) == false)
                    _choices.Add(point.PointId);
            }

            return TryPickChoice(out pointId);
        }

        private bool TryPickChoice(out string choice) {
            choice = null;
            if (_choices.Count == 0)
                return false;

            choice = _choices[Random.Range(0, _choices.Count)];
            return true;
        }
    }
}
#endif
