using UnityEngine;

namespace Features.ShipDamageModule.Scripts.Damage {
    // The look of one damaged place on every peer: the damaged meshes and the smoke / sparks while damaged; a repair
    // plays a short flash and the repair sound hook (its AudioSource plays a clip when one is set).
    public sealed class ShipDamageVisual : MonoBehaviour {
        [SerializeField] private GameObject[] _damagedLooks;
        [SerializeField] private ParticleSystem[] _damagedEffects;
        [SerializeField] private ParticleSystem _repairFlash;
        [SerializeField] private AudioSource _repairSound;

        private bool _isDamaged;

        private void Awake() =>
            Apply();

        // Only a change from damaged to whole plays the repair: a player who joins later sees no flash.
        public void Show(bool isDamaged) {
            if (_isDamaged == isDamaged)
                return;

            bool isRepaired = _isDamaged;
            _isDamaged = isDamaged;
            Apply();
            if (isRepaired)
                PlayRepair();
        }

        private void Apply() {
            foreach (GameObject look in _damagedLooks)
                look.SetActive(_isDamaged);

            foreach (ParticleSystem effect in _damagedEffects) {
                if (_isDamaged)
                    effect.Play(true);
                else
                    effect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void PlayRepair() {
            _repairFlash.Play(true);
            if (_repairSound.clip != null)
                _repairSound.Play();
        }
    }
}
