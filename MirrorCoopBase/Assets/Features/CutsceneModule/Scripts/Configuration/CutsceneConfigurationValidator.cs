using System;
using Zenject;

namespace Features.CutsceneModule.Scripts.Configuration {
    // The one startup check of the cutscene configuration: a camera turn and a return blend that fit a short cutscene.
    public sealed class CutsceneConfigurationValidator : IInitializable {
        private readonly CutsceneConfiguration _cutsceneConfiguration;

        public CutsceneConfigurationValidator(CutsceneConfiguration cutsceneConfiguration) =>
            _cutsceneConfiguration = cutsceneConfiguration;

        public void Initialize() {
            if (_cutsceneConfiguration.TurnSeconds <= 0f)
                throw new InvalidOperationException($"{_cutsceneConfiguration.name}: the camera turn needs a positive time, got {_cutsceneConfiguration.TurnSeconds} s.");

            if (_cutsceneConfiguration.DebugShotSeconds <= 0f)
                throw new InvalidOperationException($"{_cutsceneConfiguration.name}: the test cutscene's shots need a positive time, got {_cutsceneConfiguration.DebugShotSeconds} s.");
        }
    }
}
