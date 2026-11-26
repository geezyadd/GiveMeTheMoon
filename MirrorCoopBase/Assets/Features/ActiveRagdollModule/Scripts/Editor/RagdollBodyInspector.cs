using UnityEditor;
using UnityEngine;

namespace Features.ActiveRagdollModule.Editor {
    [CustomEditor(typeof(RagdollBody))]
    public sealed class RagdollBodyInspector : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            RagdollBody body = (RagdollBody)target;
            EditorGUILayout.Space();

            if (body.Profile == null) {
                EditorGUILayout.HelpBox("Assign a body profile (Create > Ragdoll > Body Profile).", MessageType.Info);
                return;
            }

            using (new EditorGUI.DisabledScope(Application.isPlaying)) {
                using (new EditorGUI.DisabledScope(HumanoidProfileFiller.CanFill(body) == false)) {
                    if (GUILayout.Button("Fill profile from Humanoid avatar"))
                        HumanoidProfileFiller.Fill(body);
                }

                using (new EditorGUI.DisabledScope(HumanoidProfileFiller.CanFill(body) == false)) {
                    if (GUILayout.Button("Fill Humanoid joint limits")) {
                        HumanoidProfileFiller.FillLimits(body);
                        RagdollJointLimitsBaker.ApplyAll(body);
                    }
                }

                if (GUILayout.Button("Apply joint limits"))
                    RagdollJointLimitsBaker.ApplyAll(body);
                if (GUILayout.Button("Bake limbs"))
                    RagdollBodyBaker.Bake(body);
                if (GUILayout.Button("Remove limbs"))
                    RagdollBodyBaker.Clear(body);
            }
        }
    }
}
