using System;
using Features.SonarModule.Contracts;
using UnityEngine;

namespace Features.SonarModule.Scripts.Configurations {
    // The sonar kind of the balloons that give at least this reward.
    [Serializable]
    public sealed class SonarTargetTier {
        [SerializeField] private int _minReward;
        [SerializeField] private SonarContactKind _kind;

        public int MinReward => _minReward;
        public SonarContactKind Kind => _kind;
    }
}
