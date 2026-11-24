using Features.ActiveRagdollModule;
using Features.ActiveRagdollModule.Editor;
using Features.CharacterMovableModule.Scripts;
using Features.FloatingControllerModule;
using UnityEditor;
using UnityEngine;

namespace Features.PlayerBodyModule.Editor {
    // Puts an asleep ragdoll on the humanoid inside the player prefab and wires the player's ragdoll body switch.
    // Running it again rebuilds the ragdoll.
    public static class PlayerRagdollBaker {
        private const string PlayerPrefabPath = "Assets/Features/PlayerModule/GameResources/Prefabs/DummyPlayer.prefab";
        private const string ProfileFolder = "Assets/Features/PlayerBodyModule/GameResources";
        private const string ProfilePath = ProfileFolder + "/PlayerRagdollProfile.asset";
        private const string ControllerPath =
            "Assets/Features/ActiveRagdollModule/GameResources/RagdollTestDummy.controller";

        [MenuItem("Tools/Ragdoll/Bake Ragdoll Into Player")]
        public static void Bake() {
            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                Debug.LogError("Ragdoll: leave Play mode before baking the player's ragdoll.");
                return;
            }

            GameObject player = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try {
                if (TryBake(player) == false)
                    return;

                PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("Ragdoll: baked into the player prefab, asleep until its owner wakes it.");
            } finally {
                PrefabUtility.UnloadPrefabContents(player);
            }
        }

        private static bool TryBake(GameObject player) {
            CharacterMovableBase movable = player.GetComponent<CharacterMovableBase>();
            Animator animator = FindHumanoid(player);
            if (movable == null || animator == null) {
                Debug.LogError("Ragdoll: the player prefab has no movement or no humanoid model.");
                return false;
            }

            GameObject model = animator.gameObject;
            RagdollBody body = model.GetComponent<RagdollBody>();
            if (body != null)
                RagdollBodyBaker.Clear(body);
            else
                body = model.AddComponent<RagdollBody>();

            SetReference(body, "_profile", LoadOrCreateProfile());
            AddPart<AnimatedPoseSampler>(model);
            SetReference(model.GetComponent<AnimatedPoseSampler>(), "_animator", animator);
            AddPart<AnimatedPoseFollower>(model);
            AddPart<RagdollGroundSensor>(model);
            AddPart<RagdollHover>(model);
            AddPart<RagdollGravity>(model);
            AddPart<RagdollDrag>(model);
            AddPart<RagdollCarrierRide>(model);
            AddPart<RagdollCarrierRideStepEnd>(model);
            AddPart<RagdollPoseGizmo>(model);

            HumanoidProfileFiller.Fill(body);
            RagdollBodyBaker.Bake(body);
            PutToSleep(body, animator);
            WireSwitch(player, movable, body);
            return true;
        }

        // Asleep in the prefab, so no peer runs it until the owner's switch wakes it.
        private static void PutToSleep(RagdollBody body, Animator animator) {
            foreach (Behaviour part in body.GetComponents<Behaviour>())
                if (part is RagdollBody || part.GetType().Namespace == typeof(RagdollBody).Namespace)
                    part.enabled = false;

            body.SetKinematic(true);
            body.SetInterpolation(false);
            foreach (RagdollLimb limb in body.Limbs)
                foreach (Collider limbCollider in limb.Colliders)
                    limbCollider.enabled = false;

            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            animator.applyRootMotion = false;
            animator.enabled = false;
        }

        private static void WireSwitch(GameObject player, CharacterMovableBase movable, RagdollBody body) {
            PlayerRagdollBody ragdollSwitch = player.GetComponent<PlayerRagdollBody>();
            if (ragdollSwitch == null)
                ragdollSwitch = player.AddComponent<PlayerRagdollBody>();

            SerializedObject serialized = new(ragdollSwitch);
            serialized.FindProperty("_movable").objectReferenceValue = movable;
            serialized.FindProperty("_capsule").objectReferenceValue = movable.Body;
            serialized.FindProperty("_capsuleCollider").objectReferenceValue = player.GetComponent<CapsuleCollider>();
            serialized.FindProperty("_floatingController").objectReferenceValue =
                player.GetComponent<FloatingController>();
            serialized.FindProperty("_ragdoll").objectReferenceValue = body;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Animator FindHumanoid(GameObject player) {
            foreach (Animator animator in player.GetComponentsInChildren<Animator>(true))
                if (animator.avatar != null && animator.avatar.isHuman)
                    return animator;
            return null;
        }

        private static void AddPart<T>(GameObject model) where T : Component {
            if (model.GetComponent<T>() == null)
                model.AddComponent<T>();
        }

        private static void SetReference(Object target, string field, Object value) {
            SerializedObject serialized = new(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RagdollBodyProfile LoadOrCreateProfile() {
            RagdollBodyProfile profile = AssetDatabase.LoadAssetAtPath<RagdollBodyProfile>(ProfilePath);
            if (profile != null)
                return profile;

            if (AssetDatabase.IsValidFolder(ProfileFolder) == false)
                AssetDatabase.CreateFolder("Assets/Features/PlayerBodyModule", "GameResources");
            profile = ScriptableObject.CreateInstance<RagdollBodyProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
            return profile;
        }
    }
}
