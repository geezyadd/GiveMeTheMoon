using Features.FlightFieldModule.Scripts.Configuration;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Targets {
    public interface IFieldTargetPopEffectPool {
        public void Play(Transform parent, Vector3 position, FieldTargetDefinition kind);
    }
}
