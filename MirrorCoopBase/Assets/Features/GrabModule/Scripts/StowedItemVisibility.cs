using UnityEngine;

namespace Features.GrabModule.Scripts {
    // Hides a stowed item's renderers and colliders and gives each back the state it had, so a renderer or trigger
    // that was off by design stays off when the item comes out again.
    public sealed class StowedItemVisibility {
        private readonly Renderer[] _renderers;
        private readonly Collider[] _colliders;
        private readonly bool[] _renderersEnabled;
        private readonly bool[] _collidersEnabled;
        private bool _isHidden;

        public StowedItemVisibility(Renderer[] renderers, Collider[] colliders) {
            _renderers = renderers;
            _colliders = colliders;
            _renderersEnabled = new bool[renderers.Length];
            _collidersEnabled = new bool[colliders.Length];
        }

        public void Hide() {
            if (_isHidden)
                return;

            _isHidden = true;
            for (int i = 0; i < _renderers.Length; i++) {
                _renderersEnabled[i] = _renderers[i].enabled;
                _renderers[i].enabled = false;
            }

            for (int i = 0; i < _colliders.Length; i++) {
                _collidersEnabled[i] = _colliders[i].enabled;
                _colliders[i].enabled = false;
            }
        }

        public void Show() {
            if (_isHidden == false)
                return;

            _isHidden = false;
            for (int i = 0; i < _renderers.Length; i++)
                _renderers[i].enabled = _renderersEnabled[i];

            for (int i = 0; i < _colliders.Length; i++)
                _colliders[i].enabled = _collidersEnabled[i];
        }
    }
}
