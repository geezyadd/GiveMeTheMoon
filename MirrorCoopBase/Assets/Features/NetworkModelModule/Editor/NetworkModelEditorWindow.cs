using System.Collections.Generic;
using System.IO;
using Features.NetworkModelModule.Scripts;
using Features.CodeGeneratorModule.CustomCodeGeneratorModule.Scripts.Editor.EditorWindow;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Features.NetworkModelModule.Scripts.Editor {
    public sealed class NetworkModelEditorWindow : EditorWindow {
        private const string DEFINITION_FOLDER = "Assets/Features/NetworkModelModule/GameResources/Definitions";
        private const string DEFAULT_OUTPUT = "Assets/Features/NetworkModelModule/Scripts/Generated";
        private const string DEFAULT_NAMESPACE = "Features.NetworkModelModule.Scripts.Generated";

        private static readonly string[] CommonTypes = {
            "bool",
            "int",
            "long",
            "float",
            "double",
            "string",
            "Vector2",
            "Vector3",
            "Quaternion",
            "Color",
            "Rect",
            "List<int>",
            "Dictionary<string, int>",
            "HashSet<int>",
            "NetworkIdentity",
            "GameObject",
            "uint"
        };

        private NetworkModelDefinition _selected;
        private SerializedObject _serialized;
        private string _status = string.Empty;
        private Vector2 _scroll;

        [MenuItem("Tools/Network Models")]
        private static void Open() =>
            GetWindow<NetworkModelEditorWindow>("Network Models");

        private void CreateGUI() {
            rootVisualElement.style.flexGrow = 1;
            IMGUIContainer container = new(DrawWindow) {
                style = { flexGrow = 1 }
            };
            rootVisualElement.Add(container);
        }

        private void DrawWindow() {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawToolbar();
            DrawDefinitionList();
            DrawInspector();
            if (string.IsNullOrEmpty(_status) == false)
                EditorGUILayout.HelpBox(_status, MessageType.Info);

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar() {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create"))
                CreateDefinition();

            if (GUILayout.Button("Generate All"))
                GenerateAll();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDefinitionList() {
            string[] guids = AssetDatabase.FindAssets("t:NetworkModelDefinition");
            EditorGUILayout.LabelField("Definitions", EditorStyles.boldLabel);
            if (guids.Length == 0)
                EditorGUILayout.LabelField("No definitions yet.");

            for (int i = 0; i < guids.Length; i++) {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                NetworkModelDefinition definition = AssetDatabase.LoadAssetAtPath<NetworkModelDefinition>(path);
                if (definition == null)
                    continue;

                string label = string.IsNullOrEmpty(definition.ModelName) ? definition.name : definition.ModelName;
                if (GUILayout.Button(label, _selected == definition ? EditorStyles.toolbarButton : EditorStyles.miniButton))
                    Select(definition);
            }
        }

        private void DrawInspector() {
            if (_selected == null) {
                EditorGUILayout.HelpBox("Select or create a definition.", MessageType.None);
                return;
            }

            if (_serialized == null || _serialized.targetObject != _selected)
                _serialized = new SerializedObject(_selected);

            _serialized.Update();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(_selected.name, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serialized.FindProperty("_modelName"), new GUIContent("Model Name"));
            EditorGUILayout.PropertyField(_serialized.FindProperty("_namespace"), new GUIContent("Namespace"));
            EditorGUILayout.PropertyField(_serialized.FindProperty("_folder"), new GUIContent("Folder"));
            EditorGUILayout.PropertyField(_serialized.FindProperty("_atomic"), new GUIContent("Atomic"));
            EditorGUILayout.PropertyField(_serialized.FindProperty("_scope"), new GUIContent("Scope"));
            DrawFields();
            _serialized.ApplyModifiedProperties();

            if (NetworkModelSpecBuilder.TryBuild(_selected, out _, out string error) == false)
                EditorGUILayout.HelpBox(error, MessageType.Error);
            else
                EditorGUILayout.HelpBox("Definition is valid.", MessageType.None);

            if (GUILayout.Button("Generate"))
                GenerateSelected();
        }

        private void DrawFields() {
            SerializedProperty fields = _serialized.FindProperty("_fields");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Fields", EditorStyles.boldLabel);
            int deleteIndex = -1;
            int moveFrom = -1;
            int moveTo = -1;
            for (int i = 0; i < fields.arraySize; i++) {
                SerializedProperty field = fields.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(field.FindPropertyRelative("_name"), new GUIContent("Name"));
                EditorGUILayout.PropertyField(field.FindPropertyRelative("_typeName"), new GUIContent("Type"));
                int typeIndex = EditorGUILayout.Popup("Type Picker", -1, CommonTypes);
                if (typeIndex >= 0)
                    field.FindPropertyRelative("_typeName").stringValue = CommonTypes[typeIndex];

                EditorGUILayout.PropertyField(field.FindPropertyRelative("_defaultValue"), new GUIContent("Default"));
                string typeName = field.FindPropertyRelative("_typeName").stringValue;
                string defaultValue = field.FindPropertyRelative("_defaultValue").stringValue;
                string fieldName = field.FindPropertyRelative("_name").stringValue;
                if (NetworkModelSpecBuilder.TryBuildField(fieldName, typeName, defaultValue, out _, out string fieldError) == false)
                    EditorGUILayout.HelpBox(fieldError, MessageType.Warning);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Up") && i > 0) {
                    moveFrom = i;
                    moveTo = i - 1;
                }

                if (GUILayout.Button("Down") && i < fields.arraySize - 1) {
                    moveFrom = i;
                    moveTo = i + 1;
                }

                if (GUILayout.Button("Remove"))
                    deleteIndex = i;

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            if (moveFrom >= 0)
                fields.MoveArrayElement(moveFrom, moveTo);

            if (deleteIndex >= 0)
                fields.DeleteArrayElementAtIndex(deleteIndex);

            if (GUILayout.Button("Add Field")) {
                fields.InsertArrayElementAtIndex(fields.arraySize);
                SerializedProperty created = fields.GetArrayElementAtIndex(fields.arraySize - 1);
                created.FindPropertyRelative("_name").stringValue = "Field" + fields.arraySize;
                created.FindPropertyRelative("_typeName").stringValue = "int";
                created.FindPropertyRelative("_defaultValue").stringValue = string.Empty;
            }
        }

        private void Select(NetworkModelDefinition definition) {
            _selected = definition;
            _serialized = null;
            _status = string.Empty;
        }

        private void CreateDefinition() {
            EnsureFolder(DEFINITION_FOLDER);
            string path = EditorUtility.SaveFilePanelInProject("Create Network Model", "NetworkModel", "asset", "Save the model definition", DEFINITION_FOLDER);
            if (string.IsNullOrEmpty(path))
                return;

            NetworkModelDefinition definition = CreateInstance<NetworkModelDefinition>();
            AssetDatabase.CreateAsset(definition, path);
            SerializedObject serialized = new SerializedObject(definition);
            serialized.FindProperty("_modelName").stringValue = Path.GetFileNameWithoutExtension(path);
            serialized.FindProperty("_namespace").stringValue = DEFAULT_NAMESPACE;
            serialized.FindProperty("_folder").stringValue = DEFAULT_OUTPUT;
            serialized.FindProperty("_atomic").boolValue = false;
            serialized.FindProperty("_scope").enumValueIndex = (int)NetworkModelScope.Shared;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Select(definition);
            _status = "Created " + path;
        }

        private void GenerateSelected() {
            if (_selected == null)
                return;

            _serialized?.ApplyModifiedProperties();
            if (NetworkModelSpecBuilder.TryBuild(_selected, out _, out string error) == false) {
                _status = error;
                return;
            }

            EditorUtility.SetDirty(_selected);
            AssetDatabase.SaveAssets();
            CodeGeneration.GenerateBySubGenerator(typeof(NetworkModelGenerator), new List<string> { _selected.ModelName });
            _status = "Generated " + _selected.ModelName + ".";
        }

        private void GenerateAll() {
            string[] guids = AssetDatabase.FindAssets("t:NetworkModelDefinition");
            List<string> names = new();
            for (int i = 0; i < guids.Length; i++) {
                NetworkModelDefinition definition = AssetDatabase.LoadAssetAtPath<NetworkModelDefinition>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (definition == null)
                    continue;

                if (NetworkModelSpecBuilder.TryBuild(definition, out _, out string error) == false) {
                    _status = definition.name + ": " + error;
                    return;
                }

                names.Add(definition.ModelName);
            }

            if (names.Count == 0) {
                _status = "Nothing to generate.";
                return;
            }

            AssetDatabase.SaveAssets();
            CodeGeneration.GenerateBySubGenerator(typeof(NetworkModelGenerator), names);
            _status = "Generated " + names.Count + " models.";
        }

        private static void EnsureFolder(string folder) {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            string leaf = Path.GetFileName(folder);
            if (string.IsNullOrEmpty(parent) == false && AssetDatabase.IsValidFolder(parent) == false)
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
