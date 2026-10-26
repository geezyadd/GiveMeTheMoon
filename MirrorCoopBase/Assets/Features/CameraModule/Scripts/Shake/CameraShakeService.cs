using System.Collections.Generic;
using Features.CameraModule.Contracts;

namespace Features.CameraModule.Scripts.Shake {
    public sealed class CameraShakeService : ICameraShakeService {
        private readonly CameraShakeModel _cameraShakeModel;

        public CameraShakeService(CameraShakeModel cameraShakeModel) =>
            _cameraShakeModel = cameraShakeModel;

        public void AddShake(CameraShakeProfile profile, float strength) {
            IReadOnlyList<CameraShakeOneShot> oneShots = _cameraShakeModel.OneShots;
            for (int i = 0; i < oneShots.Count; i++) {
                CameraShakeOneShot shake = oneShots[i];
                if (shake.Profile != profile)
                    continue;

                if (strength >= shake.CurrentStrength)
                    shake.Restart(strength);

                return;
            }

            _cameraShakeModel.AddOneShot(new CameraShakeOneShot(profile, strength));
        }

        public ICameraShakeHandle BeginShake(CameraShakeProfile profile) {
            CameraShakeContinuous shake = new(profile);
            _cameraShakeModel.AddContinuous(shake);
            return shake;
        }
    }
}
