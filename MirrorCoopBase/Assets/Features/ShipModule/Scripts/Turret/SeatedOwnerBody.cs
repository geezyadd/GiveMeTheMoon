using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Features.ShipModule.Scripts.Turret {
    // The seated owner's own body, seen from the gun's camera point: at a steep pitch the camera drops below the body
    // and looks into it. Only the owner's peer hides it; the body keeps its shadow and the others still see it.
    public sealed class SeatedOwnerBody {
        private readonly List<Renderer> _renderers = new List<Renderer>();
        private readonly List<ShadowCastingMode> _shadowModes = new List<ShadowCastingMode>();

        public void Hide(Component occupant) {
            Show();
            occupant.GetComponentsInChildren(true, _renderers);
            for (int i = 0; i < _renderers.Count; i++) {
                _shadowModes.Add(_renderers[i].shadowCastingMode);
                _renderers[i].shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
        }

        // The player object may already be gone.
        public void Show() {
            for (int i = 0; i < _renderers.Count; i++) {
                if (_renderers[i] != null)
                    _renderers[i].shadowCastingMode = _shadowModes[i];
            }

            _renderers.Clear();
            _shadowModes.Clear();
        }
    }
}
