using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Features.PlayerAnimationModule.Editor {
    // Builds the player's animator controller from code, so it is never edited by hand. One layer:
    // - Locomotion (default): a 1D blend by Forward from idle (0) to walking forward (1);
    // - Fall: entered while Fly is on, left as soon as it is off.
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
        // Cross-fades, seconds: into the fall and back to the feet.
        private const float IntoFallSeconds = 0.15f;
        private const float OutOfFallSeconds = 0.2f;

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

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState fall = machine.AddState(FallState);
            fall.motion = LoadClip(FallClipPath);
            machine.defaultState = locomotion;

            AddFlyTransition(locomotion, fall, AnimatorConditionMode.If, IntoFallSeconds);
            AddFlyTransition(fall, locomotion, AnimatorConditionMode.IfNot, OutOfFallSeconds);
        }

        private static void AddFlyTransition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, float seconds) {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = seconds;
            transition.AddCondition(mode, 0f, PlayerAnimatorParameters.FlyName);
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
