using System;
using System.Collections.Generic;
using Features.CodeGeneratorModule.CustomCodeGeneratorModule.Scripts.Editor.Core;
using Features.CodeGeneratorModule.CustomCodeGeneratorModule.Scripts.Editor.EditorWindow;
using UnityEditor;

namespace Features.NetworkModelModule.Scripts.Editor {
    [Generator(40)]
    public sealed class NetworkModelGenerator : ICodeGeneratorWithSubGenerators {
        public List<string> GetSubGeneratorNames() {
            NetworkModelDefinition[] definitions = LoadAll();
            List<string> names = new(definitions.Length);
            for (int i = 0; i < definitions.Length; i++) {
                string name = definitions[i].ModelName;
                if (string.IsNullOrEmpty(name))
                    name = definitions[i].name;

                names.Add(name);
            }

            return names;
        }

        public void ExecuteSubGeneratorByNames(GeneratorContext generatorContext, List<string> subGeneratorNames) {
            generatorContext.OverrideFolderPath("Assets");
            HashSet<string> selected = new(subGeneratorNames);
            NetworkModelDefinition[] definitions = LoadAll();
            for (int i = 0; i < definitions.Length; i++) {
                NetworkModelDefinition definition = definitions[i];
                if (selected.Contains(definition.ModelName) == false)
                    continue;

                if (NetworkModelSpecBuilder.TryBuild(definition, out NetworkModelSpec spec, out string error) == false)
                    throw new InvalidOperationException(definition.name + ": " + error);

                List<NetworkModelGeneratedFile> files = NetworkModelCodeEmitter.Emit(spec);
                for (int fileIndex = 0; fileIndex < files.Count; fileIndex++) {
                    NetworkModelGeneratedFile file = files[fileIndex];
                    generatorContext.AddCode(NetworkModelCodeEmitter.ContextFileName(spec.Folder, file.FileName), file.Text);
                }

                selected.Remove(definition.ModelName);
            }

            if (selected.Count > 0)
                throw new InvalidOperationException("Missing network model definition: " + string.Join(", ", selected));
        }

        private static NetworkModelDefinition[] LoadAll() {
            string[] guids = AssetDatabase.FindAssets("t:NetworkModelDefinition");
            List<NetworkModelDefinition> definitions = new(guids.Length);
            for (int i = 0; i < guids.Length; i++) {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                NetworkModelDefinition definition = AssetDatabase.LoadAssetAtPath<NetworkModelDefinition>(path);
                if (definition != null)
                    definitions.Add(definition);
            }

            return definitions.ToArray();
        }
    }
}
