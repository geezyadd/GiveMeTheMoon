using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace Features.AudioModule {
    // IAudioService over FMOD's RuntimeManager. An empty event reference plays nothing.
    public sealed class FmodAudioService : IAudioService {
        public void PlayOneShot(EventReference sound, Vector3 position) {
            if (sound.IsNull)
                return;

            RuntimeManager.PlayOneShot(sound, position);
        }

        public void PlayOneShot2D(EventReference sound) {
            if (sound.IsNull)
                return;

            EventInstance instance = RuntimeManager.CreateInstance(sound);
            instance.start();
            instance.release();
        }

        public SoundInstance CreateInstance(EventReference sound) =>
            new SoundInstance(sound);
    }
}
