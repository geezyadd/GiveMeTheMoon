using FMODUnity;
using UnityEngine;

namespace Features.AudioModule {
    // Plays FMOD events on this peer. Local, never over the network: each peer plays a sound where it sees its cause.
    public interface IAudioService {
        // A fire-and-forget event at a world point (an explosion, an impact).
        public void PlayOneShot(EventReference sound, Vector3 position);

        // A fire-and-forget event with no position (UI, stingers). Its event should have no spatializer in FMOD
        // Studio: a 3D event played this way sounds from the world origin.
        public void PlayOneShot2D(EventReference sound);

        // A long sound its caller starts and stops itself; the caller disposes it to release the FMOD instance.
        public SoundInstance CreateInstance(EventReference sound);

        // The volume of an FMOD Studio bus (a group of the mixer, e.g. "bus:/All/Music"): 0 is silent, 1 is as
        // mixed. A bus the loaded banks do not have is reported once and skipped.
        public void SetBusVolume(string busPath, float volume);
    }
}
