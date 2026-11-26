using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Features.PlayerAnimationModule.Editor {
    // Creates the player's slap clip once, when it does not exist yet: the idle's first Seconds with the right arm, the
    // shoulder and the trunk keyed as a slap (a wind-up to the right and back, an open swing across the front, a follow
    // through, then back to the idle). The keys are humanoid muscles, the only curves a humanoid clip plays. After that
    // the clip belongs to the Animation window: an existing clip is never touched.
    public static class SlapClipDraft {
        private const float Seconds = 0.9f;
        private const float SampleRate = 30f;

        // The wind-up, the hit and the follow-through; the clip's start and end keep the idle's own values.
        private static readonly float[] KeyTimes = { 0.22f, 0.36f, 0.5f };

        private static readonly Dictionary<string, float[]> KeyValues = new() {
            ["Right Shoulder Down-Up"] = new[] { 0.35f, 0.1f, 0f },
            ["Right Shoulder Front-Back"] = new[] { -0.3f, 0.4f, 0.5f },
            ["Right Arm Down-Up"] = new[] { 0f, 0.05f, -0.15f },
            ["Right Arm Front-Back"] = new[] { -0.3f, 0.85f, 1f },
            ["Right Arm Twist In-Out"] = new[] { 0.3f, -0.2f, -0.3f },
            ["Right Forearm Stretch"] = new[] { -0.3f, 0.9f, 0.7f },
            ["Right Hand Down-Up"] = new[] { 0.3f, -0.2f, -0.1f },
            ["Chest Twist Left-Right"] = new[] { 0.25f, -0.3f, -0.35f },
            ["Spine Twist Left-Right"] = new[] { 0.15f, -0.2f, -0.25f },
        };

        public static AnimationClip LoadOrCreate(string path, AnimationClip idle) {
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing != null || idle == null)
                return existing;

            AnimationClip clip = new() { frameRate = SampleRate };
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(idle);
            foreach (EditorCurveBinding binding in bindings) {
                AnimationCurve source = AnimationUtility.GetEditorCurve(idle, binding);
                AnimationCurve curve = KeyValues.TryGetValue(binding.propertyName, out float[] values)
                    ? Keyed(source, values)
                    : Trimmed(source);
                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }

            foreach (string muscle in KeyValues.Keys.Where(muscle => bindings.All(binding => binding.propertyName != muscle)))
                Debug.LogWarning($"Animation: the idle has no curve for the muscle {muscle}; the slap draft does not key it.");

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            settings.stopTime = Seconds;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, path);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static AnimationCurve Keyed(AnimationCurve source, float[] values) {
            AnimationCurve curve = new();
            curve.AddKey(0f, source.Evaluate(0f));
            for (int i = 0; i < KeyTimes.Length; i++)
                curve.AddKey(KeyTimes[i], values[i]);
            curve.AddKey(Seconds, source.Evaluate(Seconds));
            for (int i = 0; i < curve.length; i++) {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            }

            return curve;
        }

        private static AnimationCurve Trimmed(AnimationCurve source) {
            AnimationCurve curve = new();
            int frames = Mathf.RoundToInt(Seconds * SampleRate);
            for (int frame = 0; frame <= frames; frame++) {
                float time = frame / SampleRate;
                curve.AddKey(time, source.Evaluate(time));
            }

            return curve;
        }
    }
}
