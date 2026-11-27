using System.Collections.Generic;
using System.IO;
using System.Linq;
using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.AddressablesConstantsGenerator.Generated;
using Features.EmoteModule.Data;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Features.PlayerAnimationModule.Editor {
    // Builds the player's animator controller from code, so it is never edited by hand. Four layers:
    // - Base Layer: Locomotion, a 1D blend by Forward from idle (0) to walking forward (1);
    // - Fly (override): only Fall; PlayerAnimationDriver fades the layer's weight in while Fly is on, out when off;
    // - Emote (override, whole body, weight 1): Empty (an empty blend tree) and one state per emote of the emote
    //   catalog, entered from any state by the emote's trigger, back to Empty at its exit time or on EmoteStop;
    // - Slap (override, upper body mask, weight 1): Empty (an empty blend tree), the Slap trigger goes to Slap, Slap
    //   goes back to Empty.
    // It also builds the upper body mask and, the first time, the slap clip (SlapClipDraft).
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
        private const string SlapClipPath = ResourcesFolder + "/Animations/PlayerSlap.anim";
        private const string UpperBodyMaskPath = ResourcesFolder + "/PlayerUpperBody.mask";
        private const string LocomotionState = "Locomotion";
        private const string FallState = "Fall";
        private const string EmptyState = "Empty";

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
            AvatarMask upperBody = BuildUpperBodyMask();
            AssetDatabase.DeleteAsset(TemporaryPath);
            AnimatorController building = AnimatorController.CreateAnimatorControllerAtPath(TemporaryPath);
            Fill(building, upperBody);
            AssetDatabase.SaveAssets();
            ReplaceController();
            return AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        }

        private static void Fill(AnimatorController controller, AvatarMask upperBody) {
            controller.AddParameter(PlayerAnimatorParameters.ForwardName, AnimatorControllerParameterType.Float);
            controller.AddParameter(PlayerAnimatorParameters.FlyName, AnimatorControllerParameterType.Bool);
            controller.AddParameter(PlayerAnimatorParameters.SlapName, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(PlayerAnimatorParameters.EmoteStopName, AnimatorControllerParameterType.Trigger);

            AnimatorState locomotion = controller.CreateBlendTreeInController(LocomotionState, out BlendTree tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = PlayerAnimatorParameters.ForwardName;
            tree.useAutomaticThresholds = false;
            tree.AddChild(LoadClip(IdleClipPath), 0f);
            tree.AddChild(LoadClip(WalkClipPath), 1f);
            controller.layers[0].stateMachine.defaultState = locomotion;

            AddFlyLayer(controller);
            AddEmoteLayer(controller, LoadEmotes());
            AddSlapLayer(controller, upperBody);
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

        // The third layer, over the whole body (no mask) at weight 1: an emote stops as soon as the player walks, jumps
        // or is in the air, so nothing else needs the legs while one plays, and a dance or a laugh moves the hips and
        // the legs too. The slap layer stays above it, so a slap during an emote still shows on the upper body.
        // Empty, a blend tree with no motions, by default (as in the slap layer); each emote's state plays the clip its
        // catalog entry names (the idle without one), entered from any state by its trigger (no exit time, so a new
        // emote replaces the one that plays, the same one starts again); back to Empty at its exit time, or at once on
        // EmoteStop. The transitions keep the editor's default exit time and duration.
        private static void AddEmoteLayer(AnimatorController controller, IReadOnlyList<EmoteEntry> emotes) {
            controller.AddLayer(PlayerAnimatorParameters.EmoteLayerName);
            AnimatorControllerLayer[] layers = controller.layers;
            int layerIndex = layers.Length - 1;
            AnimatorControllerLayer emoteLayer = layers[layerIndex];
            emoteLayer.defaultWeight = 1f;
            emoteLayer.blendingMode = AnimatorLayerBlendingMode.Override;
            controller.layers = layers;

            AnimatorStateMachine machine = emoteLayer.stateMachine;
            AnimatorState empty = controller.CreateBlendTreeInController(EmptyState, out BlendTree _, layerIndex);
            machine.defaultState = empty;

            var names = new HashSet<string>();
            foreach (EmoteEntry emote in emotes) {
                string trigger = emote.TriggerName;
                if (string.IsNullOrEmpty(trigger) || IsTaken(controller, trigger) || names.Add(trigger) == false) {
                    Debug.LogError($"Animation: the emote {emote.DisplayName} needs a trigger name of its own (has '{trigger}').");
                    continue;
                }

                controller.AddParameter(trigger, AnimatorControllerParameterType.Trigger);
                AnimatorState state = machine.AddState(trigger);
                state.motion = emote.Clip != null ? emote.Clip : LoadClip(IdleClipPath);

                AnimatorStateTransition start = machine.AddAnyStateTransition(state);
                start.AddCondition(AnimatorConditionMode.If, 0f, trigger);
                start.hasExitTime = false;
                start.canTransitionToSelf = true;

                state.AddTransition(empty, true);
                AnimatorStateTransition stop = state.AddTransition(empty);
                stop.AddCondition(AnimatorConditionMode.If, 0f, PlayerAnimatorParameters.EmoteStopName);
                stop.hasExitTime = false;
            }
        }

        // An emote's trigger must not clash with the controller's own parameters (Forward, Slap, ...).
        private static bool IsTaken(AnimatorController controller, string parameter) =>
            controller.parameters.Any(existing => existing.name == parameter);

        private static IReadOnlyList<EmoteEntry> LoadEmotes() =>
            AddressableConfigurationLoader.Load<EmoteCatalog>(Address.Configurations.EmoteCatalog_Default).Emotes;

        // The fourth layer, over the upper body (the legs keep walking or falling), at weight 1: Empty, a blend tree with
        // no motions, by default; the Slap trigger goes from Empty to Slap, Slap goes back to Empty at its exit time.
        // Both transitions have the editor's default exit time and duration.
        private static void AddSlapLayer(AnimatorController controller, AvatarMask upperBody) {
            controller.AddLayer(PlayerAnimatorParameters.SlapLayerName);
            AnimatorControllerLayer[] layers = controller.layers;
            int layerIndex = layers.Length - 1;
            AnimatorControllerLayer slapLayer = layers[layerIndex];
            slapLayer.defaultWeight = 1f;
            slapLayer.blendingMode = AnimatorLayerBlendingMode.Override;
            slapLayer.avatarMask = upperBody;
            controller.layers = layers;

            AnimatorStateMachine machine = slapLayer.stateMachine;
            AnimatorState empty = controller.CreateBlendTreeInController(EmptyState, out BlendTree _, layerIndex);
            AnimatorState slap = machine.AddState(PlayerAnimatorParameters.SlapStateName);
            slap.motion = SlapClipDraft.LoadOrCreate(SlapClipPath, LoadClip(IdleClipPath));
            machine.defaultState = empty;

            AnimatorStateTransition start = empty.AddTransition(slap);
            start.AddCondition(AnimatorConditionMode.If, 0f, PlayerAnimatorParameters.SlapName);
            start.hasExitTime = false;

            slap.AddTransition(empty, true);
        }

        // The upper body of a humanoid: the trunk, the head, the arms and the fingers; not the root and the legs.
        // Created once, then changed in place (the asset keeps its GUID).
        private static AvatarMask BuildUpperBodyMask() {
            AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(UpperBodyMaskPath);
            if (mask == null) {
                mask = new AvatarMask();
                AssetDatabase.CreateAsset(mask, UpperBodyMaskPath);
            }

            for (AvatarMaskBodyPart part = 0; part < AvatarMaskBodyPart.LastBodyPart; part++)
                mask.SetHumanoidBodyPartActive(part, IsUpperBody(part));

            EditorUtility.SetDirty(mask);
            AssetDatabase.SaveAssets();
            return mask;
        }

        private static bool IsUpperBody(AvatarMaskBodyPart part) =>
            part == AvatarMaskBodyPart.Body || part == AvatarMaskBodyPart.Head
            || part == AvatarMaskBodyPart.LeftArm || part == AvatarMaskBodyPart.RightArm
            || part == AvatarMaskBodyPart.LeftFingers || part == AvatarMaskBodyPart.RightFingers
            || part == AvatarMaskBodyPart.LeftHandIK || part == AvatarMaskBodyPart.RightHandIK;

        // The new controller's file takes the place of the old one's, whose meta (and GUID) stays; a controller is
        // always the file's main object, so references to it survive. The copied file still names its controller after
        // the temporary file, so the name is set to the file's again.
        private static void ReplaceController() {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) == null)
                AssetDatabase.MoveAsset(TemporaryPath, ControllerPath);
            else {
                File.Copy(FullPath(TemporaryPath), FullPath(ControllerPath), true);
                AssetDatabase.DeleteAsset(TemporaryPath);
                AssetDatabase.ImportAsset(ControllerPath, ImportAssetOptions.ForceUpdate);
            }

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            string fileName = Path.GetFileNameWithoutExtension(ControllerPath);
            if (controller == null || controller.name == fileName)
                return;

            controller.name = fileName;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
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
