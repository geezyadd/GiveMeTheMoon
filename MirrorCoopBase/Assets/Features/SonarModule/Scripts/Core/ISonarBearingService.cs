using Features.SonarModule.Data;
using UnityEngine;

namespace Features.SonarModule.Scripts.Core {
    public interface ISonarBearingService {
        public SonarScopeBearings GetBearings(Vector3 shipPosition, float shipYawDegrees, float rangeMeters);
    }
}
