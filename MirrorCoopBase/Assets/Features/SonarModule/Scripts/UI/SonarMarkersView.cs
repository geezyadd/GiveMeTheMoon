using System.Collections.Generic;
using Features.SonarModule.Data;
using UnityEngine;

namespace Features.SonarModule.Scripts.UI {
    // The world markers over the HUD, from a pool: a screen point becomes a place on this full-screen layer.
    public sealed class SonarMarkersView : SonarMarkersViewBase {
        [SerializeField] private RectTransform _layer;
        [SerializeField] private SonarMarkerItem _markerTemplate;

        private readonly List<SonarMarkerItem> _markers = new();

        private Canvas _canvas;

        public override void ShowMarkers(IReadOnlyList<SonarMarkerDisplay> markers) {
            _markerTemplate.gameObject.SetActive(false);
            while (_markers.Count < markers.Count)
                _markers.Add(Instantiate(_markerTemplate, _layer));

            Camera eventCamera = ReadEventCamera();
            for (int i = 0; i < _markers.Count; i++) {
                if (i >= markers.Count) {
                    _markers[i].Hide();
                    continue;
                }

                RectTransformUtility.ScreenPointToLocalPointInRectangle(_layer, markers[i].ScreenPosition, eventCamera, out Vector2 local);
                _markers[i].Show(markers[i], local);
            }
        }

        // An overlay canvas takes screen points as they are; a camera canvas needs its camera. The canvas is the
        // windows root outside this prefab: it is looked up once.
        private Camera ReadEventCamera() {
            if (_canvas == null)
                _canvas = _layer.GetComponentInParent<Canvas>();

            return _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        }
    }
}
