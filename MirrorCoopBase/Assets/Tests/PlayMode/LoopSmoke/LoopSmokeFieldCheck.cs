#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Network.Generated;
using Features.FlightFieldModule.Scripts.Scene;
using Features.FlightFieldModule.Scripts.Server;
using Features.FlightFieldModule.Scripts.Structures;
using Features.GameCoreModule.Contracts;
using Features.ShipModule.Scripts;
using Mirror;
using Tests.Shared;
using NUnit.Framework;
using UnityEngine;
using Zenject;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tests.PlayMode.LoopSmoke {
    // The flight field on the host. In cruise a new field stands: one active local object per local placement near
    // the camera under the field root (every one within the build radius, none beyond the release radius), one Mirror
    // object per networked placement (the test's catalog and difficulty profiles add the debug networked beacon).
    // After landing nothing of it is left, also for a player who joins at that station. A world shift in flight moves
    // the field with the ship. Pooled objects never grow past what the fields needed.
    public sealed class LoopSmokeFieldCheck {
        private const float BUILD_TIMEOUT_SECONDS = 10f;
        private const float CLEAR_TIMEOUT_SECONDS = 5f;
        private const float SHIFT_TOLERANCE = 0.01f;
        private const float MIN_SHIFT = 1f;
        // The camera moves between the last streaming pass and the check; the system measures from the anchor cell.
        private const float STREAMING_EDGE_METERS = 50f;
#if UNITY_EDITOR
        private const string DEBUG_NETWORKED_STRUCTURE_PATH = "Assets/Features/FlightFieldModule/GameResources/Debug/Debug_NetworkBeacon.asset";
        private const string STRUCTURES_PROPERTY = "_structures";
        private const string PROFILES_PROPERTY = "_difficultyProfiles";
        private const string LOOP_ROUTES_PROPERTY = "_routesByLoop";
        private const string LOOP_PROFILES_PROPERTY = "_profiles";
        private const string LOOP_PROFILE_PROPERTY = "_profile";
        private const string OBSTACLE_STRUCTURE_PROPERTY = "_structure";
        private const string OBSTACLE_WEIGHT_PROPERTY = "_weight";
        private const string GIANT_CLASSES_PROPERTY = "_giantClasses";
        private const string GIANT_CLASS_NAME_PROPERTY = "_name";
        private const string GIANT_CLASS_OBSTACLES_PROPERTY = "_obstacles";
        private const string DEBUG_GIANT_CLASS_NAME = "Debug networked beacon";
        // The weight of the beacon's giant class: a few beacons per field, not hundreds of Mirror objects.
        private const float BEACON_WEIGHT = 0.25f;
#endif

        private readonly Dictionary<string, int> _mostLocalObjectsByStructure = new Dictionary<string, int>();
        private readonly IReadOnlyLiveFieldModel _liveFieldModel;
        private readonly IReadOnlyFlightFieldSceneModel _flightFieldSceneModel;
        private readonly IReadOnlyFlightFieldServerModel _flightFieldServerModel;
        private readonly FieldStructureCatalog _fieldStructureCatalog;
        private readonly FlightFieldConfiguration _flightFieldConfiguration;
        private readonly ILocalViewpointSource _localViewpointSource;

        private int _lastFieldNumber;
        private int _networkedPlacementsSeen;

        public int LastPlacements { get; private set; }
        public int LastFieldObjects { get; private set; }

        public int PooledObjects =>
            _flightFieldSceneModel.HasRoot ? _flightFieldSceneModel.Root.childCount : 0;

        public LoopSmokeFieldCheck(DiContainer container) {
            _liveFieldModel = container.Resolve<IReadOnlyLiveFieldModel>();
            _flightFieldSceneModel = container.Resolve<IReadOnlyFlightFieldSceneModel>();
            _flightFieldServerModel = container.Resolve<IReadOnlyFlightFieldServerModel>();
            _fieldStructureCatalog = container.Resolve<FieldStructureCatalog>();
            _flightFieldConfiguration = SessionQueries.Resolve<FlightFieldConfiguration>();
            _localViewpointSource = SessionQueries.Resolve<ILocalViewpointSource>();
        }

        // A copy of the game's catalog with the debug networked beacon, which the shipped catalog does not have.
        public static FieldStructureCatalog CreateCatalogWithNetworkedStructure(FieldStructureCatalog catalog) {
            FieldStructureCatalog copy = Object.Instantiate(catalog);
#if UNITY_EDITOR
            SerializedObject serializedCopy = new SerializedObject(copy);
            SerializedProperty structures = serializedCopy.FindProperty(STRUCTURES_PROPERTY);
            structures.arraySize++;
            structures.GetArrayElementAtIndex(structures.arraySize - 1).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<FieldStructureDefinition>(DEBUG_NETWORKED_STRUCTURE_PATH);
            serializedCopy.ApplyModifiedPropertiesWithoutUndo();
#endif
            return copy;
        }

        // A copy of the game's field configuration whose difficulty profiles (copies too) may place the debug
        // networked beacon as a giant: the obstacle types of a field come from its profile. The copies go to the list.
        public static FlightFieldConfiguration CreateConfigurationWithNetworkedObstacle(FlightFieldConfiguration configuration, List<Object> copies) {
            FlightFieldConfiguration copy = Object.Instantiate(configuration);
            copies.Add(copy);
#if UNITY_EDITOR
            Dictionary<Object, Object> profileCopies = new Dictionary<Object, Object>();
            SerializedObject serializedCopy = new SerializedObject(copy);
            SerializedProperty profiles = serializedCopy.FindProperty(PROFILES_PROPERTY);
            for (int i = 0; i < profiles.arraySize; i++) {
                SerializedProperty profile = profiles.GetArrayElementAtIndex(i);
                Object profileCopy = CreateProfileWithNetworkedObstacle(profile.objectReferenceValue);
                profileCopies.Add(profile.objectReferenceValue, profileCopy);
                copies.Add(profileCopy);
                profile.objectReferenceValue = profileCopy;
            }

            SerializedProperty loops = serializedCopy.FindProperty(LOOP_ROUTES_PROPERTY);
            for (int i = 0; i < loops.arraySize; i++)
                PointLoopAtCopies(loops.GetArrayElementAtIndex(i).FindPropertyRelative(LOOP_PROFILES_PROPERTY), profileCopies);

            serializedCopy.ApplyModifiedPropertiesWithoutUndo();
#endif
            return copy;
        }

#if UNITY_EDITOR
        private static Object CreateProfileWithNetworkedObstacle(Object profile) {
            Object copy = Object.Instantiate(profile);
            SerializedObject serializedCopy = new SerializedObject(copy);
            AddNetworkedGiantClass(serializedCopy.FindProperty(GIANT_CLASSES_PROPERTY));
            serializedCopy.ApplyModifiedPropertiesWithoutUndo();
            return copy;
        }

        // The beacon comes as a giant class of its own.
        private static void AddNetworkedGiantClass(SerializedProperty giantClasses) {
            giantClasses.arraySize++;
            SerializedProperty giantClass = giantClasses.GetArrayElementAtIndex(giantClasses.arraySize - 1);
            giantClass.FindPropertyRelative(GIANT_CLASS_NAME_PROPERTY).stringValue = DEBUG_GIANT_CLASS_NAME;
            giantClass.FindPropertyRelative(OBSTACLE_WEIGHT_PROPERTY).floatValue = BEACON_WEIGHT;
            SerializedProperty obstacles = giantClass.FindPropertyRelative(GIANT_CLASS_OBSTACLES_PROPERTY);
            obstacles.arraySize = 1;
            SerializedProperty beacon = obstacles.GetArrayElementAtIndex(0);
            beacon.FindPropertyRelative(OBSTACLE_STRUCTURE_PROPERTY).objectReferenceValue = AssetDatabase.LoadAssetAtPath<FieldStructureDefinition>(DEBUG_NETWORKED_STRUCTURE_PATH);
            beacon.FindPropertyRelative(OBSTACLE_WEIGHT_PROPERTY).floatValue = 1f;
        }

        private static void PointLoopAtCopies(SerializedProperty loopProfiles, Dictionary<Object, Object> profileCopies) {
            for (int i = 0; i < loopProfiles.arraySize; i++) {
                SerializedProperty profile = loopProfiles.GetArrayElementAtIndex(i).FindPropertyRelative(LOOP_PROFILE_PROPERTY);
                profile.objectReferenceValue = profileCopies[profile.objectReferenceValue];
            }
        }
#endif

        public IEnumerator AssertNewFieldStandsCoroutine(int loopIndex) {
            yield return WaitForCoroutine(IsLiveFieldBuilt, BUILD_TIMEOUT_SECONDS, $"the field of loop {loopIndex} to be built");
            Assert.Greater(_liveFieldModel.Placements.Count, 0, $"The field of loop {loopIndex} has no placements.");
            Assert.AreNotEqual(_lastFieldNumber, _liveFieldModel.Number, $"Loop {loopIndex} did not get a new field.");
            _lastFieldNumber = _liveFieldModel.Number;

            Dictionary<string, int> localByStructure = CountLocalPlacementsByStructure(out int networkedPlacements);
            int localPlacements = Sum(localByStructure);
            int activeLocalObjects = CountActiveLocalObjects();
            int mustStand = CountLocalPlacementsWithin(_flightFieldConfiguration.BuildRadius - STREAMING_EDGE_METERS);
            int mayStand = CountLocalPlacementsWithin(_flightFieldConfiguration.ReleaseRadius + STREAMING_EDGE_METERS);
            Assert.GreaterOrEqual(activeLocalObjects, mustStand, $"Loop {loopIndex}: local placements within the build radius are not built.");
            Assert.LessOrEqual(activeLocalObjects, mayStand, $"Loop {loopIndex}: local field objects stand beyond the release radius.");
            Assert.AreEqual(networkedPlacements, CountSpawnedNetworkedStructures(), $"Loop {loopIndex}: Mirror field objects differ from the networked placements.");
            _networkedPlacementsSeen += networkedPlacements;
            LastPlacements = localPlacements + networkedPlacements;
            LastFieldObjects = CountActiveLocalObjects() + CountSpawnedNetworkedStructures();
            RememberMostLocalObjects(localByStructure);
            Assert.LessOrEqual(_flightFieldSceneModel.Root.childCount, Sum(_mostLocalObjectsByStructure), "The pool holds more objects than any field needed.");
            Debug.Log($"[LoopSmoke] loop {loopIndex} field {_liveFieldModel.Number}: {_liveFieldModel.LengthCells} x {_liveFieldModel.WidthCells} cells, "
                      + $"{localPlacements} local + {networkedPlacements} networked placements, {activeLocalObjects} local objects active "
                      + $"({mustStand} within the build radius), "
                      + $"{_flightFieldSceneModel.Root.childCount} pooled in all");
        }

        // The beacon is picked by weight like any obstacle, so a single field may have none.
        public void AssertNetworkedPathWasChecked() =>
            Assert.Greater(_networkedPlacementsSeen, 0, "No field had a networked placement: the Mirror path was not checked.");

        // A player who joins at this station gets this state as the model snapshot: no field, nothing to build.
        public IEnumerator AssertFieldClearedCoroutine(int loopIndex) {
            Assert.AreEqual(LiveFieldNumbers.NO_FIELD, _liveFieldModel.Number, $"The field of loop {loopIndex} is still published after landing.");
            Assert.AreEqual(0, _liveFieldModel.Placements.Count, $"Placements of loop {loopIndex} are still published after landing.");
            Assert.IsFalse(_flightFieldServerModel.HasField, $"The server still keeps the field of loop {loopIndex}.");
            yield return WaitForCoroutine(IsSceneFieldCleared, CLEAR_TIMEOUT_SECONDS, $"the objects of the field of loop {loopIndex} to be released");
            Assert.AreEqual(0, CountActiveLocalObjects(), $"Local field objects of loop {loopIndex} are left after landing.");
            Assert.AreEqual(0, CountSpawnedNetworkedStructures(), $"Mirror field objects of loop {loopIndex} are left after landing.");
        }

        public IEnumerator AssertFieldFollowsWorldShiftCoroutine(Transform ship, IShipWorldShiftDebug shipWorldShiftDebug) {
            yield return WaitForCoroutine(IsLiveFieldBuilt, BUILD_TIMEOUT_SECONDS, "the field to be built before the world shift");
            Transform root = _flightFieldSceneModel.Root;
            Vector3 rootFromShip = root.position - ship.position;
            bool hasNetworked = TryFindSpawnedNetworkedStructure(out Transform networked);
            Vector3 networkedFromShip = hasNetworked ? networked.position - ship.position : Vector3.zero;
            Vector3 origin = _liveFieldModel.Origin;
            int shifts = shipWorldShiftDebug.WorldShiftCount;

            shipWorldShiftDebug.DebugForceRecenter();

            Assert.AreEqual(shifts + 1, shipWorldShiftDebug.WorldShiftCount, "The debug recenter did not shift the world.");
            Assert.Greater((_liveFieldModel.Origin - origin).magnitude, MIN_SHIFT, "The live field origin did not move with the world shift.");
            Assert.Less((root.position - ship.position - rootFromShip).magnitude, SHIFT_TOLERANCE, "The local field objects jumped against the ship in the world shift.");
            if (hasNetworked)
                Assert.Less((networked.position - ship.position - networkedFromShip).magnitude, SHIFT_TOLERANCE, "A Mirror field object jumped against the ship in the world shift.");
        }

        private bool IsLiveFieldBuilt() =>
            _liveFieldModel.Number != LiveFieldNumbers.NO_FIELD && _flightFieldSceneModel.BuiltNumber == _liveFieldModel.Number && _flightFieldSceneModel.IsBuilt;

        private int CountLocalPlacementsWithin(float radius) {
            FieldFrame frame = _flightFieldSceneModel.Frame;
            Vector2 viewpointOnPlane = frame.WorldToPlane(_localViewpointSource.GetPosition());
            int count = 0;
            foreach (FieldPlacementEntry placement in _liveFieldModel.Placements) {
                if (_fieldStructureCatalog.GetRequired(placement.StructureId).IsNetworked)
                    continue;

                if ((frame.CellToPlane(placement.Anchor) - viewpointOnPlane).sqrMagnitude <= radius * radius)
                    count++;
            }

            return count;
        }

        private bool IsSceneFieldCleared() =>
            _flightFieldSceneModel.BuiltNumber == LiveFieldNumbers.NO_FIELD;

        private Dictionary<string, int> CountLocalPlacementsByStructure(out int networkedPlacements) {
            Dictionary<string, int> localByStructure = new Dictionary<string, int>();
            networkedPlacements = 0;
            foreach (FieldPlacementEntry placement in _liveFieldModel.Placements) {
                Assert.IsTrue(_fieldStructureCatalog.TryGet(placement.StructureId, out FieldStructureDefinition structure), $"Placement of unknown structure {placement.StructureId}.");
                if (structure.IsNetworked) {
                    networkedPlacements++;
                    continue;
                }

                localByStructure.TryGetValue(placement.StructureId, out int count);
                localByStructure[placement.StructureId] = count + 1;
            }

            return localByStructure;
        }

        private void RememberMostLocalObjects(Dictionary<string, int> localByStructure) {
            foreach (KeyValuePair<string, int> pair in localByStructure) {
                _mostLocalObjectsByStructure.TryGetValue(pair.Key, out int most);
                _mostLocalObjectsByStructure[pair.Key] = Mathf.Max(most, pair.Value);
            }
        }

        private int CountActiveLocalObjects() {
            if (!_flightFieldSceneModel.HasRoot)
                return 0;

            int count = 0;
            foreach (Transform child in _flightFieldSceneModel.Root) {
                if (child.gameObject.activeSelf)
                    count++;
            }

            return count;
        }

        private static int CountSpawnedNetworkedStructures() {
            int count = 0;
            foreach (NetworkIdentity identity in NetworkServer.spawned.Values) {
                if (identity != null && identity.TryGetComponent(out FieldNetworkedStructure _))
                    count++;
            }

            return count;
        }

        private static bool TryFindSpawnedNetworkedStructure(out Transform structure) {
            structure = null;
            foreach (NetworkIdentity identity in NetworkServer.spawned.Values) {
                if (identity == null || !identity.TryGetComponent(out FieldNetworkedStructure _))
                    continue;

                structure = identity.transform;
                return true;
            }

            return false;
        }

        private static int Sum(Dictionary<string, int> counts) {
            int sum = 0;
            foreach (int count in counts.Values)
                sum += count;

            return sum;
        }

        private static IEnumerator WaitForCoroutine(System.Func<bool> condition, float timeoutSeconds, string target) {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (condition() == false) {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Timed out after {timeoutSeconds} s waiting for {target}.");

                yield return null;
            }
        }
    }
}
#endif
