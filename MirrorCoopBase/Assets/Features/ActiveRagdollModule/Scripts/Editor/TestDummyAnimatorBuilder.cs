using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Features.ActiveRagdollModule.Editor {
    // A throwaway animator for the test dummy: one blend by "Forward" from idle (0) to walking forward (1).
    // The real locomotion controller is built later.
    public static class TestDummyAnimatorBuilder {
        public const string ForwardParameter = "Forward";

        private const string ControllerPath =
            "Assets/Features/ActiveRagdollModule/GameResources/RagdollTestDummy.controller";
        private const string ClipsFolder = "Assets/Global/Packages/KevinIglesias/HumanAnimations/Animations/Male";
        private const string IdleClipPath = ClipsFolder + "/Idles/HumanM@Idle01.fbx";
        private const string WalkClipPath = ClipsFolder + "/Movement/Walk/HumanM@Walk01_Forward.fbx";

        public static AnimatorController Build() {
            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter(ForwardParameter, AnimatorControllerParameterType.Float);

            controller.CreateBlendTreeInController("Locomotion", out BlendTree tree);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = ForwardParameter;
            tree.useAutomaticThresholds = false;
            tree.AddChild(LoadClip(IdleClipPath), 0f);
            tree.AddChild(LoadClip(WalkClipPath), 1f);

            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimationClip LoadClip(string path) {
            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .FirstOrDefault(candidate => candidate.name.StartsWith("__preview__") == false);
            if (clip == null)
                Debug.LogError($"Ragdoll: no animation clip in {path}.");
            return clip;
        }
    }
}
