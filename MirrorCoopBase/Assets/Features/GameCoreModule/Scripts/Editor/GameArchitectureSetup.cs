#if UNITY_EDITOR
using System.IO;
using Features.AddressablesConstantsGenerator.Generated;
using Features.BootstrapersModule.Scripts;
using Features.CameraModule.Scripts;
using Features.GameCoreModule.Scripts.Constants;
using Features.GameCoreModule.Scripts.Installers;
using Features.LobbyModule.Scripts;
using Features.MenuModule.Scripts;
using Features.MvpModule;
using Features.CharacterMovableModule.Scripts;
using Features.FloatingControllerModule;
using Features.GrabModule.Scripts;
using Features.ShipModule.Scripts;
using Game.Connection;
using Mirror;
using Mirror.FizzySteam;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

namespace Features.GameCoreModule.Scripts.Editor {
    public static class GameArchitectureSetup {
        private const string FeaturesRoot = "Assets/Features";
        private const string GameResourcesRoot = FeaturesRoot + "/GameCoreModule/GameResources";
        private const string ScenesRoot = GameResourcesRoot + "/Scenes";
        private const string ResourcesRoot = GameResourcesRoot + "/Resources";
        private const string LobbyPrefabsRoot = FeaturesRoot + "/LobbyModule/GameResources/Prefabs";
        private const string PlayerPrefabsRoot = FeaturesRoot + "/PlayerModule/GameResources/Prefabs";
        private const string CameraModuleRoot = FeaturesRoot + "/CameraModule";
        private const string CameraPrefabsRoot = CameraModuleRoot + "/GameResources/Prefabs";
        private const string CameraResourcesRoot = CameraModuleRoot + "/GameResources/Resources";
        private const string CameraCatalogPath = CameraResourcesRoot + "/CameraCatalog.asset";
        private const string FpCameraPrefabPath = CameraPrefabsRoot + "/FPCamera.prefab";
        private const string TpCameraPrefabPath = CameraPrefabsRoot + "/TPCamera.prefab";
        private const string GrabModuleRoot = FeaturesRoot + "/GrabModule";
        private const string GrabPrefabsRoot = GrabModuleRoot + "/GameResources/Prefabs";
        private const string DummyGrabbablePrefabPath = GrabPrefabsRoot + "/DummyGrabbable.prefab";
        private const string ShipModuleRoot = FeaturesRoot + "/ShipModule";
        private const string ShipPrefabsRoot = ShipModuleRoot + "/GameResources/Prefabs";
        private const string ShipResourcesRoot = ShipModuleRoot + "/GameResources/Resources";
        private const string ShipConfigurationsRoot = ShipModuleRoot + "/GameResources/Configurations";
        private const string RocketEngineItemPrefabPath = ShipPrefabsRoot + "/RocketEngineItem.prefab";
        private const string EngineItemViewPrefabPath = ShipPrefabsRoot + "/EngineItemView.prefab";
        private const string LargeEngineItemPrefabPath = ShipPrefabsRoot + "/LargeEngineItem.prefab";
        private const string LargeEngineItemViewPrefabPath = ShipPrefabsRoot + "/LargeEngineItemView.prefab";
        private const string PropellerItemPrefabPath = ShipPrefabsRoot + "/PropellerItem.prefab";
        private const string PropellerItemViewPrefabPath = ShipPrefabsRoot + "/PropellerItemView.prefab";
        private const string ItemViewCatalogPath = ShipResourcesRoot + "/ItemViewCatalog.asset";
        private const string EngineCatalogPath = ShipResourcesRoot + "/EngineCatalog.asset";
        private const string ShipFlightConfigPath = ShipConfigurationsRoot + "/ShipFlightConfig_Default.asset";
        private const string HelmItemPrefabPath = ShipPrefabsRoot + "/HelmItem.prefab";
        private const string HelmItemViewPrefabPath = ShipPrefabsRoot + "/HelmItemView.prefab";
        private const string ShipPlatformPrefabPath = ShipPrefabsRoot + "/ShipPlatform.prefab";
        private const string StationPadPrefabPath = ShipPrefabsRoot + "/StationPad.prefab";
        private const string ShipWreckPrefabPath = ShipPrefabsRoot + "/ShipWreck.prefab";
        private const string CruiseRockPrefabPath = ShipPrefabsRoot + "/CruiseRock.prefab";
        private const string ShipRunConfigPath = ShipConfigurationsRoot + "/ShipRunConfig_Default.asset";
        private const string ShipStationCatalogPath = ShipResourcesRoot + "/ShipStationCatalog.asset";
        private const string MenuPrefabsRoot = FeaturesRoot + "/MenuModule/GameResources/Prefabs";
        private const string ConfigsRoot = FeaturesRoot + "/Connection/GameResources/Configurations";
        private const string ProjectContextPath = ResourcesRoot + "/ProjectContext.prefab";
        private const string PlayerPrefabPath = PlayerPrefabsRoot + "/Player.prefab";
        private const string DummyPlayerPrefabPath = PlayerPrefabsRoot + "/DummyPlayer.prefab";
        private const string LegacyLobbyPlayerPrefabPath = LobbyPrefabsRoot + "/LobbyPlayer.prefab";
        private const string ConnectionConfigPath = ConfigsRoot + "/ConnectionConfig_Default.asset";
        private const string MenuWindowPrefabPath = MenuPrefabsRoot + "/MenuWindow.prefab";
        private const string GameHudWindowPrefabPath = LobbyPrefabsRoot + "/GameHudWindow.prefab";
        private const string BootstrapScenePath = ScenesRoot + "/" + SceneNames.Bootstrap + ".unity";
        private const string ConfigurationsAddressableGroup = "Configurations";
        private const string LocalScenesAddressableGroup = "Scenes";
        private const string WindowsAddressableGroup = "Windows";

        [InitializeOnLoadMethod]
        private static void AutoSetupIfMissing() {
            EditorApplication.delayCall += () => {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;

                bool bootstrapExists = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath) != null;
                if (bootstrapExists == false) {
                    Debug.Log("GameCore: scenes missing — running architecture setup...");
                    Setup();
                    return;
                }

                EnsureFolders();
                CreateConnectionConfig();
                EnsureCameraModuleAssets();
                GameObject playerPrefab = CreatePlayerPrefab();
                EnsureGrabAssets(playerPrefab);
                EnsureShipAssets();
                CreateBootstrapScene();
                CreateGlobalScene(playerPrefab);
                CreateMenuScene();
                CreateLobbyScene();
                CreateGameScene();
                EnsureLobbyGrabbable();
                StripLobbyShip();
                EnsureGameShip();
                CreateWindowPrefabs();
                StripLegacySceneUi();
                SetupAddressableScenesAndConfigs();
                UpdateBuildSettings();
                AssetDatabase.SaveAssets();
            };
        }

        [MenuItem("GameCore/Setup Architecture (Scenes, Prefabs, Build Settings)")]
        public static void Setup() {
            EnsureFolders();
            CreateConnectionConfig();
            CreateProjectContext();
            EnsureCameraModuleAssets();
            GameObject playerPrefab = CreatePlayerPrefab();
            EnsureGrabAssets(playerPrefab);
            EnsureShipAssets();

            CreateBootstrapScene();
            CreateGlobalScene(playerPrefab);
            CreateMenuScene();
            CreateLobbyScene();
            CreateGameScene();
            EnsureLobbyGrabbable();
            StripLobbyShip();
            EnsureGameShip();
            CreateWindowPrefabs();
            StripLegacySceneUi();
            SetupAddressableScenesAndConfigs();
            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("GameCore architecture setup complete. Open BootstrapScene and press Play.");
        }

        private static void EnsureFolders() {
            EnsureFolder("Assets/Features");
            EnsureFolder(FeaturesRoot + "/GameCoreModule");
            EnsureFolder(GameResourcesRoot);
            EnsureFolder(ScenesRoot);
            EnsureFolder(ResourcesRoot);
            EnsureFolder(FeaturesRoot + "/LobbyModule");
            EnsureFolder(FeaturesRoot + "/LobbyModule/GameResources");
            EnsureFolder(LobbyPrefabsRoot);
            EnsureFolder(FeaturesRoot + "/PlayerModule");
            EnsureFolder(FeaturesRoot + "/PlayerModule/GameResources");
            EnsureFolder(PlayerPrefabsRoot);
            EnsureFolder(CameraModuleRoot);
            EnsureFolder(CameraModuleRoot + "/GameResources");
            EnsureFolder(CameraPrefabsRoot);
            EnsureFolder(CameraResourcesRoot);
            EnsureFolder(FeaturesRoot + "/MenuModule");
            EnsureFolder(FeaturesRoot + "/MenuModule/GameResources");
            EnsureFolder(MenuPrefabsRoot);
            EnsureFolder(FeaturesRoot + "/Connection");
            EnsureFolder(FeaturesRoot + "/Connection/GameResources");
            EnsureFolder(ConfigsRoot);
            EnsureFolder(GrabModuleRoot);
            EnsureFolder(GrabModuleRoot + "/GameResources");
            EnsureFolder(GrabPrefabsRoot);
            EnsureFolder(ShipModuleRoot);
            EnsureFolder(ShipModuleRoot + "/GameResources");
            EnsureFolder(ShipPrefabsRoot);
            EnsureFolder(ShipResourcesRoot);
            EnsureFolder(ShipConfigurationsRoot);
        }

        private static void EnsureFolder(string path) {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            string name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) == false && AssetDatabase.IsValidFolder(parent) == false)
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
        }

        private static void CreateProjectContext() {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ProjectContextPath) != null)
                return;

            var gameObject = new GameObject("ProjectContext");
            try {
                ProjectContext projectContext = gameObject.AddComponent<ProjectContext>();
                ProjectContextInstaller installer = gameObject.AddComponent<ProjectContextInstaller>();
                projectContext.Installers = new MonoInstaller[] { installer };
                PrefabUtility.SaveAsPrefabAsset(gameObject, ProjectContextPath);
            }
            finally {
                Object.DestroyImmediate(gameObject);
            }
        }

        private static GameObject CreatePlayerPrefab() {
            GameObject dummy = AssetDatabase.LoadAssetAtPath<GameObject>(DummyPlayerPrefabPath);
            if (dummy != null) {
                EnsurePlayerCameraRig(dummy);
                EnsurePlayerShipRider(dummy);
                return dummy;
            }

            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (existing != null) {
                EnsurePlayerCameraRig(existing);
                EnsurePlayerShipRider(existing);
                return existing;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(LegacyLobbyPlayerPrefabPath) != null) {
                EnsureFolder(PlayerPrefabsRoot);
                AssetDatabase.MoveAsset(LegacyLobbyPlayerPrefabPath, PlayerPrefabPath);
                GameObject moved = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
                if (moved != null) {
                    moved.name = "Player";
                    EditorUtility.SetDirty(moved);
                    return moved;
                }
            }

            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";

            CapsuleCollider capsuleCollider = player.GetComponent<CapsuleCollider>();
            capsuleCollider.height = 2f;
            capsuleCollider.center = new Vector3(0f, 1f, 0f);
            capsuleCollider.radius = 0.4f;

            Rigidbody rigidbody = player.AddComponent<Rigidbody>();
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

            player.AddComponent<NetworkIdentity>();
            NetworkRigidbodyUnreliable networkRigidbody = player.AddComponent<NetworkRigidbodyUnreliable>();
            networkRigidbody.syncDirection = SyncDirection.ClientToServer;

            CharacterMovable movable = player.AddComponent<CharacterMovable>();
            movable.syncDirection = SyncDirection.ClientToServer;
            CharacterMovableRegistrar registrar = player.AddComponent<CharacterMovableRegistrar>();
            FloatingController floatingController = player.AddComponent<FloatingController>();
            PlayerCameraAnchor cameraAnchor = player.AddComponent<PlayerCameraAnchor>();
            ZenAutoInjecter autoInjecter = player.AddComponent<ZenAutoInjecter>();
            autoInjecter.ContainerSource = ZenAutoInjecter.ContainerSources.SearchHierarchy;

            SerializedObject movableSerialized = new SerializedObject(movable);
            movableSerialized.FindProperty("_rb").objectReferenceValue = rigidbody;
            movableSerialized.FindProperty("_capsuleCollider").objectReferenceValue = capsuleCollider;
            movableSerialized.FindProperty("_floatingController").objectReferenceValue = floatingController;
            Transform rotatablePart = player.transform.Find("RotatablePart");
            if (rotatablePart != null)
                movableSerialized.FindProperty("_rotatablePart").objectReferenceValue = rotatablePart;
            movableSerialized.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject registrarSerialized = new SerializedObject(registrar);
            registrarSerialized.FindProperty("_movable").objectReferenceValue = movable;
            registrarSerialized.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject floatingSerialized = new SerializedObject(floatingController);
            floatingSerialized.FindProperty("_rb").objectReferenceValue = rigidbody;
            floatingSerialized.FindProperty("_capsuleCollider").objectReferenceValue = capsuleCollider;
            floatingSerialized.ApplyModifiedPropertiesWithoutUndo();

            AssignPlayerCameraAnchor(player, cameraAnchor);
            EnsurePlayerGrabController(player);
            EnsurePlayerUseController(player);
            EnsurePlayerShipRiderOnInstance(player);

            EnsureFolder(PlayerPrefabsRoot);
            PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
            Object.DestroyImmediate(player);
            return AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        }

        private static void EnsureCameraModuleAssets() {
            EnsureFolder(CameraPrefabsRoot);
            EnsureFolder(CameraResourcesRoot);

            GameObject fpPrefab = CreateCinemachineCameraPrefab(FpCameraPrefabPath, CameraIds.FPCamera, GameCameraKind.FirstPerson);
            GameObject tpPrefab = CreateCinemachineCameraPrefab(TpCameraPrefabPath, CameraIds.TPCamera, GameCameraKind.ThirdPerson);

            CameraCatalog catalog = AssetDatabase.LoadAssetAtPath<CameraCatalog>(CameraCatalogPath);
            if (catalog == null) {
                catalog = ScriptableObject.CreateInstance<CameraCatalog>();
                AssetDatabase.CreateAsset(catalog, CameraCatalogPath);
            }

            GameCamera fpCamera = fpPrefab != null ? fpPrefab.GetComponent<GameCamera>() : null;
            GameCamera tpCamera = tpPrefab != null ? tpPrefab.GetComponent<GameCamera>() : null;
            SerializedObject catalogSerialized = new SerializedObject(catalog);
            SerializedProperty startup = catalogSerialized.FindProperty("_startupCameraId");
            SerializedProperty blend = catalogSerialized.FindProperty("_defaultBlendSeconds");
            SerializedProperty fpProperty = catalogSerialized.FindProperty("_fpCameraPrefab");
            SerializedProperty tpProperty = catalogSerialized.FindProperty("_tpCameraPrefab");
            bool catalogDirty =
                startup.stringValue != CameraIds.TPCamera ||
                Mathf.Approximately(blend.floatValue, 0.45f) == false ||
                fpProperty.objectReferenceValue != fpCamera ||
                tpProperty.objectReferenceValue != tpCamera;
            if (catalogDirty == false)
                return;

            startup.stringValue = CameraIds.TPCamera;
            blend.floatValue = 0.45f;
            fpProperty.objectReferenceValue = fpCamera;
            tpProperty.objectReferenceValue = tpCamera;
            catalogSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static GameObject CreateCinemachineCameraPrefab(string path, string id, GameCameraKind kind) {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) {
                bool needsRebuild = kind == GameCameraKind.ThirdPerson &&
                    existing.GetComponent<Unity.Cinemachine.CinemachineRotationComposer>() != null;
                if (needsRebuild == false)
                    return existing;

                AssetDatabase.DeleteAsset(path);
            }

            GameCamera camera = GameCamera.Create(id, kind, null);
            GameObject created = camera.gameObject;
            PrefabUtility.SaveAsPrefabAsset(created, path);
            Object.DestroyImmediate(created);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static void EnsurePlayerCameraRig(GameObject prefabAsset) {
            if (prefabAsset == null)
                return;

            string path = AssetDatabase.GetAssetPath(prefabAsset);
            if (string.IsNullOrEmpty(path))
                return;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try {
                bool dirty = false;
                MonoBehaviour[] behaviours = root.GetComponents<MonoBehaviour>();
                for (int i = 0; i < behaviours.Length; i++) {
                    MonoBehaviour behaviour = behaviours[i];
                    if (behaviour == null || behaviour.GetType().Name != "PlayerLocalCameraFollow")
                        continue;

                    Object.DestroyImmediate(behaviour);
                    dirty = true;
                }

                PlayerCameraAnchor anchor = root.GetComponent<PlayerCameraAnchor>();
                if (anchor == null) {
                    anchor = root.AddComponent<PlayerCameraAnchor>();
                    dirty = true;
                }

                if (root.transform.Find("CameraFollow") == null ||
                    root.transform.Find("CameraLookAt") == null ||
                    root.transform.Find("CameraEye") == null)
                    dirty = true;

                if (dirty == false)
                    return;

                AssignPlayerCameraAnchor(root, anchor);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureGrabAssets(GameObject playerPrefab) {
            EnsureFolders();
            EnsureInteractableLayer();
            EnsurePlayerGrab(playerPrefab);
            GameObject itemPrefab = EnsureDummyGrabbablePrefab();
            RegisterSpawnPrefab(itemPrefab);
            EnsureLobbyGrabbable(itemPrefab);
        }

        private static void EnsureInteractableLayer() {
            UnityEngine.Object tagManager = AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset");
            if (tagManager == null)
                return;

            SerializedObject tags = new SerializedObject(tagManager);
            SerializedProperty layers = tags.FindProperty("layers");
            if (layers == null)
                return;

            for (int i = 0; i < layers.arraySize; i++) {
                if (layers.GetArrayElementAtIndex(i).stringValue == InteractableLayers.Name)
                    return;
            }

            for (int i = 8; i < layers.arraySize; i++) {
                SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layer.stringValue) == false)
                    continue;

                layer.stringValue = InteractableLayers.Name;
                tags.ApplyModifiedPropertiesWithoutUndo();
                return;
            }
        }

        private static void EnsurePlayerShipRider(GameObject prefabAsset) {
            if (prefabAsset == null)
                return;

            string path = AssetDatabase.GetAssetPath(prefabAsset);
            if (string.IsNullOrEmpty(path))
                return;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try {
                if (EnsurePlayerShipRiderOnInstance(root) == false)
                    return;

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool EnsurePlayerShipRiderOnInstance(GameObject player) {
            Rigidbody rigidbody = player.GetComponent<Rigidbody>();
            ShipRider rider = player.GetComponent<ShipRider>();
            bool dirty = false;
            if (rider == null) {
                rider = player.AddComponent<ShipRider>();
                dirty = true;
            }

            FloatingController floating = player.GetComponent<FloatingController>();
            NetworkRigidbodyUnreliable netBody = player.GetComponent<NetworkRigidbodyUnreliable>();
            SerializedObject serialized = new SerializedObject(rider);
            SerializedProperty body = serialized.FindProperty("_rb");
            SerializedProperty floatingProperty = serialized.FindProperty("_floating");
            SerializedProperty netBodyProperty = serialized.FindProperty("_netBody");
            SerializedProperty extents = serialized.FindProperty("_deckExtents");
            if (body.objectReferenceValue != rigidbody) {
                body.objectReferenceValue = rigidbody;
                dirty = true;
            }

            if (floatingProperty.objectReferenceValue != floating) {
                floatingProperty.objectReferenceValue = floating;
                dirty = true;
            }

            if (netBodyProperty.objectReferenceValue != netBody) {
                netBodyProperty.objectReferenceValue = netBody;
                dirty = true;
            }

            Vector2 wantedExtents = new Vector2(2.2f, 3.8f);
            if (extents.vector2Value != wantedExtents) {
                extents.vector2Value = wantedExtents;
                dirty = true;
            }

            if (dirty)
                serialized.ApplyModifiedPropertiesWithoutUndo();

            return dirty;
        }

        private static void EnsurePlayerGrab(GameObject prefabAsset) {
            if (prefabAsset == null)
                return;

            string path = AssetDatabase.GetAssetPath(prefabAsset);
            if (string.IsNullOrEmpty(path))
                return;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try {
                bool dirty = EnsurePlayerGrabController(root);
                dirty |= EnsurePlayerUseController(root);
                if (dirty == false)
                    return;

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool EnsurePlayerGrabController(GameObject player) {
            Transform armPoint = FindNamedChild(player.transform, "ArmPoint");
            if (armPoint == null)
                armPoint = EnsureChild(player.transform, "ArmPoint", new Vector3(0.183f, 1.179f, 0.728f));

            GrabController grab = player.GetComponent<GrabController>();
            bool dirty = false;
            if (grab == null) {
                grab = player.AddComponent<GrabController>();
                dirty = true;
            }

            int interactableBit = LayerMask.GetMask(InteractableLayers.Name);
            SerializedObject serialized = new SerializedObject(grab);
            SerializedProperty armProperty = serialized.FindProperty("_armPoint");
            SerializedProperty maskProperty = serialized.FindProperty("_interactableMask");
            SerializedProperty rangeProperty = serialized.FindProperty("_range");
            if (armProperty.objectReferenceValue != armPoint) {
                armProperty.objectReferenceValue = armPoint;
                dirty = true;
            }

            if (maskProperty.intValue != interactableBit) {
                maskProperty.intValue = interactableBit;
                dirty = true;
            }

            if (Mathf.Approximately(rangeProperty.floatValue, 4f) == false) {
                rangeProperty.floatValue = 4f;
                dirty = true;
            }

            if (dirty)
                serialized.ApplyModifiedPropertiesWithoutUndo();

            return dirty;
        }

        private static bool EnsurePlayerUseController(GameObject player) {
            GrabController grab = player.GetComponent<GrabController>();
            UseController use = player.GetComponent<UseController>();
            bool dirty = false;
            if (use == null) {
                use = player.AddComponent<UseController>();
                dirty = true;
            }

            int interactableBit = LayerMask.GetMask(InteractableLayers.Name);
            SerializedObject serialized = new SerializedObject(use);
            SerializedProperty grabProperty = serialized.FindProperty("_grab");
            SerializedProperty maskProperty = serialized.FindProperty("_interactableMask");
            SerializedProperty rangeProperty = serialized.FindProperty("_range");
            if (grabProperty.objectReferenceValue != grab) {
                grabProperty.objectReferenceValue = grab;
                dirty = true;
            }

            if (maskProperty.intValue != interactableBit) {
                maskProperty.intValue = interactableBit;
                dirty = true;
            }

            if (Mathf.Approximately(rangeProperty.floatValue, 4f) == false) {
                rangeProperty.floatValue = 4f;
                dirty = true;
            }

            if (dirty)
                serialized.ApplyModifiedPropertiesWithoutUndo();

            return dirty;
        }

        private static Transform FindNamedChild(Transform parent, string childName) {
            if (parent.name == childName)
                return parent;

            for (int i = 0; i < parent.childCount; i++) {
                Transform found = FindNamedChild(parent.GetChild(i), childName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static GameObject EnsureDummyGrabbablePrefab() {
            EnsureFolder(GrabPrefabsRoot);
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(DummyGrabbablePrefabPath);
            if (existing != null)
                return existing;

            GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name = "DummyGrabbable";
            item.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            int interactableLayer = LayerMask.NameToLayer(InteractableLayers.Name);
            if (interactableLayer >= 0)
                item.layer = interactableLayer;

            Rigidbody rigidbody = item.AddComponent<Rigidbody>();
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rigidbody.mass = 1f;

            item.AddComponent<NetworkIdentity>();
            NetworkRigidbodyUnreliable networkBody = item.AddComponent<NetworkRigidbodyUnreliable>();
            networkBody.syncDirection = SyncDirection.ServerToClient;
            networkBody.target = item.transform;

            Outline outline = item.AddComponent<Outline>();
            outline.enabled = false;
            outline.OutlineMode = Outline.Mode.OutlineVisible;
            outline.OutlineColor = Color.yellow;
            outline.OutlineWidth = 4f;

            Grabbable grabbable = item.AddComponent<Grabbable>();
            SerializedObject serialized = new SerializedObject(grabbable);
            serialized.FindProperty("_rb").objectReferenceValue = rigidbody;
            serialized.FindProperty("_networkBody").objectReferenceValue = networkBody;
            serialized.FindProperty("_outline").objectReferenceValue = outline;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(item, DummyGrabbablePrefabPath);
            Object.DestroyImmediate(item);
            return AssetDatabase.LoadAssetAtPath<GameObject>(DummyGrabbablePrefabPath);
        }

        private static void RegisterSpawnPrefab(GameObject prefab) {
            if (prefab == null)
                return;

            string scenePath = ScenesRoot + "/" + SceneNames.Global + ".unity";
            if (File.Exists(ToAbsolute(scenePath)) == false)
                return;

            Scene scene = OpenSceneIfNeeded(scenePath, out bool openedAdditive);
            try {
                ConnectionNetworkManager networkManager = FindInScene<ConnectionNetworkManager>(scene);
                if (networkManager == null)
                    return;

                SerializedObject serialized = new SerializedObject(networkManager);
                SerializedProperty list = serialized.FindProperty("spawnPrefabs");
                for (int i = 0; i < list.arraySize; i++) {
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue == prefab)
                        return;
                }

                list.arraySize += 1;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = prefab;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.SaveScene(scene);
            }
            finally {
                if (openedAdditive)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void EnsureShipAssets() {
            EnsureFolders();
            EnsureInteractableLayer();
            GameObject engineView = EnsureEngineItemViewPrefab();
            GameObject largeEngineView = EnsureLargeEngineItemViewPrefab();
            GameObject propellerView = EnsurePropellerItemViewPrefab();
            GameObject helmView = EnsureHelmItemViewPrefab();
            EnsureItemViewCatalog(engineView, largeEngineView, propellerView, helmView);
            EnsureEngineCatalog();
            EnsureFlightConfig();
            GameObject enginePrefab = EnsureRocketEngineItemPrefab(engineView);
            GameObject largeEnginePrefab = EnsureLargeEngineItemPrefab(largeEngineView);
            GameObject propellerPrefab = EnsurePropellerItemPrefab(propellerView);
            GameObject helmPrefab = EnsureHelmItemPrefab(helmView);
            RegisterSpawnPrefab(enginePrefab);
            RegisterSpawnPrefab(largeEnginePrefab);
            RegisterSpawnPrefab(propellerPrefab);
            RegisterSpawnPrefab(helmPrefab);
            EnsureShipPlatformPrefab();
            GameObject padPrefab = EnsureStationPadPrefab();
            EnsureWreckPrefab();
            EnsureShipRunConfig();
            GameObject rockPrefab = EnsureCruiseRockPrefab();
            EnsureStationCatalog();
            RegisterSpawnPrefab(padPrefab);
            RegisterSpawnPrefab(rockPrefab);
        }

        private static GameObject EnsureEngineItemViewPrefab() {
            EnsureFolder(ShipPrefabsRoot);
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(EngineItemViewPrefabPath);
            if (existing != null)
                return existing;

            GameObject view = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            view.name = "EngineItemView";
            view.transform.localScale = new Vector3(0.28f, 0.38f, 0.28f);
            Object.DestroyImmediate(view.GetComponent<Collider>());
            PrefabUtility.SaveAsPrefabAsset(view, EngineItemViewPrefabPath);
            Object.DestroyImmediate(view);
            return AssetDatabase.LoadAssetAtPath<GameObject>(EngineItemViewPrefabPath);
        }

        private static GameObject EnsureLargeEngineItemViewPrefab() {
            EnsureFolder(ShipPrefabsRoot);
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(LargeEngineItemViewPrefabPath);
            if (existing != null)
                return existing;

            GameObject view = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            view.name = "LargeEngineItemView";
            view.transform.localScale = new Vector3(0.52f, 0.72f, 0.52f);
            Object.DestroyImmediate(view.GetComponent<Collider>());
            PrefabUtility.SaveAsPrefabAsset(view, LargeEngineItemViewPrefabPath);
            Object.DestroyImmediate(view);
            return AssetDatabase.LoadAssetAtPath<GameObject>(LargeEngineItemViewPrefabPath);
        }

        private static GameObject EnsurePropellerItemViewPrefab() {
            EnsureFolder(ShipPrefabsRoot);
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PropellerItemViewPrefabPath);
            if (existing != null)
                return existing;

            var view = new GameObject("PropellerItemView");
            GameObject hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hub.name = "Hub";
            hub.transform.SetParent(view.transform, false);
            hub.transform.localScale = new Vector3(0.16f, 0.07f, 0.16f);
            Object.DestroyImmediate(hub.GetComponent<Collider>());

            GameObject bladeA = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bladeA.name = "BladeA";
            bladeA.transform.SetParent(view.transform, false);
            bladeA.transform.localScale = new Vector3(1.15f, 0.035f, 0.16f);
            Object.DestroyImmediate(bladeA.GetComponent<Collider>());

            GameObject bladeB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bladeB.name = "BladeB";
            bladeB.transform.SetParent(view.transform, false);
            bladeB.transform.localScale = new Vector3(1.15f, 0.035f, 0.16f);
            bladeB.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            Object.DestroyImmediate(bladeB.GetComponent<Collider>());

            PrefabUtility.SaveAsPrefabAsset(view, PropellerItemViewPrefabPath);
            Object.DestroyImmediate(view);
            return AssetDatabase.LoadAssetAtPath<GameObject>(PropellerItemViewPrefabPath);
        }

        private static void EnsureItemViewCatalog(
            GameObject engineView,
            GameObject largeEngineView,
            GameObject propellerView,
            GameObject helmView) {
            EnsureFolder(ShipResourcesRoot);
            ItemViewCatalog catalog = AssetDatabase.LoadAssetAtPath<ItemViewCatalog>(ItemViewCatalogPath);
            if (catalog == null) {
                catalog = ScriptableObject.CreateInstance<ItemViewCatalog>();
                AssetDatabase.CreateAsset(catalog, ItemViewCatalogPath);
            }

            SerializedObject serialized = new SerializedObject(catalog);
            SerializedProperty views = serialized.FindProperty("_views");
            views.arraySize = 4;
            WriteCatalogEntry(views.GetArrayElementAtIndex(0), ItemViewId.Engine, engineView);
            WriteCatalogEntry(views.GetArrayElementAtIndex(1), ItemViewId.LargeEngine, largeEngineView);
            WriteCatalogEntry(views.GetArrayElementAtIndex(2), ItemViewId.Propeller, propellerView);
            WriteCatalogEntry(views.GetArrayElementAtIndex(3), ItemViewId.Helm, helmView);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static EngineCatalog EnsureEngineCatalog() {
            EnsureFolder(ShipResourcesRoot);
            EngineCatalog catalog = AssetDatabase.LoadAssetAtPath<EngineCatalog>(EngineCatalogPath);
            if (catalog == null) {
                catalog = ScriptableObject.CreateInstance<EngineCatalog>();
                AssetDatabase.CreateAsset(catalog, EngineCatalogPath);
            }

            SerializedObject serialized = new SerializedObject(catalog);
            SerializedProperty engines = serialized.FindProperty("_engines");
            engines.arraySize = 3;
            WriteEngineEntry(engines.GetArrayElementAtIndex(0), ItemViewId.Engine, 3f);
            WriteEngineEntry(engines.GetArrayElementAtIndex(1), ItemViewId.LargeEngine, 6f);
            WriteEngineEntry(engines.GetArrayElementAtIndex(2), ItemViewId.Propeller, 2f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static ShipFlightSettings EnsureFlightConfig() {
            EnsureFolder(ShipConfigurationsRoot);
            ShipFlightSettings settings = AssetDatabase.LoadAssetAtPath<ShipFlightSettings>(ShipFlightConfigPath);
            if (settings == null) {
                settings = ScriptableObject.CreateInstance<ShipFlightSettings>();
                AssetDatabase.CreateAsset(settings, ShipFlightConfigPath);
                EditorUtility.SetDirty(settings);
            }

            MarkAddressable(
                ShipFlightConfigPath,
                Address.Configurations.ShipFlightConfig_Default,
                ConfigurationsAddressableGroup);
            return settings;
        }

        private static ShipRunConfig EnsureShipRunConfig() {
            EnsureFolder(ShipConfigurationsRoot);
            ShipRunConfig config = AssetDatabase.LoadAssetAtPath<ShipRunConfig>(ShipRunConfigPath);
            if (config == null) {
                config = ScriptableObject.CreateInstance<ShipRunConfig>();
                AssetDatabase.CreateAsset(config, ShipRunConfigPath);
                EditorUtility.SetDirty(config);
            }

            MarkAddressable(
                ShipRunConfigPath,
                Address.Configurations.ShipRunConfig_Default,
                ConfigurationsAddressableGroup);
            return config;
        }

        private static GameObject EnsureStationPadPrefab() {
            EnsureFolder(ShipPrefabsRoot);
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(StationPadPrefabPath);
            if (existing != null) {
                UpgradeStationPadPrefab(existing);
                return existing;
            }

            var root = new GameObject("StationPad");
            root.AddComponent<NetworkIdentity>();

            GameObject deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.name = "Deck";
            deck.transform.SetParent(root.transform, false);
            deck.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            deck.transform.localScale = new Vector3(16f, 0.4f, 24f);
            ApplyGroundLayer(deck.transform);

            Transform landing = EnsureChild(root.transform, "LandingPoint", new Vector3(0f, 0.45f, -9f));
            Transform berth = EnsureChild(root.transform, "BuildBerth", new Vector3(0f, 0.45f, 9f));
            Transform spawn = EnsureChild(root.transform, "PlayerSpawn", new Vector3(0f, 1.2f, 11f));
            ApplyGroundLayer(landing);
            ApplyGroundLayer(berth);

            ShipLandingPad pad = root.AddComponent<ShipLandingPad>();
            SerializedObject serialized = new SerializedObject(pad);
            serialized.FindProperty("_landingPoint").objectReferenceValue = landing;
            serialized.FindProperty("_buildBerth").objectReferenceValue = berth;
            serialized.FindProperty("_playerSpawn").objectReferenceValue = spawn;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, StationPadPrefabPath);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(StationPadPrefabPath);
        }

        private static void UpgradeStationPadPrefab(GameObject prefabAsset) {
            string path = AssetDatabase.GetAssetPath(prefabAsset);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try {
                bool dirty = false;
                Transform deck = root.transform.Find("Deck");
                if (deck != null && deck.localScale != new Vector3(16f, 0.4f, 24f)) {
                    deck.localScale = new Vector3(16f, 0.4f, 24f);
                    dirty = true;
                }

                Transform landing = EnsureChild(root.transform, "LandingPoint", new Vector3(0f, 0.45f, -9f));
                Transform berth = EnsureChild(root.transform, "BuildBerth", new Vector3(0f, 0.45f, 9f));
                Transform spawn = EnsureChild(root.transform, "PlayerSpawn", new Vector3(0f, 1.2f, 11f));
                if (landing.localPosition != new Vector3(0f, 0.45f, -9f) ||
                    berth.localPosition != new Vector3(0f, 0.45f, 9f) ||
                    spawn.localPosition != new Vector3(0f, 1.2f, 11f))
                    dirty = true;

                ShipLandingPad pad = root.GetComponent<ShipLandingPad>();
                if (pad != null) {
                    SerializedObject serialized = new SerializedObject(pad);
                    SerializedProperty landingProp = serialized.FindProperty("_landingPoint");
                    SerializedProperty berthProp = serialized.FindProperty("_buildBerth");
                    SerializedProperty spawnProp = serialized.FindProperty("_playerSpawn");
                    if (landingProp.objectReferenceValue != landing ||
                        berthProp.objectReferenceValue != berth ||
                        spawnProp.objectReferenceValue != spawn) {
                        landingProp.objectReferenceValue = landing;
                        berthProp.objectReferenceValue = berth;
                        spawnProp.objectReferenceValue = spawn;
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                        dirty = true;
                    }
                }

                if (dirty)
                    PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static GameObject EnsureWreckPrefab() {
            EnsureFolder(ShipPrefabsRoot);
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ShipWreckPrefabPath);
            if (existing != null)
                return existing;

            GameObject wreck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wreck.name = "ShipWreck";
            wreck.transform.localScale = new Vector3(4f, 0.35f, 6.5f);
            ApplyGroundLayer(wreck.transform);

            PrefabUtility.SaveAsPrefabAsset(wreck, ShipWreckPrefabPath);
            Object.DestroyImmediate(wreck);
            return AssetDatabase.LoadAssetAtPath<GameObject>(ShipWreckPrefabPath);
        }

        private static GameObject EnsureCruiseRockPrefab() {
            EnsureFolder(ShipPrefabsRoot);
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(CruiseRockPrefabPath);
            if (existing != null) {
                UpgradeCruiseRockPrefab(existing);
                return existing;
            }

            var root = new GameObject("CruiseRock");
            root.AddComponent<NetworkIdentity>();
            AssignRockNetworkTransform(root);
            root.AddComponent<CruiseRock>();
            AddRockChunk(root.transform, "ChunkA", new Vector3(0f, 0f, 0f), new Vector3(2.4f, 1.8f, 2.1f));
            AddRockChunk(root.transform, "ChunkB", new Vector3(0.9f, 0.4f, -0.6f), new Vector3(1.4f, 1.1f, 1.6f));
            AddRockChunk(root.transform, "ChunkC", new Vector3(-0.7f, -0.3f, 0.8f), new Vector3(1.2f, 1.5f, 1.1f));
            PrefabUtility.SaveAsPrefabAsset(root, CruiseRockPrefabPath);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(CruiseRockPrefabPath);
        }

        private static void UpgradeCruiseRockPrefab(GameObject prefabAsset) {
            string path = AssetDatabase.GetAssetPath(prefabAsset);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try {
                bool dirty = false;
                NetworkRigidbodyUnreliable body = root.GetComponent<NetworkRigidbodyUnreliable>();
                if (body != null) {
                    Object.DestroyImmediate(body);
                    dirty = true;
                }

                if (root.GetComponent<NetworkTransformUnreliable>() == null) {
                    AssignRockNetworkTransform(root);
                    dirty = true;
                }

                if (root.GetComponent<CruiseRock>() == null) {
                    root.AddComponent<CruiseRock>();
                    dirty = true;
                }

                if (dirty)
                    PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AssignRockNetworkTransform(GameObject root) {
            NetworkTransformUnreliable networkTransform = root.GetComponent<NetworkTransformUnreliable>();
            if (networkTransform == null)
                networkTransform = root.AddComponent<NetworkTransformUnreliable>();

            networkTransform.syncDirection = SyncDirection.ServerToClient;
            SerializedObject serialized = new SerializedObject(networkTransform);
            serialized.FindProperty("target").objectReferenceValue = root.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddRockChunk(Transform parent, string chunkName, Vector3 localPosition, Vector3 localScale) {
            GameObject chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chunk.name = chunkName;
            chunk.transform.SetParent(parent, false);
            chunk.transform.localPosition = localPosition;
            chunk.transform.localScale = localScale;
            Collider collider = chunk.GetComponent<Collider>();
            if (collider != null)
                Object.DestroyImmediate(collider);
        }

        private static ShipStationCatalog EnsureStationCatalog() {
            EnsureFolder(ShipResourcesRoot);
            ShipStationCatalog catalog = AssetDatabase.LoadAssetAtPath<ShipStationCatalog>(ShipStationCatalogPath);
            if (catalog == null) {
                catalog = ScriptableObject.CreateInstance<ShipStationCatalog>();
                AssetDatabase.CreateAsset(catalog, ShipStationCatalogPath);
            }

            GameObject pad = EnsureStationPadPrefab();
            GameObject wreck = EnsureWreckPrefab();
            GameObject rock = EnsureCruiseRockPrefab();
            GameObject engine = AssetDatabase.LoadAssetAtPath<GameObject>(RocketEngineItemPrefabPath);
            GameObject largeEngine = AssetDatabase.LoadAssetAtPath<GameObject>(LargeEngineItemPrefabPath);
            GameObject propeller = AssetDatabase.LoadAssetAtPath<GameObject>(PropellerItemPrefabPath);
            GameObject helm = AssetDatabase.LoadAssetAtPath<GameObject>(HelmItemPrefabPath);

            SerializedObject serialized = new SerializedObject(catalog);
            serialized.FindProperty("_padPrefab").objectReferenceValue = pad;
            serialized.FindProperty("_wreckPrefab").objectReferenceValue = wreck;
            serialized.FindProperty("_rockPrefab").objectReferenceValue = rock;
            SerializedProperty drops = serialized.FindProperty("_drops");
            drops.arraySize = 5;
            WriteDropEntry(drops.GetArrayElementAtIndex(0), engine, 0, 2);
            WriteDropEntry(drops.GetArrayElementAtIndex(1), largeEngine, 0, 1);
            WriteDropEntry(drops.GetArrayElementAtIndex(2), propeller, 0, 1);
            WriteDropEntry(drops.GetArrayElementAtIndex(3), helm, 0, 1);
            WriteDropEntry(drops.GetArrayElementAtIndex(4), engine, 1, 2);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void WriteDropEntry(SerializedProperty entry, GameObject prefab, int minLoop, int count) {
            entry.FindPropertyRelative("_prefab").objectReferenceValue = prefab;
            entry.FindPropertyRelative("_minLoop").intValue = minLoop;
            entry.FindPropertyRelative("_count").intValue = count;
        }

        private static void WriteEngineEntry(SerializedProperty entry, ItemViewId view, float thrust) {
            entry.FindPropertyRelative("_view").enumValueIndex = (int)view;
            entry.FindPropertyRelative("_thrust").floatValue = thrust;
        }

        private static void WriteCatalogEntry(SerializedProperty entry, ItemViewId id, GameObject prefab) {
            entry.FindPropertyRelative("_id").enumValueIndex = (int)id;
            entry.FindPropertyRelative("_prefab").objectReferenceValue = prefab;
        }

        private static GameObject EnsureRocketEngineItemPrefab(GameObject engineView) {
            EnsureFolder(ShipPrefabsRoot);
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(RocketEngineItemPrefabPath);
            if (existing != null) {
                UpgradeRocketEngineItemPrefab(existing, engineView);
                return existing;
            }

            GameObject item = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            item.name = "RocketEngineItem";
            item.transform.localScale = Vector3.one;
            int interactableLayer = LayerMask.NameToLayer(InteractableLayers.Name);
            if (interactableLayer >= 0)
                item.layer = interactableLayer;

            Rigidbody rigidbody = item.AddComponent<Rigidbody>();
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rigidbody.mass = 1f;

            item.AddComponent<NetworkIdentity>();
            NetworkRigidbodyUnreliable networkBody = item.AddComponent<NetworkRigidbodyUnreliable>();
            networkBody.syncDirection = SyncDirection.ServerToClient;
            networkBody.target = item.transform;

            Outline outline = item.AddComponent<Outline>();
            outline.enabled = false;
            outline.OutlineMode = Outline.Mode.OutlineVisible;
            outline.OutlineColor = Color.yellow;
            outline.OutlineWidth = 4f;

            Grabbable grabbable = item.AddComponent<Grabbable>();
            SerializedObject grabbableSerialized = new SerializedObject(grabbable);
            grabbableSerialized.FindProperty("_rb").objectReferenceValue = rigidbody;
            grabbableSerialized.FindProperty("_networkBody").objectReferenceValue = networkBody;
            grabbableSerialized.FindProperty("_outline").objectReferenceValue = outline;
            grabbableSerialized.ApplyModifiedPropertiesWithoutUndo();

            ShipItem shipItem = item.AddComponent<ShipItem>();
            SerializedObject itemSerialized = new SerializedObject(shipItem);
            itemSerialized.FindProperty("_type").enumValueIndex = (int)ShipModuleType.Engine;
            itemSerialized.FindProperty("_view").enumValueIndex = (int)ItemViewId.Engine;
            itemSerialized.ApplyModifiedPropertiesWithoutUndo();

            AttachCatalogView(item.transform, engineView, "EngineItemView");
            PrefabUtility.SaveAsPrefabAsset(item, RocketEngineItemPrefabPath);
            Object.DestroyImmediate(item);
            return AssetDatabase.LoadAssetAtPath<GameObject>(RocketEngineItemPrefabPath);
        }

        private static GameObject EnsureLargeEngineItemPrefab(GameObject largeEngineView) {
            EnsureFolder(ShipPrefabsRoot);
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(LargeEngineItemPrefabPath);
            if (existing != null) {
                UpgradeNamedItemPrefab(existing, largeEngineView, "LargeEngineItemView", ItemViewId.LargeEngine);
                return existing;
            }

            return CreateGrabbableShipItem(
                "LargeEngineItem",
                LargeEngineItemPrefabPath,
                largeEngineView,
                "LargeEngineItemView",
                ItemViewId.LargeEngine,
                2f);
        }

        private static GameObject EnsureHelmItemViewPrefab() {
            EnsureFolder(ShipPrefabsRoot);
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(HelmItemViewPrefabPath);
            if (existing != null)
                return existing;

            GameObject view = GameObject.CreatePrimitive(PrimitiveType.Cube);
            view.name = "HelmItemView";
            view.transform.localScale = new Vector3(0.55f, 0.7f, 0.55f);
            Object.DestroyImmediate(view.GetComponent<Collider>());
            Transform sit = EnsureChild(view.transform, "SitPoint", new Vector3(0f, 0.55f, 0.05f));
            ShipSeat seat = view.AddComponent<ShipSeat>();
            SerializedObject seatSerialized = new SerializedObject(seat);
            seatSerialized.FindProperty("_role").enumValueIndex = (int)ShipSeatRole.Helm;
            seatSerialized.FindProperty("_sitPoint").objectReferenceValue = sit;
            seatSerialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(view, HelmItemViewPrefabPath);
            Object.DestroyImmediate(view);
            return AssetDatabase.LoadAssetAtPath<GameObject>(HelmItemViewPrefabPath);
        }

        private static GameObject EnsureHelmItemPrefab(GameObject helmView) {
            EnsureFolder(ShipPrefabsRoot);
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(HelmItemPrefabPath);
            if (existing != null) {
                UpgradeNamedItemPrefab(existing, helmView, "HelmItemView", ItemViewId.Helm, ShipModuleType.Control);
                return existing;
            }

            return CreateGrabbableShipItem(
                "HelmItem",
                HelmItemPrefabPath,
                helmView,
                "HelmItemView",
                ItemViewId.Helm,
                1.2f,
                ShipModuleType.Control);
        }

        private static GameObject EnsurePropellerItemPrefab(GameObject propellerView) {
            EnsureFolder(ShipPrefabsRoot);
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PropellerItemPrefabPath);
            if (existing != null) {
                UpgradeNamedItemPrefab(existing, propellerView, "PropellerItemView", ItemViewId.Propeller);
                return existing;
            }

            return CreateGrabbableShipItem(
                "PropellerItem",
                PropellerItemPrefabPath,
                propellerView,
                "PropellerItemView",
                ItemViewId.Propeller,
                1f);
        }

        private static GameObject CreateGrabbableShipItem(
            string itemName,
            string prefabPath,
            GameObject viewPrefab,
            string viewChildName,
            ItemViewId viewId,
            float mass,
            ShipModuleType moduleType = ShipModuleType.Engine) {
            GameObject item = new GameObject(itemName);
            int interactableLayer = LayerMask.NameToLayer(InteractableLayers.Name);
            if (interactableLayer >= 0)
                item.layer = interactableLayer;

            Rigidbody rigidbody = item.AddComponent<Rigidbody>();
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rigidbody.mass = mass;

            if (item.GetComponent<Collider>() == null) {
                BoxCollider box = item.AddComponent<BoxCollider>();
                box.size = new Vector3(0.55f, 0.7f, 0.55f);
                box.center = new Vector3(0f, 0.35f, 0f);
            }

            item.AddComponent<NetworkIdentity>();
            NetworkRigidbodyUnreliable networkBody = item.AddComponent<NetworkRigidbodyUnreliable>();
            networkBody.syncDirection = SyncDirection.ServerToClient;
            networkBody.target = item.transform;

            Outline outline = item.AddComponent<Outline>();
            outline.enabled = false;
            outline.OutlineMode = Outline.Mode.OutlineVisible;
            outline.OutlineColor = Color.yellow;
            outline.OutlineWidth = 4f;

            Grabbable grabbable = item.AddComponent<Grabbable>();
            SerializedObject grabbableSerialized = new SerializedObject(grabbable);
            grabbableSerialized.FindProperty("_rb").objectReferenceValue = rigidbody;
            grabbableSerialized.FindProperty("_networkBody").objectReferenceValue = networkBody;
            grabbableSerialized.FindProperty("_outline").objectReferenceValue = outline;
            grabbableSerialized.ApplyModifiedPropertiesWithoutUndo();

            ShipItem shipItem = item.AddComponent<ShipItem>();
            SerializedObject itemSerialized = new SerializedObject(shipItem);
            itemSerialized.FindProperty("_type").enumValueIndex = (int)moduleType;
            itemSerialized.FindProperty("_view").enumValueIndex = (int)viewId;
            itemSerialized.ApplyModifiedPropertiesWithoutUndo();

            AttachCatalogView(item.transform, viewPrefab, viewChildName);
            PrefabUtility.SaveAsPrefabAsset(item, prefabPath);
            Object.DestroyImmediate(item);
            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        private static void UpgradeNamedItemPrefab(
            GameObject prefabAsset,
            GameObject viewPrefab,
            string viewChildName,
            ItemViewId viewId,
            ShipModuleType moduleType = ShipModuleType.Engine) {
            string path = AssetDatabase.GetAssetPath(prefabAsset);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try {
                bool dirty = AttachCatalogView(root.transform, viewPrefab, viewChildName);
                ShipItem shipItem = root.GetComponent<ShipItem>();
                if (shipItem == null) {
                    shipItem = root.AddComponent<ShipItem>();
                    dirty = true;
                }

                SerializedObject itemSerialized = new SerializedObject(shipItem);
                SerializedProperty viewProperty = itemSerialized.FindProperty("_view");
                SerializedProperty typeProperty = itemSerialized.FindProperty("_type");
                if (viewProperty != null && viewProperty.enumValueIndex != (int)viewId) {
                    viewProperty.enumValueIndex = (int)viewId;
                    dirty = true;
                }

                if (typeProperty != null && typeProperty.enumValueIndex != (int)moduleType) {
                    typeProperty.enumValueIndex = (int)moduleType;
                    dirty = true;
                }

                itemSerialized.ApplyModifiedPropertiesWithoutUndo();

                if (root.GetComponent<Collider>() == null) {
                    BoxCollider box = root.AddComponent<BoxCollider>();
                    box.size = new Vector3(0.55f, 0.7f, 0.55f);
                    box.center = new Vector3(0f, 0.35f, 0f);
                    dirty = true;
                }

                if (dirty)
                    PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void UpgradeRocketEngineItemPrefab(GameObject prefabAsset, GameObject engineView) {
            string path = AssetDatabase.GetAssetPath(prefabAsset);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try {
                bool dirty = AttachCatalogView(root.transform, engineView, "EngineItemView");
                ShipItem shipItem = root.GetComponent<ShipItem>();
                if (shipItem == null) {
                    shipItem = root.AddComponent<ShipItem>();
                    dirty = true;
                }

                SerializedObject itemSerialized = new SerializedObject(shipItem);
                SerializedProperty viewProperty = itemSerialized.FindProperty("_view");
                if (viewProperty != null && viewProperty.enumValueIndex != (int)ItemViewId.Engine) {
                    viewProperty.enumValueIndex = (int)ItemViewId.Engine;
                    itemSerialized.ApplyModifiedPropertiesWithoutUndo();
                    dirty = true;
                }
                else {
                    itemSerialized.ApplyModifiedPropertiesWithoutUndo();
                }

                if (dirty)
                    PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool AttachCatalogView(Transform itemRoot, GameObject viewPrefab, string viewChildName) {
            if (itemRoot == null || viewPrefab == null)
                return false;

            Transform existing = itemRoot.Find(viewChildName);
            if (existing != null)
                return false;

            GameObject view = (GameObject)PrefabUtility.InstantiatePrefab(viewPrefab, itemRoot);
            view.name = viewChildName;
            view.transform.localPosition = Vector3.zero;
            view.transform.localRotation = Quaternion.identity;
            return true;
        }

        private static GameObject EnsureShipPlatformPrefab() {
            EnsureFolder(ShipPrefabsRoot);
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ShipPlatformPrefabPath);
            if (existing != null) {
                UpgradeShipPlatformPrefab(existing);
                return existing;
            }

            int interactableLayer = LayerMask.NameToLayer(InteractableLayers.Name);

            var root = new GameObject("ShipPlatform");
            ShipBase ship = root.AddComponent<ShipBase>();

            GameObject deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.name = "Deck";
            deck.transform.SetParent(root.transform, false);
            deck.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            deck.transform.localScale = new Vector3(4f, 0.3f, 3f);

            ShipSocket leftSocket = CreateShipSocket(
                root.transform,
                "EngineSocketLeft",
                new Vector3(-1.7f, 0.15f, 0f),
                interactableLayer,
                ShipModuleType.Engine,
                true);
            ShipSocket rightSocket = CreateShipSocket(
                root.transform,
                "EngineSocketRight",
                new Vector3(1.7f, 0.15f, 0f),
                interactableLayer,
                ShipModuleType.Engine,
                true);
            ShipSocket controlSocket = CreateShipSocket(
                root.transform,
                "ControlSocket",
                new Vector3(0f, 0.15f, 0.85f),
                interactableLayer,
                ShipModuleType.Control,
                false);
            ShipLaunchLever lever = CreateLaunchLever(root.transform, ship, interactableLayer);
            EnsureShipRideVolume(root, ship);
            ApplyGroundLayer(root.transform.Find("Deck"));
            Rigidbody body = EnsureShipBody(root);
            EnsureShipPoseSync(root, ship, out ShipPoseSync poseSync);

            SerializedObject shipSerialized = new SerializedObject(ship);
            SerializedProperty socketsProperty = shipSerialized.FindProperty("_sockets");
            socketsProperty.arraySize = 3;
            socketsProperty.GetArrayElementAtIndex(0).objectReferenceValue = leftSocket;
            socketsProperty.GetArrayElementAtIndex(1).objectReferenceValue = rightSocket;
            socketsProperty.GetArrayElementAtIndex(2).objectReferenceValue = controlSocket;
            shipSerialized.FindProperty("_lever").objectReferenceValue = lever;
            shipSerialized.FindProperty("_body").objectReferenceValue = body;
            shipSerialized.FindProperty("_engines").objectReferenceValue = EnsureEngineCatalog();
            BoxCollider deckCollider = deck.GetComponent<BoxCollider>();
            shipSerialized.FindProperty("_deck").objectReferenceValue = deckCollider;
            shipSerialized.FindProperty("_poseSync").objectReferenceValue = poseSync;
            shipSerialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, ShipPlatformPrefabPath);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(ShipPlatformPrefabPath);
        }

        private static ShipSocket CreateShipSocket(
            Transform parent,
            string socketName,
            Vector3 localPosition,
            int interactableLayer,
            ShipModuleType acceptedType,
            bool requiredForLaunch) {
            GameObject socketObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            socketObject.name = socketName;
            socketObject.transform.SetParent(parent, false);
            socketObject.transform.localPosition = localPosition;
            socketObject.transform.localScale = new Vector3(0.7f, 0.2f, 0.7f);
            if (interactableLayer >= 0)
                socketObject.layer = interactableLayer;

            socketObject.AddComponent<NetworkIdentity>();
            BoxCollider socketCollider = socketObject.GetComponent<BoxCollider>();
            if (socketCollider != null)
                socketCollider.isTrigger = true;

            Outline outline = socketObject.AddComponent<Outline>();
            outline.enabled = false;
            outline.OutlineMode = Outline.Mode.OutlineVisible;
            outline.OutlineColor = new Color(0.3f, 0.85f, 1f, 1f);
            outline.OutlineWidth = 5f;

            Transform installPoint = EnsureChild(socketObject.transform, "InstallPoint", new Vector3(0f, -2.2f, 0f));
            ShipSocket socket = socketObject.AddComponent<ShipSocket>();
            AssignSocketInstallPoints(socket, outline, installPoint, acceptedType, requiredForLaunch);
            EnsureSocketInteractables(socket, parent.GetComponent<ShipBase>());
            return socket;
        }

        private static void UpgradeShipPlatformPrefab(GameObject prefabAsset) {
            string path = AssetDatabase.GetAssetPath(prefabAsset);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try {
                bool dirty = false;
                ShipBase ship = root.GetComponent<ShipBase>();
                ShipSocket[] sockets = root.GetComponentsInChildren<ShipSocket>(true);
                for (int i = 0; i < sockets.Length; i++) {
                    if (UpgradeShipSocket(sockets[i]))
                        dirty = true;
                    if (EnsureSocketInteractables(sockets[i], ship))
                        dirty = true;
                }

                if (ship != null && EnsureShipRideVolume(root, ship))
                    dirty = true;

                if (ApplyGroundLayer(root.transform.Find("Deck")))
                    dirty = true;

                if (ship != null && AssignShipBody(ship, EnsureShipBody(root)))
                    dirty = true;

                int interactableLayer = LayerMask.NameToLayer(InteractableLayers.Name);
                if (RemoveExtraEngineSockets(root))
                    dirty = true;

                if (EnsureEngineSocket(root, "EngineSocketLeft", new Vector3(-1.7f, 0.15f, 0f), true, interactableLayer))
                    dirty = true;
                if (EnsureEngineSocket(root, "EngineSocketRight", new Vector3(1.7f, 0.15f, 0f), true, interactableLayer))
                    dirty = true;

                if (EnsureControlSocket(root, interactableLayer))
                    dirty = true;

                if (MakeInteractablesTriggers(root))
                    dirty = true;

                if (EnsureLeverInteractable(root.GetComponentInChildren<ShipLaunchLever>(true)))
                    dirty = true;

                if (ship != null && EnsureShipPoseSync(root, ship, out _))
                    dirty = true;

                if (ship != null && AssignShipSocketsAndSettings(ship, root))
                    dirty = true;

                if (dirty)
                    PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool MakeInteractablesTriggers(GameObject root) {
            bool dirty = false;
            ShipSocket[] sockets = root.GetComponentsInChildren<ShipSocket>(true);
            for (int i = 0; i < sockets.Length; i++) {
                if (MakeColliderTrigger(sockets[i].GetComponent<Collider>()))
                    dirty = true;
            }

            ShipLaunchLever lever = root.GetComponentInChildren<ShipLaunchLever>(true);
            if (lever != null && MakeColliderTrigger(lever.GetComponent<Collider>()))
                dirty = true;

            return dirty;
        }

        private static bool MakeColliderTrigger(Collider collider) {
            if (collider == null || collider.isTrigger)
                return false;

            collider.isTrigger = true;
            return true;
        }

        private static bool EnsureShipRideVolume(GameObject root, ShipBase ship) {
            Transform existing = root.transform.Find("RideVolume");
            bool dirty = false;
            GameObject volumeObject;
            if (existing == null) {
                volumeObject = new GameObject("RideVolume");
                volumeObject.transform.SetParent(root.transform, false);
                dirty = true;
            }
            else {
                volumeObject = existing.gameObject;
            }

            volumeObject.transform.localPosition = new Vector3(0f, 1.45f, -1.4f);
            volumeObject.transform.localScale = Vector3.one;

            BoxCollider box = volumeObject.GetComponent<BoxCollider>();
            if (box == null) {
                box = volumeObject.AddComponent<BoxCollider>();
                dirty = true;
            }

            if (box.isTrigger == false || box.size != new Vector3(4.6f, 2.8f, 7.2f)) {
                box.isTrigger = true;
                box.size = new Vector3(4.6f, 2.8f, 7.2f);
                dirty = true;
            }

            ShipDeckRideVolume volume = volumeObject.GetComponent<ShipDeckRideVolume>();
            if (volume == null) {
                volume = volumeObject.AddComponent<ShipDeckRideVolume>();
                dirty = true;
            }

            SerializedObject serialized = new SerializedObject(volume);
            SerializedProperty shipProperty = serialized.FindProperty("_ship");
            if (shipProperty.objectReferenceValue != ship) {
                shipProperty.objectReferenceValue = ship;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                dirty = true;
            }

            SerializedObject shipSerialized = new SerializedObject(ship);
            SerializedProperty rideVolume = shipSerialized.FindProperty("_rideVolume");
            if (rideVolume.objectReferenceValue != box) {
                rideVolume.objectReferenceValue = box;
                shipSerialized.ApplyModifiedPropertiesWithoutUndo();
                dirty = true;
            }

            return dirty;
        }

        private static Rigidbody EnsureShipBody(GameObject root) {
            Rigidbody body = root.GetComponent<Rigidbody>();
            if (body == null)
                body = root.AddComponent<Rigidbody>();

            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.None;
            body.constraints = RigidbodyConstraints.None;
            return body;
        }

        private static bool AssignShipBody(ShipBase ship, Rigidbody body) {
            SerializedObject serialized = new SerializedObject(ship);
            SerializedProperty bodyProperty = serialized.FindProperty("_body");
            SerializedProperty enginesProperty = serialized.FindProperty("_engines");
            SerializedProperty deckProperty = serialized.FindProperty("_deck");
            EngineCatalog engines = EnsureEngineCatalog();
            Transform deck = ship.transform.Find("Deck");
            BoxCollider deckCollider = deck != null ? deck.GetComponent<BoxCollider>() : null;
            bool dirty = false;
            if (bodyProperty.objectReferenceValue != body) {
                bodyProperty.objectReferenceValue = body;
                dirty = true;
            }

            if (enginesProperty.objectReferenceValue != engines) {
                enginesProperty.objectReferenceValue = engines;
                dirty = true;
            }

            if (deckProperty != null && deckProperty.objectReferenceValue != deckCollider) {
                deckProperty.objectReferenceValue = deckCollider;
                dirty = true;
            }

            if (dirty)
                serialized.ApplyModifiedPropertiesWithoutUndo();

            return dirty;
        }

        private static bool RemoveExtraEngineSockets(GameObject root) {
            string[] extra = {
                "EngineSocketLeftMid",
                "EngineSocketRightMid",
                "EngineSocketLeftAft",
                "EngineSocketRightAft",
                "EngineSocketLeft (1)",
                "EngineSocketRight (1)"
            };

            bool dirty = false;
            for (int i = 0; i < extra.Length; i++) {
                Transform socket = root.transform.Find(extra[i]);
                if (socket == null)
                    continue;

                Object.DestroyImmediate(socket.gameObject);
                dirty = true;
            }

            return dirty;
        }

        private static bool EnsureEngineSocket(
            GameObject root,
            string socketName,
            Vector3 localPosition,
            bool requiredForLaunch,
            int interactableLayer) {
            Transform existing = root.transform.Find(socketName);
            if (existing == null) {
                CreateShipSocket(
                    root.transform,
                    socketName,
                    localPosition,
                    interactableLayer,
                    ShipModuleType.Engine,
                    requiredForLaunch);
                return true;
            }

            bool dirty = false;
            if (existing.localPosition != localPosition) {
                existing.localPosition = localPosition;
                dirty = true;
            }

            ShipSocket socket = existing.GetComponent<ShipSocket>();
            if (socket == null)
                return dirty;

            SerializedObject serialized = new SerializedObject(socket);
            SerializedProperty required = serialized.FindProperty("_requiredForLaunch");
            if (required.boolValue != requiredForLaunch) {
                required.boolValue = requiredForLaunch;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                dirty = true;
            }

            return dirty;
        }

        private static bool EnsureControlSocket(GameObject root, int interactableLayer) {
            Transform existing = root.transform.Find("ControlSocket");
            if (existing != null)
                return false;

            CreateShipSocket(
                root.transform,
                "ControlSocket",
                new Vector3(0f, 0.15f, 0.85f),
                interactableLayer,
                ShipModuleType.Control,
                false);
            return true;
        }

        private static bool EnsureShipPoseSync(GameObject root, ShipBase ship, out ShipPoseSync poseSync) {
            Transform existing = root.transform.Find("PoseSync");
            bool dirty = false;
            GameObject syncObject;
            if (existing == null) {
                syncObject = new GameObject("PoseSync");
                syncObject.transform.SetParent(root.transform, false);
                dirty = true;
            }
            else {
                syncObject = existing.gameObject;
            }

            if (syncObject.GetComponent<NetworkIdentity>() == null) {
                syncObject.AddComponent<NetworkIdentity>();
                dirty = true;
            }

            poseSync = syncObject.GetComponent<ShipPoseSync>();
            if (poseSync == null) {
                poseSync = syncObject.AddComponent<ShipPoseSync>();
                dirty = true;
            }

            SerializedObject serialized = new SerializedObject(poseSync);
            SerializedProperty shipProperty = serialized.FindProperty("_ship");
            if (shipProperty.objectReferenceValue != root.transform) {
                shipProperty.objectReferenceValue = root.transform;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                dirty = true;
            }

            SerializedObject shipSerialized = new SerializedObject(ship);
            SerializedProperty poseProperty = shipSerialized.FindProperty("_poseSync");
            if (poseProperty != null && poseProperty.objectReferenceValue != poseSync) {
                poseProperty.objectReferenceValue = poseSync;
                shipSerialized.ApplyModifiedPropertiesWithoutUndo();
                dirty = true;
            }

            return dirty;
        }

        private static bool AssignShipSocketsAndSettings(ShipBase ship, GameObject root) {
            ShipSocket[] sockets = root.GetComponentsInChildren<ShipSocket>(true);
            SerializedObject serialized = new SerializedObject(ship);
            SerializedProperty socketsProperty = serialized.FindProperty("_sockets");
            bool dirty = socketsProperty.arraySize != sockets.Length;
            socketsProperty.arraySize = sockets.Length;
            for (int i = 0; i < sockets.Length; i++) {
                if (socketsProperty.GetArrayElementAtIndex(i).objectReferenceValue == sockets[i])
                    continue;

                socketsProperty.GetArrayElementAtIndex(i).objectReferenceValue = sockets[i];
                dirty = true;
            }

            if (dirty)
                serialized.ApplyModifiedPropertiesWithoutUndo();

            return dirty;
        }

        private static bool ApplyGroundLayer(Transform target) {
            if (target == null)
                return false;

            int ground = LayerMask.NameToLayer("Ground");
            if (ground < 0 || target.gameObject.layer == ground)
                return false;

            target.gameObject.layer = ground;
            return true;
        }

        private static bool UpgradeShipSocket(ShipSocket socket) {
            Transform socketTransform = socket.transform;
            Transform bakedView = socketTransform.Find("InstallPoint/InstalledEngineView");
            if (bakedView == null)
                bakedView = socketTransform.Find("InstalledEngineView");

            bool dirty = false;
            if (bakedView != null) {
                Object.DestroyImmediate(bakedView.gameObject);
                dirty = true;
            }

            Transform installPoint = EnsureChild(socketTransform, "InstallPoint", new Vector3(0f, -2.2f, 0f));
            Outline outline = socket.GetComponent<Outline>();
            SerializedObject serialized = new SerializedObject(socket);
            if (outline != null)
                serialized.FindProperty("_outline").objectReferenceValue = outline;

            serialized.FindProperty("_defaultInstallPoint").objectReferenceValue = installPoint;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return dirty;
        }

        private static void AssignSocketInstallPoints(
            ShipSocket socket,
            Outline outline,
            Transform installPoint,
            ShipModuleType acceptedType,
            bool requiredForLaunch) {
            SerializedObject serialized = new SerializedObject(socket);
            serialized.FindProperty("_acceptedType").enumValueIndex = (int)acceptedType;
            serialized.FindProperty("_requiredForLaunch").boolValue = requiredForLaunch;
            if (outline != null)
                serialized.FindProperty("_outline").objectReferenceValue = outline;

            serialized.FindProperty("_defaultInstallPoint").objectReferenceValue = installPoint;
            SerializedProperty points = serialized.FindProperty("_viewInstallPoints");
            points.arraySize = 1;
            SerializedProperty entry = points.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("_view").enumValueIndex =
                acceptedType == ShipModuleType.Control ? (int)ItemViewId.Helm : (int)ItemViewId.Engine;
            entry.FindPropertyRelative("_point").objectReferenceValue = installPoint;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool EnsureSocketInteractables(ShipSocket socket, ShipBase ship) {
            if (socket == null)
                return false;

            bool dirty = false;
            Outline outline = socket.GetComponent<Outline>();
            ShipInstallInteractable install = socket.GetComponent<ShipInstallInteractable>();
            if (install == null) {
                install = socket.gameObject.AddComponent<ShipInstallInteractable>();
                dirty = true;
            }

            SerializedObject installSerialized = new SerializedObject(install);
            SerializedProperty installOutline = installSerialized.FindProperty("_outline");
            SerializedProperty installSocket = installSerialized.FindProperty("_socket");
            bool installDirty = false;
            if (installOutline.objectReferenceValue != outline) {
                installOutline.objectReferenceValue = outline;
                installDirty = true;
            }

            if (installSocket.objectReferenceValue != socket) {
                installSocket.objectReferenceValue = socket;
                installDirty = true;
            }

            if (installDirty)
                installSerialized.ApplyModifiedPropertiesWithoutUndo();

            bool wantsSit = CanHostSeat(socket.AcceptedType);
            ShipSitInteractable sit = socket.GetComponent<ShipSitInteractable>();
            if (wantsSit == false) {
                if (sit == null)
                    return dirty || installDirty;

                Object.DestroyImmediate(sit);
                return true;
            }

            if (sit == null) {
                sit = socket.gameObject.AddComponent<ShipSitInteractable>();
                dirty = true;
            }

            SerializedObject sitSerialized = new SerializedObject(sit);
            SerializedProperty sitOutline = sitSerialized.FindProperty("_outline");
            SerializedProperty sitSocket = sitSerialized.FindProperty("_socket");
            SerializedProperty sitShip = sitSerialized.FindProperty("_ship");
            bool sitDirty = false;
            if (sitOutline.objectReferenceValue != outline) {
                sitOutline.objectReferenceValue = outline;
                sitDirty = true;
            }

            if (sitSocket.objectReferenceValue != socket) {
                sitSocket.objectReferenceValue = socket;
                sitDirty = true;
            }

            if (sitShip.objectReferenceValue != ship) {
                sitShip.objectReferenceValue = ship;
                sitDirty = true;
            }

            if (sitDirty)
                sitSerialized.ApplyModifiedPropertiesWithoutUndo();

            return dirty || installDirty || sitDirty;
        }

        private static bool CanHostSeat(ShipModuleType type) {
            return type == ShipModuleType.Control
                || type == ShipModuleType.Seat
                || type == ShipModuleType.Turret;
        }

        private static bool EnsureLeverInteractable(ShipLaunchLever lever) {
            if (lever == null)
                return false;

            bool dirty = false;
            Outline outline = lever.GetComponent<Outline>();
            ShipLaunchInteractable launch = lever.GetComponent<ShipLaunchInteractable>();
            if (launch == null) {
                launch = lever.gameObject.AddComponent<ShipLaunchInteractable>();
                dirty = true;
            }

            SerializedObject serialized = new SerializedObject(launch);
            SerializedProperty outlineProperty = serialized.FindProperty("_outline");
            SerializedProperty leverProperty = serialized.FindProperty("_lever");
            bool refsDirty = false;
            if (outlineProperty.objectReferenceValue != outline) {
                outlineProperty.objectReferenceValue = outline;
                refsDirty = true;
            }

            if (leverProperty.objectReferenceValue != lever) {
                leverProperty.objectReferenceValue = lever;
                refsDirty = true;
            }

            if (refsDirty)
                serialized.ApplyModifiedPropertiesWithoutUndo();

            return dirty || refsDirty;
        }

        private static ShipLaunchLever CreateLaunchLever(Transform parent, ShipBase ship, int interactableLayer) {
            GameObject leverObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leverObject.name = "LaunchLever";
            leverObject.transform.SetParent(parent, false);
            leverObject.transform.localPosition = new Vector3(0f, 0.55f, -1.2f);
            leverObject.transform.localScale = new Vector3(0.15f, 0.7f, 0.15f);
            if (interactableLayer >= 0)
                leverObject.layer = interactableLayer;

            BoxCollider leverCollider = leverObject.GetComponent<BoxCollider>();
            if (leverCollider != null)
                leverCollider.isTrigger = true;

            leverObject.AddComponent<NetworkIdentity>();
            Outline outline = leverObject.AddComponent<Outline>();
            outline.enabled = false;
            outline.OutlineMode = Outline.Mode.OutlineVisible;
            outline.OutlineColor = new Color(1f, 0.55f, 0.15f, 1f);
            outline.OutlineWidth = 5f;

            ShipLaunchLever lever = leverObject.AddComponent<ShipLaunchLever>();
            SerializedObject serialized = new SerializedObject(lever);
            serialized.FindProperty("_ship").objectReferenceValue = ship;
            serialized.FindProperty("_outline").objectReferenceValue = outline;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EnsureLeverInteractable(lever);
            return lever;
        }

        private static void StripLobbyShip() {
            string scenePath = ScenesRoot + "/" + SceneNames.Lobby + ".unity";
            if (File.Exists(ToAbsolute(scenePath)) == false)
                return;

            Scene scene = OpenSceneIfNeeded(scenePath, out bool openedAdditive);
            try {
                bool dirty = false;
                foreach (GameObject root in scene.GetRootGameObjects()) {
                    if (root.GetComponentInChildren<ShipBase>(true) == null &&
                        root.GetComponentInChildren<ShipItem>(true) == null)
                        continue;

                    Object.DestroyImmediate(root);
                    dirty = true;
                }

                if (dirty == false)
                    return;

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally {
                if (openedAdditive)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void EnsureGameShip() {
            GameObject shipPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShipPlatformPrefabPath);
            GameObject enginePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RocketEngineItemPrefabPath);
            GameObject largeEnginePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LargeEngineItemPrefabPath);
            GameObject propellerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PropellerItemPrefabPath);
            GameObject helmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HelmItemPrefabPath);
            if (shipPrefab == null || enginePrefab == null)
                return;

            string scenePath = ScenesRoot + "/" + SceneNames.Game + ".unity";
            if (File.Exists(ToAbsolute(scenePath)) == false)
                return;

            Scene scene = OpenSceneIfNeeded(scenePath, out bool openedAdditive);
            try {
                bool dirty = false;
                ShipBase existingShip = FindInScene<ShipBase>(scene);
                if (existingShip == null) {
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(shipPrefab, scene);
                    instance.name = "ShipPlatform";
                    instance.transform.position = new Vector3(0f, 0.15f, 6f);
                    AssignSceneIdentities(instance);
                    dirty = true;
                }
                else if (AssignSceneIdentities(existingShip.gameObject)) {
                    dirty = true;
                }

                if (EnsureGameLandingPad(scene))
                    dirty = true;

                int engineCount = CountItemView(scene, ItemViewId.Engine);
                Vector3[] enginePositions = {
                    new Vector3(2.2f, 1.1f, 4.2f),
                    new Vector3(2.8f, 1.1f, 5.1f)
                };
                for (int i = engineCount; i < enginePositions.Length; i++) {
                    GameObject engine = (GameObject)PrefabUtility.InstantiatePrefab(enginePrefab, scene);
                    engine.name = "RocketEngineItem";
                    engine.transform.position = enginePositions[i];
                    AssignSceneIdentities(engine);
                    dirty = true;
                }

                if (largeEnginePrefab != null && SceneHasItemView(scene, ItemViewId.LargeEngine) == false) {
                    GameObject largeEngine = (GameObject)PrefabUtility.InstantiatePrefab(largeEnginePrefab, scene);
                    largeEngine.name = "LargeEngineItem";
                    largeEngine.transform.position = new Vector3(-2.2f, 1.2f, 4.2f);
                    AssignSceneIdentities(largeEngine);
                    dirty = true;
                }

                if (propellerPrefab != null && SceneHasItemView(scene, ItemViewId.Propeller) == false) {
                    GameObject propeller = (GameObject)PrefabUtility.InstantiatePrefab(propellerPrefab, scene);
                    propeller.name = "PropellerItem";
                    propeller.transform.position = new Vector3(-2.8f, 1.1f, 5.1f);
                    AssignSceneIdentities(propeller);
                    dirty = true;
                }

                if (helmPrefab != null && SceneHasItemView(scene, ItemViewId.Helm) == false) {
                    GameObject helm = (GameObject)PrefabUtility.InstantiatePrefab(helmPrefab, scene);
                    helm.name = "HelmItem";
                    helm.transform.position = new Vector3(0f, 1.2f, 3.6f);
                    AssignSceneIdentities(helm);
                    dirty = true;
                }

                if (dirty == false)
                    return;

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally {
                if (openedAdditive)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static bool EnsureGameLandingPad(Scene scene) {
            GameObject padPrefab = EnsureStationPadPrefab();
            bool dirty = false;
            ShipLandingPad pad = FindInScene<ShipLandingPad>(scene);
            if (pad != null && pad.GetComponent<NetworkIdentity>() == null) {
                Object.DestroyImmediate(pad.transform.root.gameObject);
                pad = null;
                dirty = true;
            }

            GameObject leftover = FindNamedRoot(scene, "LandingPlatform");
            if (leftover != null) {
                Object.DestroyImmediate(leftover);
                pad = FindInScene<ShipLandingPad>(scene);
                dirty = true;
            }

            if (pad == null && padPrefab != null) {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(padPrefab, scene);
                instance.name = "StationPad";
                instance.transform.position = new Vector3(0f, 0f, 6f);
                AssignSceneIdentities(instance);
                pad = instance.GetComponentInChildren<ShipLandingPad>();
                dirty = true;
            }
            else if (pad != null && AssignSceneIdentities(pad.gameObject)) {
                dirty = true;
            }

            ShipBase ship = FindInScene<ShipBase>(scene);
            if (ship != null && pad != null) {
                Transform landing = pad.LandingPoint;
                Transform berth = pad.BuildBerth;
                Transform place = berth != null ? berth : landing;
                if (place != null && (ship.transform.position - place.position).sqrMagnitude > 0.01f) {
                    ship.transform.SetPositionAndRotation(place.position, place.rotation);
                    dirty = true;
                }
            }

            if (EnsureRunDirector(scene, ship, pad))
                dirty = true;

            return dirty;
        }

        private static bool EnsureRunDirector(Scene scene, ShipBase ship, ShipLandingPad pad) {
            ShipRunDirector director = FindInScene<ShipRunDirector>(scene);
            bool dirty = false;
            if (director == null) {
                GameObject directorObject = new GameObject("ShipRunDirector");
                SceneManager.MoveGameObjectToScene(directorObject, scene);
                directorObject.AddComponent<NetworkIdentity>();
                director = directorObject.AddComponent<ShipRunDirector>();
                dirty = true;
            }

            if (AssignSceneIdentities(director.gameObject))
                dirty = true;

            ShipStationCatalog catalog = EnsureStationCatalog();
            SerializedObject serialized = new SerializedObject(director);
            SerializedProperty shipProperty = serialized.FindProperty("_ship");
            SerializedProperty padProperty = serialized.FindProperty("_startPad");
            SerializedProperty stationsProperty = serialized.FindProperty("_stations");
            bool refsDirty =
                shipProperty.objectReferenceValue != ship ||
                padProperty.objectReferenceValue != pad ||
                stationsProperty.objectReferenceValue != catalog;
            if (refsDirty) {
                shipProperty.objectReferenceValue = ship;
                padProperty.objectReferenceValue = pad;
                stationsProperty.objectReferenceValue = catalog;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                dirty = true;
            }

            return dirty;
        }

        private static GameObject FindNamedRoot(Scene scene, string objectName) {
            foreach (GameObject root in scene.GetRootGameObjects()) {
                if (root.name == objectName)
                    return root;
            }

            return null;
        }

        private static bool AssignSceneIdentities(GameObject instance) {
            bool dirty = false;
            NetworkIdentity[] identities = instance.GetComponentsInChildren<NetworkIdentity>(true);
            for (int i = 0; i < identities.Length; i++) {
                if (EnsureSceneIdentity(identities[i].gameObject))
                    dirty = true;
            }

            return dirty;
        }

        private static int CountInScene<T>(Scene scene) where T : Object {
            int count = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
                count += root.GetComponentsInChildren<T>(true).Length;

            return count;
        }

        private static int CountItemView(Scene scene, ItemViewId view) {
            int count = 0;
            foreach (GameObject root in scene.GetRootGameObjects()) {
                ShipItem[] items = root.GetComponentsInChildren<ShipItem>(true);
                for (int i = 0; i < items.Length; i++) {
                    if (items[i].View == view)
                        count++;
                }
            }

            return count;
        }

        private static bool SceneHasItemView(Scene scene, ItemViewId view) {
            foreach (GameObject root in scene.GetRootGameObjects()) {
                ShipItem[] items = root.GetComponentsInChildren<ShipItem>(true);
                for (int i = 0; i < items.Length; i++) {
                    if (items[i].View == view)
                        return true;
                }
            }

            return false;
        }

        private static void EnsureLobbyGrabbable() {
            EnsureLobbyGrabbable(AssetDatabase.LoadAssetAtPath<GameObject>(DummyGrabbablePrefabPath));
        }

        private static void EnsureLobbyGrabbable(GameObject prefab) {
            if (prefab == null)
                return;

            string scenePath = ScenesRoot + "/" + SceneNames.Lobby + ".unity";
            if (File.Exists(ToAbsolute(scenePath)) == false)
                return;

            Scene scene = OpenSceneIfNeeded(scenePath, out bool openedAdditive);
            try {
                Grabbable existing = FindInScene<Grabbable>(scene);
                GameObject instance = existing != null ? existing.gameObject : null;
                bool created = false;
                if (instance == null) {
                    instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    instance.name = "DummyGrabbable";
                    instance.transform.position = new Vector3(1.2f, 1.2f, 1.5f);
                    created = true;
                }

                bool assignedId = EnsureSceneIdentity(instance);
                if (created == false && assignedId == false)
                    return;

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally {
                if (openedAdditive)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static Scene OpenSceneIfNeeded(string scenePath, out bool openedAdditive) {
            for (int i = 0; i < SceneManager.sceneCount; i++) {
                Scene loaded = SceneManager.GetSceneAt(i);
                if (loaded.path != scenePath)
                    continue;

                openedAdditive = false;
                return loaded;
            }

            openedAdditive = true;
            return EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        }

        private static bool EnsureSceneIdentity(GameObject instance) {
            NetworkIdentity identity = instance.GetComponent<NetworkIdentity>();
            if (identity == null)
                return false;

            SerializedObject serialized = new SerializedObject(identity);
            SerializedProperty sceneId = serialized.FindProperty("sceneId");
            if (sceneId == null || sceneId.longValue != 0)
                return false;

            sceneId.longValue = (long)(uint)UnityEngine.Random.Range(1, int.MaxValue);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(identity);
            return true;
        }

        private static T FindInScene<T>(Scene scene) where T : Object {
            foreach (GameObject root in scene.GetRootGameObjects()) {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static void AssignPlayerCameraAnchor(GameObject player, PlayerCameraAnchor anchor) {
            Transform follow = EnsureChild(player.transform, "CameraFollow", new Vector3(0f, 1.55f, 0f));
            Transform lookAt = EnsureChild(player.transform, "CameraLookAt", new Vector3(0f, 1.4f, 0f));
            Transform eye = EnsureChild(player.transform, "CameraEye", new Vector3(0f, 1.65f, 0f));

            SerializedObject anchorSerialized = new SerializedObject(anchor);
            anchorSerialized.FindProperty("_follow").objectReferenceValue = follow;
            anchorSerialized.FindProperty("_lookAt").objectReferenceValue = lookAt;
            anchorSerialized.FindProperty("_eye").objectReferenceValue = eye;
            anchorSerialized.FindProperty("_startupCameraId").stringValue = CameraIds.TPCamera;
            anchorSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform EnsureChild(Transform parent, string childName, Vector3 localPosition) {
            Transform existing = parent.Find(childName);
            if (existing != null) {
                existing.localPosition = localPosition;
                return existing;
            }

            var child = new GameObject(childName);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localRotation = Quaternion.identity;
            return child.transform;
        }

        private static void CreateBootstrapScene() {
            string path = ScenesRoot + "/" + SceneNames.Bootstrap + ".unity";
            if (File.Exists(ToAbsolute(path)))
                return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject contextObject = CreateSceneContextObject(scene, "BootstrapSceneContext", null, null);
            contextObject.AddComponent<BootstrapSceneBootstrapper>();
            EditorSceneManager.SaveScene(scene, path);
        }

        private static void CreateGlobalScene(GameObject playerPrefab) {
            string path = ScenesRoot + "/" + SceneNames.Global + ".unity";
            if (File.Exists(ToAbsolute(path)))
                return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject contextObject = CreateSceneContextObject(
                scene,
                "GlobalSceneContext",
                new[] { SceneNames.GlobalContract },
                null);
            GlobalSceneInstaller installer = contextObject.AddComponent<GlobalSceneInstaller>();
            contextObject.GetComponent<SceneContext>().Installers = new MonoInstaller[] { installer };
            contextObject.AddComponent<GlobalSceneBootstrapper>();

            CreateConnectionObject(scene, playerPrefab);
            CreateSteamManager(scene);
            EditorSceneManager.SaveScene(scene, path);
        }

        private static void CreateConnectionObject(Scene scene, GameObject playerPrefab) {
            var connectionObject = new GameObject("ConnectionNetwork");
            SceneManager.MoveGameObjectToScene(connectionObject, scene);

            TelepathyTransport transport = connectionObject.AddComponent<TelepathyTransport>();
            FizzySteamworks fizzyTransport = connectionObject.AddComponent<FizzySteamworks>();
            fizzyTransport.enabled = false;
            ConnectionAuthenticator authenticator = connectionObject.AddComponent<ConnectionAuthenticator>();
            ConnectionNetworkManager networkManager = connectionObject.AddComponent<ConnectionNetworkManager>();

            SerializedObject networkSerialized = new SerializedObject(networkManager);
            networkSerialized.FindProperty("dontDestroyOnLoad").boolValue = true;
            networkSerialized.FindProperty("maxConnections").intValue = 4;
            networkSerialized.FindProperty("offlineScene").stringValue = string.Empty;
            networkSerialized.FindProperty("onlineScene").stringValue = string.Empty;
            networkSerialized.FindProperty("transport").objectReferenceValue = transport;
            networkSerialized.FindProperty("telepathyTransport").objectReferenceValue = transport;
            networkSerialized.FindProperty("fizzyTransport").objectReferenceValue = fizzyTransport;
            networkSerialized.FindProperty("authenticator").objectReferenceValue = authenticator;
            networkSerialized.FindProperty("playerPrefab").objectReferenceValue = playerPrefab;
            networkSerialized.FindProperty("autoCreatePlayer").boolValue = true;
            networkSerialized.FindProperty("lobbySceneName").stringValue = SceneNames.Lobby;
            networkSerialized.FindProperty("gameSceneName").stringValue = SceneNames.Game;
            networkSerialized.FindProperty("menuSceneName").stringValue = SceneNames.Menu;
            networkSerialized.FindProperty("persistentSceneName").stringValue = SceneNames.Global;
            networkSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateSteamManager(Scene scene) {
            if (UnityEngine.Object.FindFirstObjectByType<SteamManager>(FindObjectsInactive.Include) != null)
                return;

            var steamObject = new GameObject("SteamManager");
            SceneManager.MoveGameObjectToScene(steamObject, scene);
            steamObject.AddComponent<SteamManager>();
        }

        private static void CreateMenuScene() {
            string path = ScenesRoot + "/" + SceneNames.Menu + ".unity";
            if (File.Exists(ToAbsolute(path)))
                return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject contextObject = CreateSceneContextObject(
                scene,
                "MenuSceneContext",
                null,
                new[] { SceneNames.GlobalContract });
            MenuSceneInstaller installer = contextObject.AddComponent<MenuSceneInstaller>();
            contextObject.GetComponent<SceneContext>().Installers = new MonoInstaller[] { installer };
            EditorSceneManager.SaveScene(scene, path);
        }

        private static void CreateLobbyScene() {
            string path = ScenesRoot + "/" + SceneNames.Lobby + ".unity";
            if (File.Exists(ToAbsolute(path)))
                return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            GameObject contextObject = CreateSceneContextObject(
                scene,
                "LobbySceneContext",
                null,
                new[] { SceneNames.GlobalContract });
            LobbySceneInstaller installer = contextObject.AddComponent<LobbySceneInstaller>();
            contextObject.GetComponent<SceneContext>().Installers = new MonoInstaller[] { installer };

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            SceneManager.MoveGameObjectToScene(ground, scene);

            GameObject spawnPointObject = new GameObject("SpawnPoint");
            spawnPointObject.transform.position = new Vector3(0f, 1f, 0f);
            SceneManager.MoveGameObjectToScene(spawnPointObject, scene);
            spawnPointObject.AddComponent<ConnectionSpawnPoint>();
            EditorSceneManager.SaveScene(scene, path);
        }

        private static void CreateGameScene() {
            string path = ScenesRoot + "/" + SceneNames.Game + ".unity";
            if (File.Exists(ToAbsolute(path)))
                return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            GameObject contextObject = CreateSceneContextObject(
                scene,
                "GameSceneContext",
                null,
                new[] { SceneNames.GlobalContract });
            GameSceneInstaller installer = contextObject.AddComponent<GameSceneInstaller>();
            contextObject.GetComponent<SceneContext>().Installers = new MonoInstaller[] { installer };

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            SceneManager.MoveGameObjectToScene(ground, scene);

            GameObject spawnPointObject = new GameObject("SpawnPoint");
            spawnPointObject.transform.position = new Vector3(0f, 1f, 0f);
            SceneManager.MoveGameObjectToScene(spawnPointObject, scene);
            spawnPointObject.AddComponent<ConnectionSpawnPoint>();
            EditorSceneManager.SaveScene(scene, path);
        }

        private static GameObject CreateSceneContextObject(
            Scene scene,
            string objectName,
            string[] contractNames,
            string[] parentContractNames) {
            var contextObject = new GameObject(objectName);
            SceneManager.MoveGameObjectToScene(contextObject, scene);
            SceneContext sceneContext = contextObject.AddComponent<SceneContext>();
            sceneContext.ContractNames = contractNames ?? new string[0];
            sceneContext.ParentContractNames = parentContractNames ?? new string[0];
            return contextObject;
        }

        private static void UpdateBuildSettings() {
            EditorBuildSettings.scenes = new[] {
                new EditorBuildSettingsScene(ScenesRoot + "/" + SceneNames.Bootstrap + ".unity", true),
            };
        }

        private static ConnectionConfig CreateConnectionConfig() {
            ConnectionConfig existing = AssetDatabase.LoadAssetAtPath<ConnectionConfig>(ConnectionConfigPath);
            if (existing != null) {
                MarkAddressable(
                    ConnectionConfigPath,
                    Address.Configurations.ConnectionConfig_Default,
                    ConfigurationsAddressableGroup);
                return existing;
            }

            ConnectionConfig config = ScriptableObject.CreateInstance<ConnectionConfig>();
            AssetDatabase.CreateAsset(config, ConnectionConfigPath);
            EditorUtility.SetDirty(config);
            MarkAddressable(
                ConnectionConfigPath,
                Address.Configurations.ConnectionConfig_Default,
                ConfigurationsAddressableGroup);
            return config;
        }

        private static void SetupAddressableScenesAndConfigs() {
            CreateConnectionConfig();
            EnsureShipRunConfig();
            EnsureFlightConfig();

            MarkAddressable(
                ScenesRoot + "/" + SceneNames.Global + ".unity",
                SceneNames.Global,
                LocalScenesAddressableGroup);

            MarkAddressable(
                ScenesRoot + "/" + SceneNames.Menu + ".unity",
                SceneNames.Menu,
                LocalScenesAddressableGroup);

            MarkAddressable(
                ScenesRoot + "/" + SceneNames.Lobby + ".unity",
                SceneNames.Lobby,
                LocalScenesAddressableGroup);

            MarkAddressable(
                ScenesRoot + "/" + SceneNames.Game + ".unity",
                SceneNames.Game,
                LocalScenesAddressableGroup);

            CreateWindowPrefabs();
        }

        private static void StripLegacySceneUi() {
            StripSceneRootObjects(ScenesRoot + "/" + SceneNames.Menu + ".unity", "Canvas");
            StripSceneRootObjects(ScenesRoot + "/" + SceneNames.Lobby + ".unity", "Canvas", "EventSystem");
        }

        private static void StripSceneRootObjects(string scenePath, params string[] rootNames) {
            if (File.Exists(ToAbsolute(scenePath)) == false)
                return;

            Scene scene = default;
            bool openedAdditive = false;
            for (int i = 0; i < SceneManager.sceneCount; i++) {
                Scene loaded = SceneManager.GetSceneAt(i);
                if (loaded.path != scenePath)
                    continue;

                scene = loaded;
                break;
            }

            if (scene.IsValid() == false) {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                openedAdditive = true;
            }

            bool dirty = false;
            foreach (GameObject root in scene.GetRootGameObjects()) {
                bool isCanvas = root.GetComponent<Canvas>() != null;
                bool nameMatches = false;
                foreach (string rootName in rootNames) {
                    if (root.name != rootName)
                        continue;

                    nameMatches = true;
                    break;
                }

                if (isCanvas == false && nameMatches == false)
                    continue;

                Object.DestroyImmediate(root);
                dirty = true;
            }

            if (dirty)
                EditorSceneManager.SaveScene(scene);

            if (openedAdditive)
                EditorSceneManager.CloseScene(scene, true);
        }

        private static void CreateWindowPrefabs() {
            CreateMenuWindowPrefab();
            CreateGameHudWindowPrefab();
        }

        private static void CreateMenuWindowPrefab() {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(MenuWindowPrefabPath) != null) {
                MarkAddressable(MenuWindowPrefabPath, nameof(MenuWindow), WindowsAddressableGroup);
                return;
            }

            var root = new GameObject(nameof(MenuWindow), typeof(RectTransform));
            StretchFullScreen(root.GetComponent<RectTransform>());
            root.AddComponent<MonoWindowInstance>();

            GameObject panel = CreateUiPanel(root.transform, "Panel");
            MenuSessionView view = panel.AddComponent<MenuSessionView>();
            InputField addressInput = CreateInputField(panel.transform, "AddressInput", "localhost or Steam lobby id", new Vector2(0f, 80f));
            Button hostButton = CreateButton(panel.transform, "HostButton", "Host", new Vector2(-120f, 20f));
            Button joinButton = CreateButton(panel.transform, "JoinButton", "Join", new Vector2(-120f, -40f));
            Button hostSteamButton = CreateButton(panel.transform, "HostSteamButton", "Host Steam", new Vector2(120f, 20f));
            Button joinSteamButton = CreateButton(panel.transform, "JoinSteamButton", "Join Steam", new Vector2(120f, -40f));
            Text statusText = CreateLabel(panel.transform, "StatusText", string.Empty, new Vector2(0f, -100f));

            SerializedObject viewSerialized = new SerializedObject(view);
            viewSerialized.FindProperty("_addressInput").objectReferenceValue = addressInput;
            viewSerialized.FindProperty("_hostButton").objectReferenceValue = hostButton;
            viewSerialized.FindProperty("_joinButton").objectReferenceValue = joinButton;
            viewSerialized.FindProperty("_hostSteamButton").objectReferenceValue = hostSteamButton;
            viewSerialized.FindProperty("_joinSteamButton").objectReferenceValue = joinSteamButton;
            viewSerialized.FindProperty("_statusText").objectReferenceValue = statusText;
            viewSerialized.ApplyModifiedPropertiesWithoutUndo();

            EnsureFolder(MenuPrefabsRoot);
            PrefabUtility.SaveAsPrefabAsset(root, MenuWindowPrefabPath);
            Object.DestroyImmediate(root);
            MarkAddressable(MenuWindowPrefabPath, nameof(MenuWindow), WindowsAddressableGroup);
        }

        private static void CreateGameHudWindowPrefab() {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(GameHudWindowPrefabPath);
            if (existing != null) {
                UpgradeGameHudWindowPrefab(existing);
                MarkAddressable(GameHudWindowPrefabPath, nameof(GameHudWindow), WindowsAddressableGroup);
                return;
            }

            var root = new GameObject(nameof(GameHudWindow), typeof(RectTransform));
            StretchFullScreen(root.GetComponent<RectTransform>());
            root.AddComponent<MonoWindowInstance>();

            Button leaveButton = CreateButton(root.transform, "LeaveButton", "Leave", new Vector2(0f, 200f));
            LeaveSessionView leaveView = leaveButton.gameObject.AddComponent<LeaveSessionView>();
            SerializedObject leaveSerialized = new SerializedObject(leaveView);
            leaveSerialized.FindProperty("_button").objectReferenceValue = leaveButton;
            leaveSerialized.ApplyModifiedPropertiesWithoutUndo();

            Button startButton = CreateButton(root.transform, "StartGameButton", "Start Game", new Vector2(0f, 140f));
            StartGameView startView = startButton.gameObject.AddComponent<StartGameView>();
            SerializedObject startSerialized = new SerializedObject(startView);
            startSerialized.FindProperty("_button").objectReferenceValue = startButton;
            startSerialized.ApplyModifiedPropertiesWithoutUndo();

            EnsureFolder(LobbyPrefabsRoot);
            PrefabUtility.SaveAsPrefabAsset(root, GameHudWindowPrefabPath);
            Object.DestroyImmediate(root);
            MarkAddressable(GameHudWindowPrefabPath, nameof(GameHudWindow), WindowsAddressableGroup);
        }

        private static void UpgradeGameHudWindowPrefab(GameObject prefabAsset) {
            string path = AssetDatabase.GetAssetPath(prefabAsset);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try {
                if (root.GetComponentInChildren<StartGameView>(true) != null)
                    return;

                Button startButton = CreateButton(root.transform, "StartGameButton", "Start Game", new Vector2(-140f, 140f));
                RectTransform rect = startButton.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.anchoredPosition = new Vector2(-138.9f, -110f);

                StartGameView startView = startButton.gameObject.AddComponent<StartGameView>();
                SerializedObject startSerialized = new SerializedObject(startView);
                startSerialized.FindProperty("_button").objectReferenceValue = startButton;
                startSerialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void StretchFullScreen(RectTransform rect) {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void MarkAddressable(string assetPath, string address, string groupName) {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null) {
                Debug.LogWarning("GameCore: AddressableAssetSettings not found. Skip marking " + assetPath);
                return;
            }

            AddressableAssetGroup group = settings.FindGroup(groupName);
            if (group == null) {
                group = settings.CreateGroup(
                    groupName,
                    false,
                    false,
                    true,
                    null,
                    typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema),
                    typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema));
            }

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid)) {
                Debug.LogWarning("GameCore: Cannot mark addressable, GUID missing for " + assetPath);
                return;
            }

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.SetAddress(address);
            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(group);
        }

        private static GameObject CreateUiPanel(Transform parent, string name) {
            var panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(420f, 360f);
            return panel;
        }

        private static InputField CreateInputField(Transform parent, string name, string placeholder, Vector2 anchoredPosition) {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(320f, 40f);
            rect.anchoredPosition = anchoredPosition;

            Image image = root.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.9f);
            InputField inputField = root.AddComponent<InputField>();

            Text text = CreateLabel(root.transform, "Text", string.Empty, Vector2.zero);
            text.color = Color.black;
            text.alignment = TextAnchor.MiddleLeft;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 0f);
            textRect.offsetMax = new Vector2(-10f, 0f);

            Text placeholderText = CreateLabel(root.transform, "Placeholder", placeholder, Vector2.zero);
            placeholderText.fontStyle = FontStyle.Italic;
            placeholderText.color = new Color(0.2f, 0.2f, 0.2f, 0.6f);
            placeholderText.alignment = TextAnchor.MiddleLeft;
            RectTransform placeholderRect = placeholderText.rectTransform;
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(10f, 0f);
            placeholderRect.offsetMax = new Vector2(-10f, 0f);

            inputField.textComponent = text;
            inputField.placeholder = placeholderText;
            return inputField;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition) {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(220f, 44f);
            rect.anchoredPosition = anchoredPosition;

            Image image = root.AddComponent<Image>();
            image.color = new Color(0.2f, 0.45f, 0.8f, 1f);
            Button button = root.AddComponent<Button>();

            Text text = CreateLabel(root.transform, "Text", label, Vector2.zero);
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            return button;
        }

        private static Text CreateLabel(Transform parent, string name, string value, Vector2 anchoredPosition) {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(360f, 40f);
            rect.anchoredPosition = anchoredPosition;

            Text text = root.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            return text;
        }

        private static string ToAbsolute(string assetPath) {
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), assetPath));
        }
    }
}
#endif
