using System;
using FMOD.Studio;
using FMODUnity;
using STOP_MODE = FMOD.Studio.STOP_MODE;

namespace Features.AudioModule {
    // A long sound (music, ambience, a loop) its owner starts and stops. It holds one FMOD event instance and must be
    // disposed when no longer needed: Dispose stops it and releases the instance. Made for an empty event reference
    // it does nothing.
    public sealed class SoundInstance : IDisposable {
        private EventInstance _instance;
        private bool _isDisposed;

        internal SoundInstance(EventReference sound) {
            if (sound.IsNull == false)
                _instance = RuntimeManager.CreateInstance(sound);
        }

        public bool IsPlaying {
            get {
                if (_instance.isValid() == false)
                    return false;

                _instance.getPlaybackState(out PLAYBACK_STATE state);
                return state != PLAYBACK_STATE.STOPPED;
            }
        }

        // Starts it from the beginning, also after a stop.
        public void Start() {
            if (_isDisposed == false && _instance.isValid())
                _instance.start();
        }

        // fadeOut: lets the event's own fade (AHDSR release) play out; false cuts it at once.
        public void Stop(bool fadeOut = true) {
            if (_instance.isValid())
                _instance.stop(fadeOut ? STOP_MODE.ALLOWFADEOUT : STOP_MODE.IMMEDIATE);
        }

        // Stops it with its fade and releases the instance; FMOD frees it once the fade is over.
        public void Dispose() {
            if (_isDisposed)
                return;

            _isDisposed = true;
            if (_instance.isValid() == false)
                return;

            _instance.stop(STOP_MODE.ALLOWFADEOUT);
            _instance.release();
            _instance.clearHandle();
        }
    }
}
