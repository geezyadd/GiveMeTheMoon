using UnityEngine;

namespace Features.BehaviourTreeModule.Editor {
    // Pan and zoom of the graph: drag with the left or middle mouse button to pan, scroll to zoom around the cursor.
    public sealed class BtGraphCamera {
        private const float MIN_ZOOM = 0.2f;
        private const float MAX_ZOOM = 2f;
        private const float ZOOM_PER_SCROLL = 0.05f;
        private const float FRAME_MARGIN = 40f;
        private const int LEFT_BUTTON = 0;
        private const int MIDDLE_BUTTON = 2;

        private Vector2 _offset;
        private float _zoom = 1f;

        public float Zoom => _zoom;

        public Vector2 ToView(Vector2 graphPoint) =>
            graphPoint * _zoom + _offset;

        public Rect ToView(Rect graphRect) =>
            new(ToView(graphRect.position), graphRect.size * _zoom);

        public void Frame(Rect graphBounds, Vector2 viewSize) {
            Vector2 available = viewSize - Vector2.one * (FRAME_MARGIN * 2f);
            float fitZoom = Mathf.Min(available.x / Mathf.Max(graphBounds.width, 1f), available.y / Mathf.Max(graphBounds.height, 1f));
            _zoom = Mathf.Clamp(fitZoom, MIN_ZOOM, MAX_ZOOM);
            _offset = viewSize / 2f - graphBounds.center * _zoom;
        }

        public bool HandleInput(Event current) {
            if (current.type == EventType.MouseDrag && IsPanButton(current.button)) {
                _offset += current.delta;
                return true;
            }

            if (current.type != EventType.ScrollWheel)
                return false;

            ZoomAround(current.mousePosition, -current.delta.y * ZOOM_PER_SCROLL);
            return true;
        }

        private void ZoomAround(Vector2 viewPoint, float zoomChange) {
            Vector2 graphPoint = (viewPoint - _offset) / _zoom;
            _zoom = Mathf.Clamp(_zoom * (1f + zoomChange), MIN_ZOOM, MAX_ZOOM);
            _offset = viewPoint - graphPoint * _zoom;
        }

        private static bool IsPanButton(int button) =>
            button == LEFT_BUTTON || button == MIDDLE_BUTTON;
    }
}
