using Features.WeaponModule.Scripts.Definitions;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Feel {
    // The sound hooks of a weapon: a sound per shot, a fire loop with a tail when it stops, casing clinks and impacts.
    // A clip the profile leaves empty keeps its hook silent.
    public sealed class WeaponFeelAudio {
        private readonly AudioSource _loopSource;
        private readonly AudioSource _oneShotSource;
        private readonly WeaponFeelProfile _profile;

        private bool _isFiring;
        private int _impacts;

        public WeaponFeelAudio(AudioSource loopSource, AudioSource oneShotSource, WeaponFeelProfile profile) {
            _loopSource = loopSource;
            _oneShotSource = oneShotSource;
            _profile = profile;
            _loopSource.clip = profile.FireLoopClip;
            _loopSource.loop = true;
        }

        public void PresentFiring(bool isFiring) {
            if (isFiring == _isFiring)
                return;

            _isFiring = isFiring;
            if (isFiring) {
                if (_profile.FireLoopClip != null)
                    _loopSource.Play();

                return;
            }

            _loopSource.Stop();
            PlayOneShot(_profile.FireTailClip);
        }

        public void PresentShot() =>
            PlayOneShot(_profile.ShotClip);

        // At most one clink a frame: a pile of landings in one frame is one sound.
        public void PresentCasingsLanded(int landedCount) {
            if (landedCount > 0 && Random.value < _profile.CasingClinkShare)
                PlayOneShot(_profile.CasingClip);
        }

        public void PresentImpact() {
            _impacts++;
            if (_impacts % _profile.ImpactSoundEveryNth == 0)
                PlayOneShot(_profile.ImpactClip);
        }

        private void PlayOneShot(AudioClip clip) {
            if (clip != null)
                _oneShotSource.PlayOneShot(clip);
        }
    }
}
