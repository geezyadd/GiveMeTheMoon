using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace Features.BehaviourTreeModule.Editor {
    // An editor tool, not game UI: in Play mode it shows the live tree of the selected object's tree holder and the
    // tree's last status changes. F or the Frame button fits the graph into the view.
    public sealed class BtInspectorWindow : EditorWindow {
        private const string MENU_PATH = "Window/GiveMeTheMoon/BT Inspector";
        private const string TITLE = "BT Inspector";
        private const string FRAME_LABEL = "Frame";
        private const string NOT_PLAYING = "Enter Play mode and select an object with a behaviour tree.";
        private const string NO_TREE = "The selected object has no running behaviour tree on this peer.";
        private const string HEADER_FORMAT = "{0}   tick {1}   {2:0.00} s";
        private const string CHANGE_FORMAT = "{0,8:0.00}  {1}  {2}";
        private const string CHANGES_TITLE = "Last status changes";
        private const int LOG_CAPACITY = 60;
        private const float SIDE_PANEL_WIDTH = 260f;
        private const float TOOLBAR_HEIGHT = 20f;

        private readonly BtGraphLayout _layout = new();
        private readonly BtGraphCamera _camera = new();
        private readonly BtGraphPainter _painter = new();
        private readonly BtStatusChangeLog _changeLog = new(LOG_CAPACITY);

        private BtTree _tree;
        private Vector2 _logScroll;
        private bool _isFramePending;

        [MenuItem(MENU_PATH)]
        private static void Open() =>
            GetWindow<BtInspectorWindow>(TITLE);

        private void OnEnable() =>
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;

        private void OnDisable() {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            ForgetTree();
        }

        private void Update() {
            if (!EditorApplication.isPlaying)
                return;

            RefreshTree();
            Repaint();
        }

        private void OnGUI() {
            if (!EditorApplication.isPlaying || _tree == null) {
                EditorGUILayout.HelpBox(EditorApplication.isPlaying ? NO_TREE : NOT_PLAYING, MessageType.Info);
                return;
            }

            Rect graphRect = new(0f, TOOLBAR_HEIGHT, position.width - SIDE_PANEL_WIDTH, position.height - TOOLBAR_HEIGHT);
            DrawToolbar();
            DrawGraph(graphRect);
            DrawChangeLog(new Rect(graphRect.xMax, TOOLBAR_HEIGHT, SIDE_PANEL_WIDTH, graphRect.height));
        }

        private void DrawToolbar() {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label(string.Format(CultureInfo.InvariantCulture, HEADER_FORMAT, _tree.GetType().Name, _tree.Clock.TickCount,
                _tree.Clock.ElapsedSeconds));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(FRAME_LABEL, EditorStyles.toolbarButton))
                _isFramePending = true;

            GUILayout.EndHorizontal();
        }

        private void DrawGraph(Rect graphRect) {
            Event current = Event.current;
            if (_isFramePending || IsFrameKey(current)) {
                _camera.Frame(_layout.Bounds, graphRect.size);
                _isFramePending = false;
            }

            GUI.BeginGroup(graphRect);
            bool isInsideGraph = new Rect(Vector2.zero, graphRect.size).Contains(current.mousePosition);
            if (isInsideGraph && _camera.HandleInput(current))
                current.Use();

            _painter.Paint(_layout, _camera, _tree.Clock.TickCount);
            GUI.EndGroup();
        }

        private void DrawChangeLog(Rect panelRect) {
            GUILayout.BeginArea(panelRect, EditorStyles.helpBox);
            GUILayout.Label(CHANGES_TITLE, EditorStyles.boldLabel);
            _logScroll = GUILayout.BeginScrollView(_logScroll);
            for (int age = 0; age < _changeLog.Count; age++) {
                BtStatusChange change = _changeLog.GetChange(age);
                GUILayout.Label(string.Format(CultureInfo.InvariantCulture, CHANGE_FORMAT, change.TreeSeconds, change.Status, change.NodeName),
                    EditorStyles.miniLabel);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void RefreshTree() {
            BtTree selectedTree = FindSelectedTree();
            if (selectedTree == _tree)
                return;

            ForgetTree();
            if (selectedTree == null)
                return;

            _tree = selectedTree;
            _layout.Arrange(_tree.Root);
            _changeLog.Watch(_tree);
            _isFramePending = true;
        }

        private void ForgetTree() {
            _changeLog.Unwatch();
            _layout.Clear();
            _tree = null;
        }

        private static BtTree FindSelectedTree() {
            GameObject selected = Selection.activeGameObject;
            if (selected == null || !selected.TryGetComponent(out IBtTreeHolder holder))
                return null;

            return holder.TryGetTree(out BtTree tree) ? tree : null;
        }

        private static bool IsFrameKey(Event current) =>
            current.type == EventType.KeyDown && current.keyCode == KeyCode.F;

        private void HandlePlayModeStateChanged(PlayModeStateChange change) {
            if (change == PlayModeStateChange.ExitingPlayMode)
                ForgetTree();

            Repaint();
        }
    }
}
