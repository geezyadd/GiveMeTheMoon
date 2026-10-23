using Features.ShipModule.Scripts;
using Features.StagePresetModule.Scripts.Configurations;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Features.StagePresetModule.Editor.Tests {
    // The shipped assets the tests read, and presets built in memory for the failure cases.
    public static class StagePresetTestAssets {
        private const string SHIP_PLATFORM_PATH = "Assets/Features/ShipModule/GameResources/Prefabs/ShipPlatform.prefab";

        public static ShipBase LoadShipPlatform() {
            GameObject ship = AssetDatabase.LoadAssetAtPath<GameObject>(SHIP_PLATFORM_PATH);
            Assert.IsNotNull(ship, $"No ship prefab at {SHIP_PLATFORM_PATH}.");
            return ship.GetComponent<ShipBase>();
        }

        public static ShipSocket[] LoadShipPlatformSockets() =>
            LoadShipPlatform().GetComponentsInChildren<ShipSocket>(true);

        public static StagePreset CreatePreset(string id, int startLoopIndex, long startingBalance, params (string socketId, ModuleDefinition module)[] modules) {
            StagePreset preset = ScriptableObject.CreateInstance<StagePreset>();
            preset.name = id;
            SerializedObject serializedPreset = new SerializedObject(preset);
            serializedPreset.FindProperty("_id").stringValue = id;
            serializedPreset.FindProperty("_displayName").stringValue = id;
            serializedPreset.FindProperty("_startLoopIndex").intValue = startLoopIndex;
            serializedPreset.FindProperty("_startingBalance").longValue = startingBalance;
            SerializedProperty entries = serializedPreset.FindProperty("_modules");
            entries.arraySize = modules.Length;
            for (int i = 0; i < modules.Length; i++) {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_socketId").stringValue = modules[i].socketId;
                entry.FindPropertyRelative("_module").objectReferenceValue = modules[i].module;
            }

            serializedPreset.ApplyModifiedPropertiesWithoutUndo();
            return preset;
        }

        public static void SetFieldProfile(StagePreset preset, string profileId) =>
            SetString(preset, "_fieldProfileId", profileId);

        public static void SetExtensions(StagePreset preset, params ScriptableObject[] extensions) {
            SerializedObject serializedPreset = new SerializedObject(preset);
            SerializedProperty property = serializedPreset.FindProperty("_extensions");
            property.arraySize = extensions.Length;
            for (int i = 0; i < extensions.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = extensions[i];

            serializedPreset.ApplyModifiedPropertiesWithoutUndo();
        }

        public static StagePresetCatalog CreateCatalog(params StagePreset[] presets) {
            StagePresetCatalog catalog = ScriptableObject.CreateInstance<StagePresetCatalog>();
            catalog.name = "TestStagePresetCatalog";
            SerializedObject serializedCatalog = new SerializedObject(catalog);
            SerializedProperty property = serializedCatalog.FindProperty("_presets");
            property.arraySize = presets.Length;
            for (int i = 0; i < presets.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = presets[i];

            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }

        public static ModuleDefinition CreateModule(string id, ShipModuleType slotType) {
            ModuleDefinition module = ScriptableObject.CreateInstance<ModuleDefinition>();
            module.name = id;
            SerializedObject serializedModule = new SerializedObject(module);
            serializedModule.FindProperty("_id").stringValue = id;
            serializedModule.FindProperty("_slotType").enumValueIndex = (int)slotType;
            serializedModule.ApplyModifiedPropertiesWithoutUndo();
            return module;
        }

        private static void SetString(Object target, string property, string value) {
            SerializedObject serializedTarget = new SerializedObject(target);
            serializedTarget.FindProperty(property).stringValue = value;
            serializedTarget.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
