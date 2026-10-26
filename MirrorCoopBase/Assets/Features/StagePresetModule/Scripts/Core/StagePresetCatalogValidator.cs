using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Requests;
using Features.ShipModule.Scripts;
using Features.StagePresetModule.Scripts.Configurations;
using UnityEngine;
using Zenject;

namespace Features.StagePresetModule.Scripts.Core {
    // The one startup check of the stage preset catalog: every preset set, ids present and unique, modules from the
    // ModuleCatalog, one module per socket, a known field profile, extension blocks set and one per type. The ship's
    // sockets are checked at the run start (StagePresetSocketResolver), when the ship exists.
    public sealed class StagePresetCatalogValidator : IInitializable {
        private readonly StagePresetCatalog _stagePresetCatalog;
        private readonly ModuleCatalog _moduleCatalog;
        private readonly IFieldProfileLookup _fieldProfileLookup;

        public StagePresetCatalogValidator(StagePresetCatalog stagePresetCatalog, ModuleCatalog moduleCatalog, IFieldProfileLookup fieldProfileLookup) {
            _stagePresetCatalog = stagePresetCatalog;
            _moduleCatalog = moduleCatalog;
            _fieldProfileLookup = fieldProfileLookup;
        }

        public void Initialize() {
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _stagePresetCatalog.Presets.Count; i++)
                ValidatePreset(_stagePresetCatalog.Presets[i], i, ids);
        }

        private void ValidatePreset(StagePreset preset, int index, HashSet<string> ids) {
            if (preset == null)
                throw new InvalidOperationException($"{_stagePresetCatalog.name}: preset entry {index} is empty.");

            if (string.IsNullOrEmpty(preset.Id))
                throw new InvalidOperationException($"{_stagePresetCatalog.name}: preset {preset.name} has no id.");

            if (ids.Add(preset.Id) == false)
                throw new InvalidOperationException($"{_stagePresetCatalog.name}: preset id '{preset.Id}' is used twice (second: {preset.name}).");

            if (string.IsNullOrEmpty(preset.DisplayName))
                throw new InvalidOperationException($"{preset.name}: the preset has no display name.");

            if (preset.StartLoopIndex < 0)
                throw new InvalidOperationException($"{preset.name}: start loop index {preset.StartLoopIndex} is negative.");

            if (preset.StartingBalance < 0)
                throw new InvalidOperationException($"{preset.name}: starting balance {preset.StartingBalance} is negative.");

            if (preset.HasFieldProfile && _fieldProfileLookup.TryFind(preset.FieldProfileId, out _) == false)
                throw new InvalidOperationException($"{preset.name}: no field difficulty profile '{preset.FieldProfileId}'.");

            ValidateModules(preset);
            ValidateExtensions(preset);
        }

        private void ValidateModules(StagePreset preset) {
            HashSet<string> sockets = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < preset.Modules.Count; i++) {
                StagePresetSocketModule entry = preset.Modules[i];
                if (string.IsNullOrEmpty(entry.SocketId))
                    throw new InvalidOperationException($"{preset.name}: module entry {i} has no socket id.");

                if (sockets.Add(entry.SocketId) == false)
                    throw new InvalidOperationException($"{preset.name}: socket '{entry.SocketId}' gets two modules.");

                if (entry.Module == null)
                    throw new InvalidOperationException($"{preset.name}: socket '{entry.SocketId}' has no module.");

                if (_moduleCatalog.TryGet(entry.Module.Id, out ModuleDefinition module) == false || module != entry.Module)
                    throw new InvalidOperationException($"{preset.name}: module {entry.Module.name} is not in the {_moduleCatalog.name}.");
            }
        }

        private static void ValidateExtensions(StagePreset preset) {
            HashSet<Type> types = new HashSet<Type>();
            for (int i = 0; i < preset.Extensions.Count; i++) {
                ScriptableObject extension = preset.Extensions[i];
                if (extension == null)
                    throw new InvalidOperationException($"{preset.name}: extension entry {i} is empty.");

                if (types.Add(extension.GetType()) == false)
                    throw new InvalidOperationException($"{preset.name}: two extension blocks of type {extension.GetType().Name}.");
            }
        }
    }
}
