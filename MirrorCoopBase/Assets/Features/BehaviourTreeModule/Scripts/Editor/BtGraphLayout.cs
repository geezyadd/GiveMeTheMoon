using System.Collections.Generic;
using UnityEngine;

namespace Features.BehaviourTreeModule.Editor {
    // Places the nodes of a tree top-down in graph space: leaves side by side, every parent centred over its children.
    public sealed class BtGraphLayout {
        private const float BOX_WIDTH = 150f;
        private const float BOX_HEIGHT = 44f;
        private const float COLUMN_STEP = 170f;
        private const float ROW_STEP = 80f;

        private readonly Dictionary<BtNode, Rect> _boxes = new();

        private int _nextColumn;

        public Rect Bounds { get; private set; }
        public IReadOnlyDictionary<BtNode, Rect> Boxes => _boxes;

        public void Arrange(BtNode root) {
            _boxes.Clear();
            _nextColumn = 0;
            PlaceBranch(root, 0);
            Bounds = CalculateBounds();
        }

        public void Clear() {
            _boxes.Clear();
            Bounds = Rect.zero;
        }

        private float PlaceBranch(BtNode node, int depth) {
            float centreX = node.Children.Count == 0 ? _nextColumn++ * COLUMN_STEP : PlaceChildren(node, depth);
            _boxes[node] = new Rect(centreX - BOX_WIDTH / 2f, depth * ROW_STEP, BOX_WIDTH, BOX_HEIGHT);
            return centreX;
        }

        private float PlaceChildren(BtNode node, int depth) {
            float firstX = 0f;
            float lastX = 0f;
            for (int i = 0; i < node.Children.Count; i++) {
                float childX = PlaceBranch(node.Children[i], depth + 1);
                if (i == 0)
                    firstX = childX;

                lastX = childX;
            }

            return (firstX + lastX) / 2f;
        }

        private Rect CalculateBounds() {
            Rect bounds = Rect.zero;
            bool isFirst = true;
            foreach (Rect box in _boxes.Values) {
                bounds = isFirst ? box : Rect.MinMaxRect(Mathf.Min(bounds.xMin, box.xMin), Mathf.Min(bounds.yMin, box.yMin),
                    Mathf.Max(bounds.xMax, box.xMax), Mathf.Max(bounds.yMax, box.yMax));
                isFirst = false;
            }

            return bounds;
        }
    }
}
