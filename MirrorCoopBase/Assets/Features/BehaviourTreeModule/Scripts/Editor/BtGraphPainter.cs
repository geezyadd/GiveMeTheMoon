using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Features.BehaviourTreeModule.Editor {
    // Draws the laid-out tree: a link from every parent to its children, then a box per node coloured by its status
    // and dimmed when the node was not ticked on the tree's last tick.
    public sealed class BtGraphPainter {
        private const float DIMMED_ALPHA = 0.35f;
        private const float MIN_LABEL_ZOOM = 0.5f;
        private const int BASE_FONT_SIZE = 11;
        private const string LINE_BREAK = "\n";
        private const string UNEXPECTED_STATUS = "The inspector has no colour for this status.";

        private static readonly Color _inactiveColour = new(0.35f, 0.35f, 0.35f);
        private static readonly Color _runningColour = new(0.95f, 0.75f, 0.2f);
        private static readonly Color _successColour = new(0.3f, 0.75f, 0.35f);
        private static readonly Color _failureColour = new(0.85f, 0.3f, 0.3f);
        private static readonly Color _linkColour = new(0.7f, 0.7f, 0.7f, 0.6f);

        private GUIStyle _labelStyle;

        public void Paint(BtGraphLayout layout, BtGraphCamera camera, int currentTick) {
            PaintLinks(layout, camera);
            foreach (KeyValuePair<BtNode, Rect> box in layout.Boxes)
                PaintBox(box.Key, camera.ToView(box.Value), camera.Zoom, currentTick);
        }

        private static void PaintLinks(BtGraphLayout layout, BtGraphCamera camera) {
            Handles.color = _linkColour;
            foreach (KeyValuePair<BtNode, Rect> box in layout.Boxes) {
                BtNode node = box.Key;
                Vector2 from = camera.ToView(new Vector2(box.Value.center.x, box.Value.yMax));
                for (int i = 0; i < node.Children.Count; i++) {
                    Rect childBox = layout.Boxes[node.Children[i]];
                    Handles.DrawLine(from, camera.ToView(new Vector2(childBox.center.x, childBox.yMin)));
                }
            }
        }

        private void PaintBox(BtNode node, Rect viewRect, float zoom, int currentTick) {
            Color colour = GetStatusColour(node.Status);
            if (node.LastRunTick != currentTick)
                colour.a = DIMMED_ALPHA;

            EditorGUI.DrawRect(viewRect, colour);
            if (zoom < MIN_LABEL_ZOOM)
                return;

            GUIStyle style = GetLabelStyle();
            style.fontSize = Mathf.RoundToInt(BASE_FONT_SIZE * zoom);
            GUI.Label(viewRect, GetLabel(node), style);
        }

        private static string GetLabel(BtNode node) =>
            string.IsNullOrEmpty(node.DebugText) ? node.Name : node.Name + LINE_BREAK + node.DebugText;

        private GUIStyle GetLabelStyle() {
            if (_labelStyle != null)
                return _labelStyle;

            _labelStyle = new GUIStyle(EditorStyles.label) {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                clipping = TextClipping.Clip
            };
            _labelStyle.normal.textColor = Color.black;
            return _labelStyle;
        }

        private static Color GetStatusColour(BtStatus status) =>
            status switch {
                BtStatus.Inactive => _inactiveColour,
                BtStatus.Running => _runningColour,
                BtStatus.Success => _successColour,
                BtStatus.Failure => _failureColour,
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, UNEXPECTED_STATUS)
            };
    }
}
