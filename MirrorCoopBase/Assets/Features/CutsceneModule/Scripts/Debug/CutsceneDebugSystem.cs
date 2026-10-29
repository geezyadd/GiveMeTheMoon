#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Features.CutsceneModule.Contracts;
using Features.CutsceneModule.Scripts.Configuration;
using Features.DebugModule.Contracts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Debug;
using UnityEngine;
using Zenject;

namespace Features.CutsceneModule.Scripts.Debug {
    // Debug only, on the host: "Play a test cutscene" shows every player the ship from one side, then from the other,
    // through the same server path a feature's cutscene takes.
    public sealed class CutsceneDebugSystem : IInitializable, IDisposable {
        private const string PLAY_LABEL = "Play a test cutscene";
        private const string PLAY_DESCRIPTION = "Every player's camera looks at the ship from its right, then from its left, then returns; Space skips it for everyone.";

        private readonly IDebugActionRegistry _debugActionRegistry;
        private readonly IShipDebugActionConditions _shipDebugActionConditions;
        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly ICutsceneDirector _cutsceneDirector;
        private readonly CutsceneConfiguration _cutsceneConfiguration;
        private readonly List<CutsceneShot> _shots = new();

        private IDisposable _registration;

        public CutsceneDebugSystem(
            IDebugActionRegistry debugActionRegistry,
            IShipDebugActionConditions shipDebugActionConditions,
            IShipRunBindingModel shipRunBindingModel,
            ICutsceneDirector cutsceneDirector,
            CutsceneConfiguration cutsceneConfiguration) {
            _debugActionRegistry = debugActionRegistry;
            _shipDebugActionConditions = shipDebugActionConditions;
            _shipRunBindingModel = shipRunBindingModel;
            _cutsceneDirector = cutsceneDirector;
            _cutsceneConfiguration = cutsceneConfiguration;
        }

        public void Initialize() =>
            _registration = _debugActionRegistry.Register(new DebugActionDefinition(DebugSections.VIEW, PLAY_LABEL, PLAY_DESCRIPTION, _shipDebugActionConditions.ReadHostWithShipReason, PlayTestCutscene));

        public void Dispose() =>
            _registration.Dispose();

        private void PlayTestCutscene() {
            Transform ship = _shipRunBindingModel.Ship.transform;
            Vector3 up = Vector3.up * _cutsceneConfiguration.DebugHeightMeters;
            Vector3 side = ship.right * _cutsceneConfiguration.DebugSideMeters;
            _shots.Clear();
            _shots.Add(CutsceneShot.LookAt(ship.position + side + up, ship.position, _cutsceneConfiguration.DebugShotSeconds));
            _shots.Add(CutsceneShot.LookAt(ship.position - side + up, ship.position, _cutsceneConfiguration.DebugShotSeconds));
            _cutsceneDirector.ServerPlay(_shots);
        }
    }
}
#endif
