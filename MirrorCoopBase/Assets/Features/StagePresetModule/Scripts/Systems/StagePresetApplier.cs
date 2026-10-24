using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Targets;
using Features.ShipModule.Scripts;
using Features.ShopModule.Scripts.Configurations;
using Features.ShopModule.Scripts.Core;
using Features.StagePresetModule.Scripts.Configurations;
using Features.StagePresetModule.Scripts.Core;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Features.FlightFieldModule.Scripts.Requests;
#endif

namespace Features.StagePresetModule.Scripts.Systems {
    // The server's one place that turns the selected stage preset into the run: start loop, modules through the
    // sockets' normal install path, starting balance, station drops, field profile, the target balloon count scale of
    // its FieldTargetPresetBlock. No preset: a normal run.
    public sealed class StagePresetApplier : IShipRunStage, IWalletStartingBalanceSource {
        private const int NORMAL_START_LOOP_INDEX = 0;
        private const string INSTALL_FAILED_FORMAT = "Stage preset '{0}': module '{1}' cannot be installed in socket '{2}' (it holds '{3}').";

        private readonly IStagePresetSelectionService _stagePresetSelectionService;
        private readonly IStagePresetSocketResolver _stagePresetSocketResolver;
        private readonly WalletConfiguration _walletConfiguration;
        private readonly IFieldTargetCountScaleWriter _fieldTargetCountScaleWriter;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private readonly IFieldProfileDebug _fieldProfileDebug;
#endif

        public int StartLoopIndex =>
            _stagePresetSelectionService.TryGetSelectedPreset(out StagePreset preset) ? preset.StartLoopIndex : NORMAL_START_LOOP_INDEX;

        public bool AreStationDropsEnabled =>
            _stagePresetSelectionService.TryGetSelectedPreset(out StagePreset preset) == false || preset.AreStationDropsEnabled;

        public long StartingBalance =>
            _stagePresetSelectionService.TryGetSelectedPreset(out StagePreset preset) ? preset.StartingBalance : _walletConfiguration.StartingBalance;

        public StagePresetApplier(
            IStagePresetSelectionService stagePresetSelectionService,
            IStagePresetSocketResolver stagePresetSocketResolver,
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            IFieldProfileDebug fieldProfileDebug,
#endif
            WalletConfiguration walletConfiguration,
            IFieldTargetCountScaleWriter fieldTargetCountScaleWriter) {
            _stagePresetSelectionService = stagePresetSelectionService;
            _stagePresetSocketResolver = stagePresetSocketResolver;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _fieldProfileDebug = fieldProfileDebug;
#endif
            _walletConfiguration = walletConfiguration;
            _fieldTargetCountScaleWriter = fieldTargetCountScaleWriter;
        }

        public void ServerPrepareShip(ShipBase ship) {
            if (_stagePresetSelectionService.TryGetSelectedPreset(out StagePreset preset) == false)
                return;

            IReadOnlyList<ShipSocket> sockets = _stagePresetSocketResolver.ResolveSockets(preset, ship.Sockets);
            for (int i = 0; i < sockets.Count; i++)
                InstallModule(preset, preset.Modules[i].Module, sockets[i]);

            if (_stagePresetSelectionService.TryGetSelectedExtension(out FieldTargetPresetBlock targets))
                _fieldTargetCountScaleWriter.SetCountScale(targets.CountScale);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (preset.HasFieldProfile)
                _fieldProfileDebug.DebugUseProfile(preset.FieldProfileId);
#endif
        }

        public void OnRunEnded() {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_stagePresetSelectionService.TryGetSelectedPreset(out StagePreset preset) && preset.HasFieldProfile)
                _fieldProfileDebug.DebugClearProfile();
#endif
            _stagePresetSelectionService.Clear();
            _fieldTargetCountScaleWriter.Reset();
        }

        // A socket that already holds the module (its starting module, e.g. the helm) keeps it.
        private static void InstallModule(StagePreset preset, ModuleDefinition module, ShipSocket socket) {
            if (socket.InstalledModuleId == module.Id)
                return;

            if (socket.ServerTryInstall(module.Id) == false)
                throw new InvalidOperationException(string.Format(INSTALL_FAILED_FORMAT, preset.Id, module.Id, socket.SocketId, socket.InstalledModuleId));
        }
    }
}
