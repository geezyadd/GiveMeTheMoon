using UnityEngine;
using UnityEngine.UI;

namespace Features.EmoteModule.UI {
    // A piece of a ring around the rect's centre, drawn as a mesh: from Start Degrees (clockwise from the top) over
    // Sweep Degrees, between the inner and the outer radius, with a small gap on each side so sectors stand apart.
    public sealed class EmoteSectorGraphic : MaskableGraphic {
        [SerializeField, Min(0f)] private float _innerRadius = 70f;
        [SerializeField, Min(0f)] private float _outerRadius = 220f;
        [SerializeField, Min(0f)] private float _gapDegrees = 1.5f;
        [SerializeField, Min(1)] private int _segmentsPerTurn = 96;
        [SerializeField] private float _startDegrees;
        [SerializeField] private float _sweepDegrees = 90f;

        public void SetArc(float startDegrees, float sweepDegrees) {
            _startDegrees = startDegrees;
            _sweepDegrees = sweepDegrees;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertices) {
            vertices.Clear();
            float start = _startDegrees + _gapDegrees;
            float sweep = _sweepDegrees - _gapDegrees * 2f;
            if (sweep <= 0f || _outerRadius <= _innerRadius)
                return;

            int segments = Mathf.Max(1, Mathf.CeilToInt(_segmentsPerTurn * sweep / 360f));
            Vector2 centre = rectTransform.rect.center;
            for (int i = 0; i <= segments; i++) {
                float radians = (start + sweep * i / segments) * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
                vertices.AddVert(centre + direction * _innerRadius, color, Vector2.zero);
                vertices.AddVert(centre + direction * _outerRadius, color, Vector2.zero);
                if (i == 0)
                    continue;

                int inner = i * 2;
                vertices.AddTriangle(inner - 2, inner - 1, inner + 1);
                vertices.AddTriangle(inner - 2, inner + 1, inner);
            }
        }
    }
}
