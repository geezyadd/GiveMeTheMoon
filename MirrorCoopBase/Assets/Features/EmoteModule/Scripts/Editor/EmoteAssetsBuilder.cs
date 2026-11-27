using System.IO;
using System.Linq;
using Features.CharacterMovableModule.Scripts;
using Features.EmoteModule.Data;
using Features.EmoteModule.Player;
using Features.EmoteModule.UI;
using Features.MvpModule;
using Features.PlayerAnimationModule;
using Features.PlayerAnimationModule.Editor;
using Features.PlayerReplicaModule;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Features.EmoteModule.Editor {
    // Creates the emote module's assets once and puts them where the game finds them (Tools > Emotes > Build Emote
    // Assets): the emote catalog with placeholder emotes and the wheel's configuration (Addressables Configurations),
    // the wheel window's prefab (Addressables Windows), the emote components on the player prefab; then rebuilds the
    // player animator, whose Emote layer is made from the catalog. Assets that exist are left as they are (the catalog,
    // the configuration and the window may have been changed by hand since); only the player's references are set
    // again.
    public static class EmoteAssetsBuilder {
        private const string ModuleFolder = "Assets/Features/EmoteModule/GameResources";
        private const string CatalogPath = ModuleFolder + "/Configurations/EmoteCatalog_Default.asset";
        private const string ConfigurationPath = ModuleFolder + "/Configurations/EmoteWheelConfiguration_Default.asset";
        private const string WindowPath = ModuleFolder + "/Prefabs/EmoteWheelWindow.prefab";
        private const string PlayerPrefabPath = "Assets/Features/PlayerModule/GameResources/Prefabs/DummyPlayer.prefab";
        private const string SlapClipPath = "Assets/Features/PlayerAnimationModule/GameResources/Animations/PlayerSlap.anim";
        private const string ClipsFolder = "Assets/Global/Packages/KevinIglesias/HumanAnimations/Animations/Male";
        private const string ConfigurationsGroup = "Configurations";
        private const string WindowsGroup = "Windows";
        private const float WheelPixels = 480f;

        [MenuItem("Tools/Emotes/Build Emote Assets")]
        public static void BuildAll() {
            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                Debug.LogError("Emote: leave Play mode before building the emote assets.");
                return;
            }

            CreateConfigurations();
            CreateWheelWindow();
            AddToPlayer();
            PlayerAnimatorBuilder.BuildAndAssign();
            AssetDatabase.SaveAssets();
            Debug.Log("Emote: the emote assets are built.");
        }

        // The catalog (placeholder clips reused from the idle, the talk and the slap) and the wheel's configuration, in
        // the Configurations group at their file names.
        public static void CreateConfigurations() {
            if (AssetDatabase.LoadAssetAtPath<EmoteCatalog>(CatalogPath) == null) {
                EmoteCatalog catalog = ScriptableObject.CreateInstance<EmoteCatalog>();
                catalog.SetEmotes(new[] {
                    new EmoteEntry("Wave", "EmoteWave", LoadClip(SlapClipPath)),
                    new EmoteEntry("Dance", "EmoteDance", LoadClip(ClipsFolder + "/Idles/HumanM@Idle01-Idle02.fbx")),
                    new EmoteEntry("Point", "EmotePoint", LoadClip(ClipsFolder + "/Idles/HumanM@Idle02.fbx")),
                    new EmoteEntry("Laugh", "EmoteLaugh", LoadClip(ClipsFolder + "/Social/Conversation/HumanM@Talk01.fbx"))
                });
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            if (AssetDatabase.LoadAssetAtPath<EmoteWheelConfiguration>(ConfigurationPath) == null)
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<EmoteWheelConfiguration>(), ConfigurationPath);

            AddToGroup(CatalogPath, ConfigurationsGroup);
            AddToGroup(ConfigurationPath, ConfigurationsGroup);
            AssetDatabase.SaveAssets();
        }

        private static void CreateWheelWindow() {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(WindowPath) == null) {
                GameObject root = BuildWheelWindow();
                PrefabUtility.SaveAsPrefabAsset(root, WindowPath);
                Object.DestroyImmediate(root);
            }

            AddToGroup(WindowPath, WindowsGroup);
        }

        // The wheel window: a full screen root, the wheel in its middle with a sector template the view copies once per
        // emote (the ring piece, and the icon or the name in its middle).
        private static GameObject BuildWheelWindow() {
            GameObject root = NewUiObject(nameof(EmoteWheelWindow), null);
            Stretch(root.GetComponent<RectTransform>());
            root.AddComponent<MonoWindowInstance>();

            GameObject wheel = NewUiObject("Wheel", root.transform);
            wheel.GetComponent<RectTransform>().sizeDelta = new Vector2(WheelPixels, WheelPixels);
            EmoteWheelView view = wheel.AddComponent<EmoteWheelView>();

            GameObject sectors = NewUiObject("Sectors", wheel.transform);
            Stretch(sectors.GetComponent<RectTransform>());

            GameObject template = NewUiObject("SectorTemplate", sectors.transform);
            Stretch(template.GetComponent<RectTransform>());
            EmoteSectorGraphic graphic = template.AddComponent<EmoteSectorGraphic>();
            graphic.raycastTarget = false;
            EmoteWheelSector sector = template.AddComponent<EmoteWheelSector>();

            GameObject content = NewUiObject("Content", template.transform);
            content.GetComponent<RectTransform>().sizeDelta = new Vector2(140f, 64f);

            GameObject iconObject = NewUiObject("Icon", content.transform);
            iconObject.GetComponent<RectTransform>().sizeDelta = new Vector2(64f, 64f);
            Image icon = iconObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            GameObject labelObject = NewUiObject("Label", content.transform);
            Stretch(labelObject.GetComponent<RectTransform>());
            Text label = labelObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 24;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            labelObject.AddComponent<Outline>();

            SetReferences(sector, ("_graphic", graphic), ("_content", content.GetComponent<RectTransform>()),
                ("_label", label), ("_icon", icon));
            SetReferences(view, ("_sectorTemplate", sector), ("_sectorsRoot", sectors.GetComponent<RectTransform>()));
            return root;
        }

        // The emote components sit on the player's root, next to its state channel (which reads the plays from there).
        private static void AddToPlayer() {
            GameObject player = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try {
                BodyStateChannel channel = player.GetComponentInChildren<BodyStateChannel>(true);
                PlayerAnimationDriver driver = player.GetComponentInChildren<PlayerAnimationDriver>(true);
                if (channel == null || driver == null) {
                    Debug.LogError("Emote: the player prefab has no body state channel or no animation driver.");
                    return;
                }

                GameObject body = channel.gameObject;
                EmotePlayer emotePlayer = GetOrAdd<EmotePlayer>(body);
                PlayerEmotePlays plays = GetOrAdd<PlayerEmotePlays>(body);
                CharacterMovableBase movable = body.GetComponent<CharacterMovableBase>();
                SetReferences(emotePlayer, ("_driver", driver), ("_movable", movable), ("_plays", plays));
                SetReferences(plays, ("_movable", movable), ("_copy", body.GetComponent<BodyCopyDriver>()),
                    ("_player", emotePlayer));
                PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
            } finally {
                PrefabUtility.UnloadPrefabContents(player);
            }
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void SetReferences(Object target, params (string field, Object value)[] references) {
            var serialized = new SerializedObject(target);
            foreach ((string field, Object value) in references) {
                SerializedProperty property = serialized.FindProperty(field);
                if (property == null || value == null) {
                    Debug.LogError($"Emote: {target.name} has no {field} or nothing to put there.");
                    continue;
                }

                property.objectReferenceValue = value;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject NewUiObject(string name, Transform parent) {
            var created = new GameObject(name, typeof(RectTransform)) { layer = LayerMask.NameToLayer("UI") };
            if (parent != null)
                created.transform.SetParent(parent, false);
            return created;
        }

        private static void Stretch(RectTransform rect) {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void AddToGroup(string assetPath, string groupName) {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetGroup group = settings != null ? settings.FindGroup(groupName) : null;
            if (group == null) {
                Debug.LogError($"Emote: no Addressables group {groupName}.");
                return;
            }

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(assetPath), group);
            entry.address = Path.GetFileNameWithoutExtension(assetPath);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
        }

        private static AnimationClip LoadClip(string path) {
            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .FirstOrDefault(candidate => candidate.name.StartsWith("__preview__") == false);
            if (clip == null)
                Debug.LogError($"Emote: no animation clip in {path}.");
            return clip;
        }
    }
}
