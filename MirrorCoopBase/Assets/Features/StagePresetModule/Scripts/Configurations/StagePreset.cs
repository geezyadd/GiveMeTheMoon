using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.StagePresetModule.Scripts.Configurations {
    // One stage of the game a playtest run can start in: loop, field, installed modules, money, drops.
    [CreateAssetMenu(
        fileName = nameof(StagePreset),
        menuName = "Configurations/StagePresetModule/" + nameof(StagePreset))]
    public sealed class StagePreset : ScriptableObject {
        [Tooltip("Stable id synced to the clients: never rename it once it is used.")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [Tooltip("The run starts on this loop: flight length, station distance and field difficulty follow it.")]
        [SerializeField] private int _startLoopIndex;
        [Tooltip("Empty: the loop picks the field profile as in a normal run. Otherwise every field of the run uses this "
                 + "difficulty profile id (editor / development builds).")]
        [SerializeField] private string _fieldProfileId;
        [Tooltip("Installed at the run start on top of the ship's starting modules.")]
        [SerializeField] private StagePresetSocketModule[] _modules = Array.Empty<StagePresetSocketModule>();
        [Tooltip("The crew's balance at the run start.")]
        [SerializeField] private long _startingBalance;
        [Tooltip("Off: stations of the run drop no free modules.")]
        [SerializeField] private bool _areStationDropsEnabled;
        [Tooltip("Optional settings blocks: a system reads its own block by type, the preset does not know them.")]
        [SerializeField] private ScriptableObject[] _extensions = Array.Empty<ScriptableObject>();

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public int StartLoopIndex => _startLoopIndex;
        public string FieldProfileId => _fieldProfileId;
        public bool HasFieldProfile => string.IsNullOrEmpty(_fieldProfileId) == false;
        public IReadOnlyList<StagePresetSocketModule> Modules => _modules;
        public long StartingBalance => _startingBalance;
        public bool AreStationDropsEnabled => _areStationDropsEnabled;
        public IReadOnlyList<ScriptableObject> Extensions => _extensions;
    }
}
