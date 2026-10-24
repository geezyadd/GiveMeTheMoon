using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // A stage preset's extension block for the target balloons: scales the count of every flight of the run, so an
    // early stage shows fewer and a late one more.
    [CreateAssetMenu(menuName = "Game/Flight Field/Target Preset Block", fileName = "FieldTargetPresetBlock_")]
    public sealed class FieldTargetPresetBlock : ScriptableObject {
        [Tooltip("The balloon count of every flight is multiplied by this (1 = as the target configuration says).")]
        [SerializeField, Min(0f)] private float _countScale = 1f;

        public float CountScale => _countScale;
    }
}
