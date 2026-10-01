using System.Collections.Generic;
using Features.NetworkModelModule.Scripts;

namespace Features.NetworkModelModule.Scripts.Editor {
    public static class NetworkModelSpecBuilder {
        public static bool TryBuild(NetworkModelDefinition definition, out NetworkModelSpec spec, out string error) {
            spec = null;
            error = null;
            if (definition == null) {
                error = "Definition is missing.";
                return false;
            }

            if (NetworkModelTypeResolver.TryMakeTypeName(definition.ModelName, out string modelName, out error) == false)
                return false;

            string namespaceName = definition.Namespace == null ? string.Empty : definition.Namespace.Trim();
            if (IsNamespace(namespaceName) == false) {
                error = "Namespace must be a dotted C# namespace.";
                return false;
            }

            string folder = definition.Folder == null ? string.Empty : definition.Folder.Replace('\\', '/').Trim().TrimEnd('/');
            if (folder.StartsWith("Assets/") == false || folder.Contains("..")) {
                error = "Folder must be inside Assets/.";
                return false;
            }

            IReadOnlyList<NetworkModelDefinition.Field> source = definition.Fields;
            if (source == null || source.Count == 0) {
                error = "Add at least one field.";
                return false;
            }

            List<NetworkModelResolvedField> fields = new(source.Count);
            HashSet<string> names = new();
            bool hasScalar = false;
            for (int i = 0; i < source.Count; i++) {
                if (TryBuildField(source[i], out NetworkModelResolvedField field, out error) == false)
                    return false;

                if (names.Add(field.PropertyName) == false) {
                    error = "Duplicate field " + field.PropertyName + ".";
                    return false;
                }

                if (field.Kind == NetworkModelFieldKind.Scalar)
                    hasScalar = true;

                fields.Add(field);
            }

            if (definition.Atomic && hasScalar == false) {
                error = "Atomic models need at least one scalar field.";
                return false;
            }

            spec = new NetworkModelSpec {
                ModelName = modelName,
                Namespace = namespaceName,
                Folder = folder,
                Atomic = definition.Atomic,
                Scope = definition.Scope,
                Fields = fields
            };
            return true;
        }

        public static bool TryBuildField(string name, string typeName, string defaultValue, out NetworkModelResolvedField field, out string error) {
            field = null;
            if (NetworkModelTypeResolver.TryMakeTypeName(name, out string propertyName, out error) == false)
                return false;

            if (NetworkModelTypeResolver.TryResolveType(typeName, out ResolvedNetworkType resolved, out error) == false)
                return false;

            if (NetworkModelTypeResolver.TryValidateDefault(resolved, defaultValue, out string expression, out error) == false)
                return false;

            field = new NetworkModelResolvedField {
                PropertyName = propertyName,
                FieldName = NetworkModelTypeResolver.ToFieldName(propertyName),
                Kind = resolved.Kind,
                TypeName = resolved.TypeName,
                KeyTypeName = resolved.KeyTypeName,
                ValueTypeName = resolved.ValueTypeName,
                DefaultExpression = expression,
                NeedsUnityEngine = resolved.NeedsUnityEngine,
                NeedsMirror = resolved.NeedsMirror
            };
            return true;
        }

        private static bool TryBuildField(NetworkModelDefinition.Field source, out NetworkModelResolvedField field, out string error) {
            field = null;
            if (source == null) {
                error = "A field entry is missing.";
                return false;
            }

            return TryBuildField(source.Name, source.TypeName, source.DefaultValue, out field, out error);
        }

        private static bool IsNamespace(string namespaceName) {
            if (namespaceName.Length == 0)
                return false;

            string[] parts = namespaceName.Split('.');
            for (int i = 0; i < parts.Length; i++) {
                if (NetworkModelTypeResolver.TryMakeTypeName(parts[i], out string part, out _) == false || part != parts[i])
                    return false;
            }

            return true;
        }
    }
}
