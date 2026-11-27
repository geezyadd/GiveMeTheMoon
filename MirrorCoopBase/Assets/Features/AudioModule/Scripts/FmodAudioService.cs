using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace Features.AudioModule {
    // IAudioService over FMOD's RuntimeManager. An empty event reference plays nothing.
    public sealed class FmodAudioService : IAudioService {
        private readonly HashSet<string> _missingBuses = new();

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

        public void SetBusVolume(string busPath, float volume) {
            Bus bus;
            try {
                bus = RuntimeManager.GetBus(busPath);
            }
            catch (BusNotFoundException) {
                if (_missingBuses.Add(busPath))
                    Debug.LogWarning($"Audio: no FMOD bus '{busPath}' in the loaded banks, its volume is not set.");
                return;
            }

            bus.setVolume(Mathf.Clamp01(volume));
        }
    }
}
