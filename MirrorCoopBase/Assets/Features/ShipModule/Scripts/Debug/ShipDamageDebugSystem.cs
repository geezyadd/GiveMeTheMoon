#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using Features.ShipModule.Scripts.Damage;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Features.ShipModule.Scripts.Debug {
    // Debug only, on the host: F5 breaks a random working module and opens a random free breach, through the same
    // condition service a rock impact uses, so the stats, the synced model and the events all see it.
    public sealed class ShipDamageDebugSystem : ITickable, IShipDamageDebug {
        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly IShipConditionService _shipConditionService;
        private readonly IShipModuleCondition _shipModuleCondition;
        private readonly IShipBreachCondition _shipBreachCondition;
        private readonly List<string> _choices = new List<string>();

        public ShipDamageDebugSystem(
            IShipRunBindingModel shipRunBindingModel,
            IShipConditionService shipConditionService,
            IShipModuleCondition shipModuleCondition,
            IShipBreachCondition shipBreachCondition) {
            _shipRunBindingModel = shipRunBindingModel;
            _shipConditionService = shipConditionService;
            _shipModuleCondition = shipModuleCondition;
            _shipBreachCondition = shipBreachCondition;
        }

        public void Tick() {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && NetworkServer.active && keyboard.f5Key.wasPressedThisFrame)
                DebugBreakRandom();
        }

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

            if (TryPickFreePoint(ship, out string pointId))
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

        private bool TryPickFreePoint(ShipBase ship, out string pointId) {
            _choices.Clear();
            foreach (ShipHullDamagePoint point in ship.DamagePoints) {
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
