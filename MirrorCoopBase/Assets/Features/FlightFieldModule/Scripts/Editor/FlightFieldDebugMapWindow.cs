using System.Diagnostics;
using System.Linq;
using Features.FlightFieldModule.Scripts.Model;
using UnityEditor;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor {
    // Editor map of a generated field: pick a seed and a loop, generate with the game's configuration and look at
    // the cells, structures, station zones, takeoff corridor and routes from above. It does not need Play mode.
    public sealed class FlightFieldDebugMapWindow : EditorWindow {
        private const string MENU_PATH = "Tools/Flight Field/Debug Map";
        private const string TITLE = "Flight Field Map";
        private const int MIN_PIXELS_PER_CELL = 2;
        private const int MAX_PIXELS_PER_CELL = 24;
        private const int MAX_LOOP = 30;
        private const float LEGEND_SWATCH_SIZE = 12f;

        [SerializeField] private int _seed = 1;
        [SerializeField] private int _loopIndex;
        [SerializeField] private int _pixelsPerCell = 12;
        [SerializeField] private FieldDebugScenario _scenario = new();

        private FlightField _field;
        private Texture2D _map;
        private double _generationMilliseconds;
        private Vector2 _scroll;

        [MenuItem(MENU_PATH)]
        public static void Open() =>
            GetWindow<FlightFieldDebugMapWindow>(TITLE);

        public void ShowSeed(int seed, int loopIndex) {
            _seed = seed;
            _loopIndex = loopIndex;
            Generate();
        }

        private void OnDisable() =>
            DestroyMap();

        private void OnGUI() {
            DrawControls();
            if (_field == null)
                return;

            DrawSummary();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            Rect mapRect = GUILayoutUtility.GetRect(_map.width, _map.height, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
            GUI.DrawTexture(mapRect, _map);
            EditorGUILayout.EndScrollView();
        }

        private void DrawControls() {
            EditorGUILayout.BeginHorizontal();
            _seed = EditorGUILayout.IntField("Seed", _seed);
            if (GUILayout.Button("<"))
                ShowSeed(_seed - 1, _loopIndex);

            if (GUILayout.Button(">"))
                ShowSeed(_seed + 1, _loopIndex);

            EditorGUILayout.EndHorizontal();
            _loopIndex = EditorGUILayout.IntSlider("Loop", _loopIndex, 0, MAX_LOOP);
            _pixelsPerCell = EditorGUILayout.IntSlider("Pixels per cell", _pixelsPerCell, MIN_PIXELS_PER_CELL, MAX_PIXELS_PER_CELL);
            SerializedObject serializedWindow = new(this);
            EditorGUILayout.PropertyField(serializedWindow.FindProperty(nameof(_scenario)), true);
            serializedWindow.ApplyModifiedProperties();
            if (GUILayout.Button("Generate"))
                Generate();
        }

        private void DrawSummary() {
            EditorGUILayout.LabelField($"Field {_field.Frame.SideCells} x {_field.Frame.SideCells} cells of {_field.Frame.CellSize} m, seed used {_field.Seed}, generated in {_generationMilliseconds:0.0} ms"
                + (_field.IsPassageForced ? ", PASSAGE FORCED" : string.Empty));
            string structures = string.Join(", ", _field.Placements.GroupBy(placement => placement.StructureId).OrderBy(group => group.Key)
                .Select(group => $"{group.Key}: {group.Count()}"));
            EditorGUILayout.LabelField($"Placements {_field.Placements.Count} ({structures})");
            foreach (FieldRoute route in _field.Routes)
                EditorGUILayout.LabelField($"Route {route.Id}: difficulty {route.Difficulty:0.00}, corridor {route.CorridorWidthCells} cells, centerline {route.Centerline.Count} cells");

            DrawLegend();
        }

        private static void DrawLegend() {
            EditorGUILayout.BeginHorizontal();
            DrawLegendEntry(FlightFieldMapLegend.StationZone, "Station zone");
            DrawLegendEntry(FlightFieldMapLegend.TakeoffCorridor, "Takeoff corridor");
            DrawLegendEntry(FlightFieldMapLegend.RouteCorridor, "Route corridor");
            DrawLegendEntry(FlightFieldMapLegend.Centerline, "Route centerline");
            DrawLegendEntry(FlightFieldMapLegend.StationA, "A");
            DrawLegendEntry(FlightFieldMapLegend.StationB, "B");
            DrawLegendEntry(FlightFieldMapLegend.CruiseStart, "Cruise start");
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawLegendEntry(Color color, string label) {
            Rect swatch = GUILayoutUtility.GetRect(LEGEND_SWATCH_SIZE, LEGEND_SWATCH_SIZE, GUILayout.ExpandWidth(false));
            EditorGUI.DrawRect(swatch, color);
            GUILayout.Label(label, GUILayout.ExpandWidth(false));
        }

        private void Generate() {
            FlightFieldEditorComposition composition = new FlightFieldEditorCompositionFactory().Create();
            FieldRequest request = _scenario.CreateRequest(composition.RequestFactory, _seed, _loopIndex);
            Stopwatch stopwatch = Stopwatch.StartNew();
            _field = composition.Generator.Generate(request);
            _generationMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
            DestroyMap();
            _map = new FlightFieldMapPainter().Paint(_field, _pixelsPerCell);
            Repaint();
        }

        private void DestroyMap() {
            if (_map != null)
                DestroyImmediate(_map);

            _map = null;
        }
    }
}
