using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Features.ActiveRagdollModule.Editor {
    // Test helper while the ragdoll is built on its own: puts a baked humanoid dummy into the lobby, scaled like the
    // humanoid inside the player prefab. Running it again replaces the old dummy.
    public static class LobbyTestDummyPlacer {
        private const string LobbyScenePath = "Assets/Features/GameCoreModule/GameResources/Scenes/LobbyScene.unity";
        private const string PlayerPrefabPath = "Assets/Features/PlayerModule/GameResources/Prefabs/DummyPlayer.prefab";
        private const string HumanoidPrefabPath = "Assets/Global/Packages/HumanoidDemo/Prefabs/humanoid demo.prefab";
        private const string ProfileFolder = "Assets/Features/ActiveRagdollModule/GameResources";
        private const string ProfilePath = ProfileFolder + "/HumanoidRagdollProfile.asset";
        private const string DummyName = "RagdollTestDummy";
        private static readonly Vector3 OffsetFromSpawn = new(0f, 1f, 2f);

        [MenuItem("Tools/Ragdoll/Place Test Dummy In Lobby")]
        public static void Place() {
            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                Debug.LogError("Ragdoll: leave Play mode before placing the test dummy.");
                return;
            }

            Scene lobby = SceneManager.GetSceneByPath(LobbyScenePath);
            bool openedHere = lobby.isLoaded == false;
            if (openedHere)
                lobby = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Additive);

            // Never save a lobby that did not load in full: that would wipe the scene.
            if (HasSceneContent(lobby) == false) {
                Debug.LogError("Ragdoll: the lobby scene looks empty, nothing was changed.");
                if (openedHere)
                    EditorSceneManager.CloseScene(lobby, true);
                return;
            }

            RemoveOldDummy(lobby);
            RagdollBody body = CreateDummy(lobby);
            HumanoidProfileFiller.Fill(body);
            RagdollBodyBaker.Bake(body);

            EditorSceneManager.MarkSceneDirty(lobby);
            EditorSceneManager.SaveScene(lobby);
            if (openedHere)
                EditorSceneManager.CloseScene(lobby, true);

            AssetDatabase.SaveAssets();
            Debug.Log($"Ragdoll: {DummyName} placed in the lobby with {body.Limbs.Count} limbs.");
        }

        private static RagdollBody CreateDummy(Scene lobby) {
            GameObject root = new(DummyName);
            SceneManager.MoveGameObjectToScene(root, lobby);
            root.transform.position = SpawnPosition(lobby) + OffsetFromSpawn;

            GameObject humanoidPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HumanoidPrefabPath);
            GameObject humanoid = (GameObject)PrefabUtility.InstantiatePrefab(humanoidPrefab, root.transform);
            humanoid.transform.localPosition = Vector3.zero;
            humanoid.transform.localRotation = Quaternion.identity;
            humanoid.transform.localScale = PlayerHumanoidScale();

            Animator animator = humanoid.GetComponentInChildren<Animator>();
            animator.runtimeAnimatorController = TestDummyAnimatorBuilder.Build();
            animator.applyRootMotion = false;

            RagdollBody body = root.AddComponent<RagdollBody>();
            root.AddComponent<AnimatedPoseSampler>();
            root.AddComponent<AnimatedPoseFollower>();
            root.AddComponent<RagdollGroundSensor>();
            root.AddComponent<RagdollHover>();
            root.AddComponent<RagdollGravity>();
            root.AddComponent<RagdollDrag>();
            root.AddComponent<RagdollPoseGizmo>();
            root.AddComponent<RagdollRootPin>();
            SerializedObject serializedBody = new(body);
            serializedBody.FindProperty("_profile").objectReferenceValue = LoadOrCreateProfile();
            serializedBody.ApplyModifiedPropertiesWithoutUndo();
            return body;
        }

        private static bool HasSceneContent(Scene lobby) {
            if (lobby.IsValid() == false || lobby.isLoaded == false)
                return false;

            foreach (GameObject rootObject in lobby.GetRootGameObjects())
                if (rootObject.name != DummyName)
                    return true;
            return false;
        }

        private static void RemoveOldDummy(Scene lobby) {
            foreach (GameObject rootObject in lobby.GetRootGameObjects())
                if (rootObject.name == DummyName)
                    Object.DestroyImmediate(rootObject);
        }

        // The humanoid's world scale inside the player prefab, so the dummy is exactly the player's size.
        private static Vector3 PlayerHumanoidScale() {
            GameObject player = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try {
                Animator animator = player.GetComponentInChildren<Animator>(true);
                if (animator != null && animator.isHuman)
                    return animator.transform.lossyScale;

                Debug.LogWarning("Ragdoll: no humanoid in the player prefab, the dummy keeps scale 1.");
                return Vector3.one;
            } finally {
                PrefabUtility.UnloadPrefabContents(player);
            }
        }

        // The first network start position in the lobby (looked up by name, so this editor code needs no Mirror),
        // or the scene origin.
        private static Vector3 SpawnPosition(Scene lobby) {
            foreach (GameObject rootObject in lobby.GetRootGameObjects())
                foreach (Component component in rootObject.GetComponentsInChildren<Component>(true))
                    if (component != null && component.GetType().Name == "NetworkStartPosition")
                        return component.transform.position;

            return Vector3.zero;
        }

        private static RagdollBodyProfile LoadOrCreateProfile() {
            RagdollBodyProfile profile = AssetDatabase.LoadAssetAtPath<RagdollBodyProfile>(ProfilePath);
            if (profile != null)
                return profile;

            if (AssetDatabase.IsValidFolder(ProfileFolder) == false)
                AssetDatabase.CreateFolder("Assets/Features/ActiveRagdollModule", "GameResources");
            profile = ScriptableObject.CreateInstance<RagdollBodyProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
            return profile;
        }
    }
}
