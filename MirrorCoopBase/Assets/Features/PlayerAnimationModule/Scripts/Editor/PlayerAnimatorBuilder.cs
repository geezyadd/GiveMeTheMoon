using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Features.PlayerAnimationModule.Editor {
    // Builds the player's animator controller from code, so it is never edited by hand. Two layers:
    // - Base Layer: Locomotion, a 1D blend by Forward from idle (0) to walking forward (1);
    // - Fly (override): only Fall; PlayerAnimationDriver fades the layer's weight in while Fly is on, out when off.
    // Running it again rebuilds the controller in place (the asset keeps its GUID, so every reference keeps working) and
    // puts it on the humanoid in the player prefab.
    public static class PlayerAnimatorBuilder {
        public const string ControllerPath = ResourcesFolder + "/PlayerAnimator.controller";

        private const string ModuleFolder = "Assets/Features/PlayerAnimationModule";
        private const string ResourcesFolder = ModuleFolder + "/GameResources";
        private const string TemporaryPath = ResourcesFolder + "/PlayerAnimator.building.controller";
        private const string PlayerPrefabPath = "Assets/Features/PlayerModule/GameResources/Prefabs/DummyPlayer.prefab";
        private const string ClipsFolder = "Assets/Global/Packages/KevinIglesias/HumanAnimations/Animations/Male";
        private const string IdleClipPath = ClipsFolder + "/Idles/HumanM@Idle01.fbx";
        private const string WalkClipPath = ClipsFolder + "/Movement/Walk/HumanM@Walk01_Forward.fbx";
        private const string FallClipPath = ClipsFolder + "/Movement/Jump/HumanM@Fall01.anim";
        private const string LocomotionState = "Locomotion";
        private const string FallState = "Fall";

        [MenuItem("Tools/Animation/Build Player Animator")]
        public static void BuildAndAssign() {
            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                Debug.LogError("Animation: leave Play mode before building the player animator.");
                return;
            }

            AnimatorController controller = Build();
            AssignToPlayer(controller);
            Debug.Log($"Animation: built {ControllerPath} and put it on the player prefab.");
        }

        // The controller as it is now, built first if it does not exist yet.
        public static AnimatorController LoadOrBuild() {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            return controller != null ? controller : Build();
        }

        public static AnimatorController Build() {
            EnsureResourcesFolder();
            AssetDatabase.DeleteAsset(TemporaryPath);
            AnimatorController building = AnimatorController.CreateAnimatorControllerAtPath(TemporaryPath);
            Fill(building);
            AssetDatabase.SaveAssets();
            ReplaceController();
            return AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        }

        private static void Fill(AnimatorController controller) {
            controller.AddParameter(PlayerAnimatorParameters.ForwardName, AnimatorControllerParameterType.Float);
            controller.AddParameter(PlayerAnimatorParameters.FlyName, AnimatorControllerParameterType.Bool);

            AnimatorState locomotion = controller.CreateBlendTreeInController(LocomotionState, out BlendTree tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = PlayerAnimatorParameters.ForwardName;
            tree.useAutomaticThresholds = false;
            tree.AddChild(LoadClip(IdleClipPath), 0f);
            tree.AddChild(LoadClip(WalkClipPath), 1f);
            controller.layers[0].stateMachine.defaultState = locomotion;

            AddFlyLayer(controller);
        }

        // The second layer, over the locomotion: only Fall, over the whole body. It starts at weight 0 and
        // PlayerAnimationDriver fades its weight in while Fly is on and out when it is off: an empty state in an
        // override layer would freeze the layer's last pose (or, writing defaults, pull the body to its rest pose).
        private static void AddFlyLayer(AnimatorController controller) {
            controller.AddLayer(PlayerAnimatorParameters.FlyLayerName);
            AnimatorControllerLayer[] layers = controller.layers;
            AnimatorControllerLayer flyLayer = layers[layers.Length - 1];
            flyLayer.defaultWeight = 0f;
            flyLayer.blendingMode = AnimatorLayerBlendingMode.Override;
            controller.layers = layers;

            AnimatorStateMachine machine = flyLayer.stateMachine;
            AnimatorState fall = machine.AddState(FallState);
            fall.motion = LoadClip(FallClipPath);
            machine.defaultState = fall;
        }

        // The new controller's file takes the place of the old one's, whose meta (and GUID) stays; a controller is
        // always the file's main object, so references to it survive.
        private static void ReplaceController() {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) == null) {
                AssetDatabase.MoveAsset(TemporaryPath, ControllerPath);
                return;
            }

            File.Copy(FullPath(TemporaryPath), FullPath(ControllerPath), true);
            AssetDatabase.DeleteAsset(TemporaryPath);
            AssetDatabase.ImportAsset(ControllerPath, ImportAssetOptions.ForceUpdate);
        }

        private static void AssignToPlayer(AnimatorController controller) {
            GameObject player = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try {
                Animator humanoid = player.GetComponentsInChildren<Animator>(true)
                    .FirstOrDefault(animator => animator.avatar != null && animator.avatar.isHuman);
                if (humanoid == null) {
                    Debug.LogError("Animation: the player prefab has no humanoid model.");
                    return;
                }

                humanoid.runtimeAnimatorController = controller;
                PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
            } finally {
                PrefabUtility.UnloadPrefabContents(player);
            }
        }

        private static AnimationClip LoadClip(string path) {
            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .FirstOrDefault(candidate => candidate.name.StartsWith("__preview__") == false);
            if (clip == null)
                Debug.LogError($"Animation: no animation clip in {path}.");
            return clip;
        }

        private static void EnsureResourcesFolder() {
            if (AssetDatabase.IsValidFolder(ResourcesFolder) == false)
                AssetDatabase.CreateFolder(ModuleFolder, "GameResources");
        }

        private static string FullPath(string assetPath) =>
            Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? string.Empty, assetPath);
    }
}
