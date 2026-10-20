using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Features.AddressablesConstantsGenerator.Generated;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;
using UnityEditor;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor {
    // Editor map of a generated field: pick a seed, a loop and (optionally) a difficulty profile, generate with the
    // game's configuration and look at the zones or the density map, every route, the shortest way along the route
    // and outside all corridors, and the balance numbers. It does not need Play mode. "Live field" shows the field of
    // the running game (host) instead, with the ship, refreshed twice a second.
    public sealed class FlightFieldDebugMapWindow : EditorWindow {
        private const string MENU_PATH = "Tools/Flight Field/Debug Map";
        private const string TITLE = "Flight Field Map";
        private const string PASSAGE_FORCED_NOTE = ", PASSAGE FORCED";
        private const string PROFILE_BY_LOOP = "By loop";
        private const int MIN_PIXELS_PER_CELL = 2;
        private const int MAX_PIXELS_PER_CELL = 24;
        private const int MAX_LOOP = 30;
        private const float LEGEND_SWATCH_SIZE = 12f;
        private const double LIVE_REFRESH_SECONDS = 0.5;
        private const float METERS_PER_KILOMETER = 1000f;
        private const string NO_LIVE_FIELD = "No live field: enter Play mode as host and launch the ship.";
        // The upper diameters of the small, medium and large rocks in the size line; above them the massive ones.
        private static readonly float[] _sizeLimitsMeters = { 35f, 150f, 350f };

        [SerializeField] private int _seed = 1;
        [SerializeField] private int _loopIndex;
        [SerializeField] private int _pixelsPerCell = 12;
        [SerializeField] private int _profileChoice;
        [SerializeField] private FlightFieldMapLayer _layer = FlightFieldMapLayer.Zones;
        [SerializeField] private bool _isShowingPaths = true;
        [SerializeField] private bool _isShowingSlalom = true;
        [SerializeField] private FieldDebugScenario _scenario = new();
        [SerializeField] private bool _isLive;

        private readonly List<string> _summaryLines = new();

        private SerializedObject _serializedWindow;
        private SerializedProperty _scenarioProperty;
        private Texture2D _map;
        private Vector2 _scroll;
        private double _nextLiveRefresh;
        private string[] _profileChoices;

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
            _profileChoice = EditorGUILayout.Popup("Profile", _profileChoice, GetProfileChoices());
            _layer = (FlightFieldMapLayer)EditorGUILayout.EnumPopup("Layer", _layer);
            _isShowingPaths = EditorGUILayout.Toggle("Shortest ways", _isShowingPaths);
            _isShowingSlalom = EditorGUILayout.Toggle("Rocks and slalom way", _isShowingSlalom);
            _pixelsPerCell = EditorGUILayout.IntSlider("Pixels per cell", _pixelsPerCell, MIN_PIXELS_PER_CELL, MAX_PIXELS_PER_CELL);
            _serializedWindow.Update();
            EditorGUILayout.PropertyField(_scenarioProperty, true);
            _serializedWindow.ApplyModifiedProperties();
            if (GUILayout.Button("Generate"))
                Generate();

            _isLive = EditorGUILayout.Toggle("Live field (Play mode, host)", _isLive);
        }

        // Read once per window: the profile list changes only with the configuration asset.
        private string[] GetProfileChoices() {
            if (_profileChoices != null)
                return _profileChoices;

            FlightFieldConfiguration configuration = FlightFieldEditorCompositionFactory.LoadConfiguration<FlightFieldConfiguration>(Address.Configurations.FlightFieldConfiguration_Default);
            _profileChoices = new[] { PROFILE_BY_LOOP }.Concat(configuration.DifficultyProfiles.Select(profile => profile.Id)).ToArray();
            return _profileChoices;
        }

        private void DrawSummary() {
            foreach (string line in _summaryLines)
                EditorGUILayout.LabelField(line);

            DrawLegend();
        }

        private static void DrawLegend() {
            EditorGUILayout.BeginHorizontal();
            DrawLegendEntry(FlightFieldMapLegend.Structure, "Giant");
            DrawLegendEntry(FlightFieldMapLegend.Debris, "Debris");
            DrawLegendEntry(FlightFieldMapLegend.StationZone, "Station zone");
            DrawLegendEntry(FlightFieldMapLegend.TakeoffCorridor, "Takeoff");
            DrawLegendEntry(FlightFieldMapLegend.RouteCorridor, "Main corridor");
            DrawLegendEntry(FlightFieldMapLegend.AlternativeCorridor, "Alternative");
            DrawLegendEntry(FlightFieldMapLegend.DeadEndCorridor, "Dead end");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            DrawLegendEntry(FlightFieldMapLegend.Centerline, "Main line");
            DrawLegendEntry(FlightFieldMapLegend.AlternativeCenterline, "Alternative line");
            DrawLegendEntry(FlightFieldMapLegend.ClosedAlternativeCenterline, "Closed alternative");
            DrawLegendEntry(FlightFieldMapLegend.DeadEndCenterline, "Dead end line");
            DrawLegendEntry(FlightFieldMapLegend.RoutePath, "L_route way");
            DrawLegendEntry(FlightFieldMapLegend.FreePath, "L_free way");
            DrawLegendEntry(FlightFieldMapLegend.StationA, "A");
            DrawLegendEntry(FlightFieldMapLegend.StationB, "B");
            DrawLegendEntry(FlightFieldMapLegend.Ship, "Ship");
            DrawLegendEntry(FlightFieldMapLegend.Rock, "Rock");
            DrawLegendEntry(FlightFieldMapLegend.OffPlaneRock, "Rock off plane");
            DrawLegendEntry(FlightFieldMapLegend.SlalomPath, "Slalom way");
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawLegendEntry(Color color, string label) {
            Rect swatch = GUILayoutUtility.GetRect(LEGEND_SWATCH_SIZE, LEGEND_SWATCH_SIZE, GUILayout.ExpandWidth(false));
            EditorGUI.DrawRect(swatch, color);
            GUILayout.Label(label, GUILayout.ExpandWidth(false));
        }

        private void Generate() {
            FlightFieldEditorComposition composition = new FlightFieldEditorCompositionFactory().Create();
            FieldRequest request = WithChosenProfile(_scenario.CreateRequest(composition.RequestFactory, _seed, _loopIndex), composition.Configuration);
            Stopwatch stopwatch = Stopwatch.StartNew();
            FlightField field = composition.Generator.Generate(request);
            double generationMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
            DestroyMap();
            FlightFieldMapPainter painter = new();
            _map = painter.Paint(field, _pixelsPerCell, _layer, _isShowingPaths);
            FillSummary(field, generationMilliseconds);
            if (_isShowingSlalom)
                ShowSlalom(painter, new FieldSlalomAnalyzer(composition).Analyze(field), field.Frame.CellSize);

            Repaint();
        }

        private void ShowSlalom(FlightFieldMapPainter painter, FieldSlalomReport report, float cellSize) {
            painter.PaintSlalom(_map, report, cellSize, _pixelsPerCell);
            string way = report.IsPathFound
                ? $"way {report.PathLengthMeters:0} m vs straight {report.StraightLengthMeters:0} m ({report.PathToStraightRatio:0.00}), narrowest gap {report.MinGapMeters:0.0} m"
                : "NO WAY for the ship";
            _summaryLines.Add($"Slalom: {way}; rocks {report.Rocks.Count}, in main channels {report.ChannelRocks} over {report.ChannelLengthMeters / METERS_PER_KILOMETER:0.0} km "
                + $"= {report.RocksPerKilometer:0} per km");
            int[] sizes = report.CountBySize(_sizeLimitsMeters);
            _summaryLines.Add($"Flight plane: {report.PlaneRocksPerSquareKilometer():0} rocks per km2 (giants {report.Plane.GiantDiametersMeters.Count}), largest {report.LargestRockMeters():0} m; "
                + $"sizes up to {_sizeLimitsMeters[0]:0} m: {sizes[0]}, up to {_sizeLimitsMeters[1]:0} m: {sizes[1]}, up to {_sizeLimitsMeters[2]:0} m: {sizes[2]}, "
                + $"above: {sizes[3]}");
        }

        private FieldRequest WithChosenProfile(FieldRequest request, FlightFieldConfiguration configuration) {
            int profileIndex = _profileChoice - 1;
            bool isProfileChosen = profileIndex >= 0 && profileIndex < configuration.DifficultyProfiles.Count;
            return isProfileChosen
                ? new FieldRequest(request.StationA, request.StationB, request.CruiseStart, request.LoopIndex, request.Seed, request.RouteRequests,
                    configuration.DifficultyProfiles[profileIndex])
                : request;
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
            _map = painter.Paint(snapshot.Field, _pixelsPerCell, _layer, _isShowingPaths);
            Vector2Int shipCell = snapshot.LiveFrame.WorldToCell(snapshot.ShipPosition);
            if (snapshot.HasShip)
                painter.PaintShip(_map, shipCell, _pixelsPerCell);

            FillSummary(snapshot.Field, 0d);
            _summaryLines.Insert(0, $"Live field {snapshot.Number}, origin {snapshot.LiveFrame.Origin}, ship cell {(snapshot.HasShip ? shipCell.ToString() : "-")}");
            Repaint();
        }

        private void FillSummary(FlightField field, double generationMilliseconds) {
            _summaryLines.Clear();
            _summaryLines.Add($"Profile {field.ProfileId}, difficulty {field.Difficulty:0.00}. Field {field.Frame.LengthCells} x {field.Frame.WidthCells} cells of {field.Frame.CellSize} m, "
                + $"seed used {field.Seed}, generated in {generationMilliseconds:0.0} ms" + (field.IsPassageForced ? PASSAGE_FORCED_NOTE : string.Empty));
            string structures = string.Join(", ", field.Placements.GroupBy(placement => placement.StructureId).OrderBy(group => group.Key)
                .Select(group => $"{group.Key}: {group.Count()}"));
            _summaryLines.Add($"Placements {field.Placements.Count} ({structures})");
            FieldMetrics metrics = field.Metrics;
            string freeWay = metrics.HasFreePath ? $"{metrics.FreeLength} cells, L_free / L_route {metrics.FreeToRouteRatio:0.00}" : "none";
            _summaryLines.Add($"L_route {metrics.RouteLength} cells, L_free {freeWay}, straight A -> B blocked: {metrics.IsStraightBlocked}, "
                + $"metric met: {metrics.IsPathMetricMet} after {metrics.PathRepairRounds} repair rounds");
            _summaryLines.Add($"Zones: giant {metrics.BlockedShare:P0}, debris {metrics.DebrisShare:P0}, empty {metrics.EmptyShare:P0}; "
                + $"smallest corridor gap {metrics.MinCorridorGapMeters:0.0} m, dodges per minute {metrics.DodgesPerMinute:0.0}");
            foreach (FieldRoute route in field.Routes)
                _summaryLines.Add($"Route {route.Id} {route.Kind}{(route.IsClosed ? " (closed)" : string.Empty)}: difficulty {route.Difficulty:0.00}, corridor {route.CorridorWidthCells}+ cells, centerline {route.Centerline.Count} cells");
        }

        private void DestroyMap() {
            if (_map != null)
                DestroyImmediate(_map);

            _map = null;
        }
    }
}
