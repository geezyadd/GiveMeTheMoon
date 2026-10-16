using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Features.FlightFieldModule.Scripts.Model;
using UnityEditor;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor {
    // Editor map of a generated field: pick a seed and a loop, generate with the game's configuration and look at
    // the cells, structures, station zones, takeoff corridor and routes from above. It does not need Play mode.
    // "Live field" shows the field of the running game (host) instead, with the ship, refreshed twice a second.
    public sealed class FlightFieldDebugMapWindow : EditorWindow {
        private const string MENU_PATH = "Tools/Flight Field/Debug Map";
        private const string TITLE = "Flight Field Map";
        private const string PASSAGE_FORCED_NOTE = ", PASSAGE FORCED";
        private const int MIN_PIXELS_PER_CELL = 2;
        private const int MAX_PIXELS_PER_CELL = 24;
        private const int MAX_LOOP = 30;
        private const float LEGEND_SWATCH_SIZE = 12f;
        private const double LIVE_REFRESH_SECONDS = 0.5;
        private const string NO_LIVE_FIELD = "No live field: enter Play mode as host and launch the ship.";

        [SerializeField] private int _seed = 1;
        [SerializeField] private int _loopIndex;
        [SerializeField] private int _pixelsPerCell = 12;
        [SerializeField] private FieldDebugScenario _scenario = new();
        [SerializeField] private bool _isLive;

        private readonly List<string> _summaryLines = new();

        private SerializedObject _serializedWindow;
        private SerializedProperty _scenarioProperty;
        private Texture2D _map;
        private Vector2 _scroll;
        private double _nextLiveRefresh;

        [MenuItem(MENU_PATH)]
        public static void Open() =>
            GetWindow<FlightFieldDebugMapWindow>(TITLE);

        private void OnEnable() {
            _serializedWindow = new SerializedObject(this);
            _scenarioProperty = _serializedWindow.FindProperty(nameof(_scenario));
        }

        private void OnDisable() {
            DestroyMap();
            _serializedWindow.Dispose();
        }

        private void OnInspectorUpdate() {
            bool isLiveRefreshDue = _isLive && EditorApplication.isPlaying && EditorApplication.timeSinceStartup >= _nextLiveRefresh;
            if (!isLiveRefreshDue)
                return;

            _nextLiveRefresh = EditorApplication.timeSinceStartup + LIVE_REFRESH_SECONDS;
            ShowLiveField();
        }

        private void OnGUI() {
            DrawControls();
            if (_map == null)
                return;

            DrawSummary();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            Rect mapRect = GUILayoutUtility.GetRect(_map.width, _map.height, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
            GUI.DrawTexture(mapRect, _map);
            EditorGUILayout.EndScrollView();
        }

        private void ShowSeed(int seed) {
            _seed = seed;
            Generate();
        }

        private void DrawControls() {
            EditorGUILayout.BeginHorizontal();
            _seed = EditorGUILayout.IntField("Seed", _seed);
            if (GUILayout.Button("<"))
                ShowSeed(_seed - 1);

            if (GUILayout.Button(">"))
                ShowSeed(_seed + 1);

            EditorGUILayout.EndHorizontal();
            _loopIndex = EditorGUILayout.IntSlider("Loop", _loopIndex, 0, MAX_LOOP);
            _pixelsPerCell = EditorGUILayout.IntSlider("Pixels per cell", _pixelsPerCell, MIN_PIXELS_PER_CELL, MAX_PIXELS_PER_CELL);
            _serializedWindow.Update();
            EditorGUILayout.PropertyField(_scenarioProperty, true);
            _serializedWindow.ApplyModifiedProperties();
            if (GUILayout.Button("Generate"))
                Generate();

            _isLive = EditorGUILayout.Toggle("Live field (Play mode, host)", _isLive);
        }

        private void DrawSummary() {
            foreach (string line in _summaryLines)
                EditorGUILayout.LabelField(line);

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
            DrawLegendEntry(FlightFieldMapLegend.Ship, "Ship");
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
            FlightField field = composition.Generator.Generate(request);
            double generationMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
            DestroyMap();
            _map = new FlightFieldMapPainter().Paint(field, _pixelsPerCell);
            FillSummary(field, generationMilliseconds);
            Repaint();
        }

        private void ShowLiveField() {
            DestroyMap();
            _summaryLines.Clear();
            if (!new FlightFieldLiveReader().TryRead(out FlightFieldLiveSnapshot snapshot)) {
                _summaryLines.Add(NO_LIVE_FIELD);
                Repaint();
                return;
            }

            FlightFieldMapPainter painter = new();
            _map = painter.Paint(snapshot.Field, _pixelsPerCell);
            Vector2Int shipCell = snapshot.LiveFrame.WorldToCell(snapshot.ShipPosition);
            if (snapshot.HasShip)
                painter.PaintShip(_map, shipCell, _pixelsPerCell);

            FillSummary(snapshot.Field, 0d);
            _summaryLines.Insert(0, $"Live field {snapshot.Number}, origin {snapshot.LiveFrame.Origin}, ship cell {(snapshot.HasShip ? shipCell.ToString() : "-")}");
            Repaint();
        }

        private void FillSummary(FlightField field, double generationMilliseconds) {
            _summaryLines.Clear();
            _summaryLines.Add($"Field {field.Frame.LengthCells} x {field.Frame.WidthCells} cells of {field.Frame.CellSize} m, seed used {field.Seed}, generated in {generationMilliseconds:0.0} ms"
                + (field.IsPassageForced ? PASSAGE_FORCED_NOTE : string.Empty));
            string structures = string.Join(", ", field.Placements.GroupBy(placement => placement.StructureId).OrderBy(group => group.Key)
                .Select(group => $"{group.Key}: {group.Count()}"));
            _summaryLines.Add($"Placements {field.Placements.Count} ({structures})");
            foreach (FieldRoute route in field.Routes)
                _summaryLines.Add($"Route {route.Id}: difficulty {route.Difficulty:0.00}, corridor {route.CorridorWidthCells} cells, centerline {route.Centerline.Count} cells");
        }

        private void DestroyMap() {
            if (_map != null)
                DestroyImmediate(_map);

            _map = null;
        }
    }
}
