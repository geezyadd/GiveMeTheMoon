using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Features.NetworkModelModule.Scripts.Editor {
    public static class NetworkModelTypeResolver {
        private static readonly Dictionary<string, Alias> Aliases = BuildAliases();
        private static readonly HashSet<string> Keywords = BuildKeywords();
        private static readonly HashSet<string> ReservedProperties = new() {
            "State",
            "Model",
            "IsAvailable",
            "OnChanged",
            "OnAvailableChanged"
        };

        private static readonly Dictionary<string, List<Type>> TypeCache = new(StringComparer.Ordinal);

        public static bool TryMakeTypeName(string raw, out string typeName, out string error) {
            typeName = null;
            error = null;
            if (string.IsNullOrWhiteSpace(raw)) {
                error = "Name is empty.";
                return false;
            }

            string trimmed = raw.Trim();
            if (trimmed.Length > 0 && char.IsLetter(trimmed[0]) && char.IsLower(trimmed[0]))
                trimmed = char.ToUpperInvariant(trimmed[0]) + trimmed.Substring(1);

            if (IsIdentifier(trimmed) == false) {
                error = "Name must be a C# identifier.";
                return false;
            }

            if (Keywords.Contains(trimmed)) {
                error = "Name is a C# keyword.";
                return false;
            }

            if (ReservedProperties.Contains(trimmed)) {
                error = "Name is reserved.";
                return false;
            }

            typeName = trimmed;
            return true;
        }

        public static bool TryResolveType(string raw, out ResolvedNetworkType resolved, out string error) {
            resolved = null;
            error = null;
            if (string.IsNullOrWhiteSpace(raw)) {
                error = "Type is empty.";
                return false;
            }

            string typeName = RemoveWhitespace(raw);
            if (TryParseGeneric(typeName, out string genericName, out List<string> arguments))
                return TryResolveGeneric(genericName, arguments, out resolved, out error);

            return TryResolveScalar(typeName, out resolved, out error);
        }

        public static bool TryValidateDefault(ResolvedNetworkType resolved, string raw, out string expression, out string error) {
            expression = null;
            error = null;
            if (string.IsNullOrWhiteSpace(raw))
                return true;

            if (resolved.Kind != NetworkModelFieldKind.Scalar) {
                error = "Collections start empty. Leave the default blank.";
                return false;
            }

            string text = raw.Trim();
            if (text.IndexOf(';') >= 0 || text.IndexOf('{') >= 0 || text.IndexOf('}') >= 0 || text.IndexOf('\n') >= 0 || text.IndexOf('\r') >= 0) {
                error = "Default value cannot contain statements.";
                return false;
            }

            if (resolved.RuntimeType != null && resolved.RuntimeType.IsEnum)
                return TryValidateEnumDefault(resolved, text, out expression, out error);

            switch (resolved.TypeName) {
                case "bool":
                    if (text == "true" || text == "false") {
                        expression = text;
                        return true;
                    }

                    error = "Bool default must be true or false.";
                    return false;
                case "string":
                    expression = QuoteString(text);
                    return true;
                case "char":
                    return TryValidateCharDefault(text, out expression, out error);
                case "float":
                    return TryValidateNumberDefault(text, "f", true, out expression, out error);
                case "double":
                    return TryValidateNumberDefault(text, "d", true, out expression, out error);
                case "decimal":
                    return TryValidateNumberDefault(text, "m", true, out expression, out error);
                case "byte":
                case "short":
                case "ushort":
                case "int":
                case "uint":
                case "long":
                case "ulong":
                case "sbyte":
                    return TryValidateIntegerDefault(resolved.TypeName, text, out expression, out error);
                default:
                    if (text == "default" || text.StartsWith("new ", StringComparison.Ordinal) || text.IndexOf('.') >= 0) {
                        expression = text;
                        return true;
                    }

                    error = "Default must be empty, default, or an expression such as new Type(...).";
                    return false;
            }
        }

        public static string ToFieldName(string propertyName) =>
            "_" + char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);

        private static bool TryResolveGeneric(string genericName, List<string> arguments, out ResolvedNetworkType resolved, out string error) {
            resolved = null;
            error = null;
            if (genericName == "List" && arguments.Count == 1)
                return TryResolveSingleCollection(NetworkModelFieldKind.List, arguments[0], out resolved, out error);

            if (genericName == "HashSet" && arguments.Count == 1)
                return TryResolveSingleCollection(NetworkModelFieldKind.HashSet, arguments[0], out resolved, out error);

            if (genericName == "Dictionary" && arguments.Count == 2)
                return TryResolveDictionary(arguments[0], arguments[1], out resolved, out error);

            error = "Unsupported type " + genericName + ". Use a scalar, List<T>, Dictionary<K,V> or HashSet<T>.";
            return false;
        }

        private static bool TryResolveSingleCollection(NetworkModelFieldKind kind, string elementRaw, out ResolvedNetworkType resolved, out string error) {
            resolved = null;
            if (TryResolveScalar(RemoveWhitespace(elementRaw), out ResolvedNetworkType element, out error) == false)
                return false;

            resolved = new ResolvedNetworkType {
                Kind = kind,
                TypeName = element.TypeName,
                NeedsUnityEngine = element.NeedsUnityEngine,
                NeedsMirror = element.NeedsMirror
            };
            return true;
        }

        private static bool TryResolveDictionary(string keyRaw, string valueRaw, out ResolvedNetworkType resolved, out string error) {
            resolved = null;
            if (TryResolveScalar(RemoveWhitespace(keyRaw), out ResolvedNetworkType key, out error) == false)
                return false;

            if (TryResolveScalar(RemoveWhitespace(valueRaw), out ResolvedNetworkType value, out error) == false)
                return false;

            resolved = new ResolvedNetworkType {
                Kind = NetworkModelFieldKind.Dictionary,
                KeyTypeName = key.TypeName,
                ValueTypeName = value.TypeName,
                NeedsUnityEngine = key.NeedsUnityEngine || value.NeedsUnityEngine,
                NeedsMirror = key.NeedsMirror || value.NeedsMirror
            };
            return true;
        }

        private static bool TryResolveScalar(string typeName, out ResolvedNetworkType resolved, out string error) {
            resolved = null;
            error = null;
            if (Aliases.TryGetValue(typeName, out Alias alias)) {
                resolved = alias.ToResolved();
                return true;
            }

            if (TryParseGeneric(typeName, out _, out _)) {
                error = "Nested collections are not supported. Use a serializable struct.";
                return false;
            }

            List<Type> matches = FindTypes(typeName);
            if (matches.Count == 0) {
                error = "Unknown type " + typeName + ".";
                return false;
            }

            if (matches.Count > 1) {
                error = "Type " + typeName + " is ambiguous.";
                return false;
            }

            Type type = matches[0];
            if (IsSupportedCustomType(type, out error) == false)
                return false;

            resolved = new ResolvedNetworkType {
                Kind = NetworkModelFieldKind.Scalar,
                TypeName = "global::" + type.FullName.Replace('+', '.'),
                NeedsUnityEngine = false,
                NeedsMirror = false,
                RuntimeType = type
            };
            return true;
        }

        private static bool IsSupportedCustomType(Type type, out string error) {
            error = null;
            if (type.IsArray || type.IsInterface || type.IsGenericTypeDefinition || type.ContainsGenericParameters) {
                error = "Type " + type.Name + " cannot be synced.";
                return false;
            }

            if (type.IsAbstract && type.IsClass) {
                error = "Type " + type.Name + " is abstract.";
                return false;
            }

            if (typeof(Delegate).IsAssignableFrom(type)) {
                error = "Delegates cannot be synced.";
                return false;
            }

            if (IsNetworkBehaviour(type)) {
                error = "NetworkBehaviour references are not synced. Use NetworkIdentity or a uint netId.";
                return false;
            }

            if (IsUnityObject(type)) {
                error = "UnityEngine.Object references are not synced. Use NetworkIdentity, GameObject or a plain struct.";
                return false;
            }

            return true;
        }

        private static bool TryValidateEnumDefault(ResolvedNetworkType resolved, string text, out string expression, out string error) {
            expression = null;
            error = null;
            if (text == "default") {
                expression = "default";
                return true;
            }

            string name = text;
            int dot = text.LastIndexOf('.');
            if (dot >= 0 && dot < text.Length - 1)
                name = text.Substring(dot + 1);

            if (Enum.IsDefined(resolved.RuntimeType, name) == false) {
                error = "Unknown enum value " + text + ".";
                return false;
            }

            expression = resolved.TypeName + "." + name;
            return true;
        }

        private static bool TryValidateCharDefault(string text, out string expression, out string error) {
            expression = null;
            error = null;
            if (text.Length == 3 && text[0] == '\'' && text[2] == '\'') {
                expression = text;
                return true;
            }

            if (text.Length == 1) {
                expression = "'" + text + "'";
                return true;
            }

            error = "Char default must be one character.";
            return false;
        }

        private static bool TryValidateIntegerDefault(string typeName, string text, out string expression, out string error) {
            expression = null;
            error = null;
            bool unsigned = typeName == "byte" || typeName == "ushort" || typeName == "uint" || typeName == "ulong";
            if (unsigned && text.StartsWith("-", StringComparison.Ordinal)) {
                error = "Default is out of range for " + typeName + ".";
                return false;
            }

            string digits = text;
            if (digits.EndsWith("L", StringComparison.Ordinal) || digits.EndsWith("l", StringComparison.Ordinal) || digits.EndsWith("U", StringComparison.Ordinal) || digits.EndsWith("u", StringComparison.Ordinal))
                digits = digits.TrimEnd('L', 'l', 'U', 'u');

            if (ulong.TryParse(digits.TrimStart('-'), out _) == false) {
                error = "Default is not an integer.";
                return false;
            }

            expression = text;
            return true;
        }

        private static bool TryValidateNumberDefault(string text, string suffix, bool allowDecimal, out string expression, out string error) {
            expression = null;
            error = null;
            string body = text;
            if (body.Length > 0 && char.IsLetter(body[body.Length - 1])) {
                char mark = char.ToLowerInvariant(body[body.Length - 1]);
                if (mark.ToString() != suffix) {
                    error = "Numeric suffix does not match the type.";
                    return false;
                }

                body = body.Substring(0, body.Length - 1);
            }

            if (double.TryParse(body, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _) == false) {
                error = "Default is not a number.";
                return false;
            }

            if (allowDecimal == false && body.IndexOf('.') >= 0) {
                error = "Default is not an integer.";
                return false;
            }

            expression = body + suffix;
            return true;
        }

        private static string QuoteString(string text) {
            if (text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"')
                return text;

            StringBuilder builder = new StringBuilder(text.Length + 2);
            builder.Append('"');
            for (int i = 0; i < text.Length; i++) {
                char character = text[i];
                if (character == '\\' || character == '"')
                    builder.Append('\\');

                builder.Append(character);
            }

            builder.Append('"');
            return builder.ToString();
        }

        private static bool TryParseGeneric(string typeName, out string genericName, out List<string> arguments) {
            genericName = null;
            arguments = null;
            int open = typeName.IndexOf('<');
            if (open <= 0 || typeName.EndsWith(">", StringComparison.Ordinal) == false)
                return false;

            genericName = typeName.Substring(0, open);
            string inner = typeName.Substring(open + 1, typeName.Length - open - 2);
            arguments = SplitArguments(inner);
            return arguments.Count > 0;
        }

        private static List<string> SplitArguments(string inner) {
            List<string> arguments = new();
            int depth = 0;
            int start = 0;
            for (int i = 0; i < inner.Length; i++) {
                char character = inner[i];
                if (character == '<')
                    depth++;
                else if (character == '>')
                    depth--;
                else if (character == ',' && depth == 0) {
                    arguments.Add(inner.Substring(start, i - start));
                    start = i + 1;
                }
            }

            if (start < inner.Length)
                arguments.Add(inner.Substring(start));

            return arguments;
        }

        private static string RemoveWhitespace(string raw) {
            StringBuilder builder = new(raw.Length);
            for (int i = 0; i < raw.Length; i++) {
                if (char.IsWhiteSpace(raw[i]) == false)
                    builder.Append(raw[i]);
            }

            return builder.ToString();
        }

        private static bool IsIdentifier(string text) {
            if (text.Length == 0 || (char.IsLetter(text[0]) == false && text[0] != '_'))
                return false;

            for (int i = 1; i < text.Length; i++) {
                if (char.IsLetterOrDigit(text[i]) == false && text[i] != '_')
                    return false;
            }

            return true;
        }

        private static List<Type> FindTypes(string name) {
            if (TypeCache.TryGetValue(name, out List<Type> cached))
                return cached;

            List<Type> matches = new();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++) {
                Assembly assembly = assemblies[i];
                if (assembly.IsDynamic)
                    continue;

                Type[] types = GetTypes(assembly);
                for (int typeIndex = 0; typeIndex < types.Length; typeIndex++) {
                    Type type = types[typeIndex];
                    if (type == null || type.IsPublic == false || type.FullName == null)
                        continue;

                    if (type.Name == name || type.FullName == name)
                        matches.Add(type);
                }
            }

            TypeCache[name] = matches;
            return matches;
        }

        private static Type[] GetTypes(Assembly assembly) {
            try {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception) {
                return exception.Types ?? Array.Empty<Type>();
            }
        }

        private static bool IsNetworkBehaviour(Type type) {
            Type current = type;
            while (current != null) {
                if (current.FullName == "Mirror.NetworkBehaviour")
                    return true;

                current = current.BaseType;
            }

            return false;
        }

        private static bool IsUnityObject(Type type) {
            Type current = type;
            while (current != null) {
                if (current.FullName == "UnityEngine.Object")
                    return true;

                current = current.BaseType;
            }

            return false;
        }

        private static Dictionary<string, Alias> BuildAliases() {
            Dictionary<string, Alias> aliases = new(StringComparer.Ordinal);
            AddPrimitive(aliases, "bool", "bool", typeof(bool), "Boolean", "System.Boolean");
            AddPrimitive(aliases, "byte", "byte", typeof(byte), "Byte", "System.Byte");
            AddPrimitive(aliases, "sbyte", "sbyte", typeof(sbyte), "SByte", "System.SByte");
            AddPrimitive(aliases, "short", "short", typeof(short), "Int16", "System.Int16");
            AddPrimitive(aliases, "ushort", "ushort", typeof(ushort), "UInt16", "System.UInt16");
            AddPrimitive(aliases, "int", "int", typeof(int), "Int32", "System.Int32");
            AddPrimitive(aliases, "uint", "uint", typeof(uint), "UInt32", "System.UInt32");
            AddPrimitive(aliases, "long", "long", typeof(long), "Int64", "System.Int64");
            AddPrimitive(aliases, "ulong", "ulong", typeof(ulong), "UInt64", "System.UInt64");
            AddPrimitive(aliases, "float", "float", typeof(float), "Single", "System.Single");
            AddPrimitive(aliases, "double", "double", typeof(double), "Double", "System.Double");
            AddPrimitive(aliases, "decimal", "decimal", typeof(decimal), "Decimal", "System.Decimal");
            AddPrimitive(aliases, "char", "char", typeof(char), "Char", "System.Char");
            AddPrimitive(aliases, "string", "string", typeof(string), "String", "System.String");
            AddUnity(aliases, "Vector2");
            AddUnity(aliases, "Vector3");
            AddUnity(aliases, "Vector4");
            AddUnity(aliases, "Vector2Int");
            AddUnity(aliases, "Vector3Int");
            AddUnity(aliases, "Quaternion");
            AddUnity(aliases, "Color");
            AddUnity(aliases, "Color32");
            AddUnity(aliases, "Rect");
            aliases["GameObject"] = new Alias("GameObject", true, false, typeof(UnityEngine.GameObject));
            aliases["UnityEngine.GameObject"] = aliases["GameObject"];
            aliases["NetworkIdentity"] = new Alias("NetworkIdentity", false, true, null);
            aliases["Mirror.NetworkIdentity"] = aliases["NetworkIdentity"];
            return aliases;
        }

        private static void AddPrimitive(Dictionary<string, Alias> aliases, string emitted, string key, Type type, string shortName, string fullName) {
            Alias alias = new Alias(emitted, false, false, type);
            aliases[key] = alias;
            aliases[shortName] = alias;
            aliases[fullName] = alias;
        }

        private static void AddUnity(Dictionary<string, Alias> aliases, string name) {
            Alias alias = new Alias(name, true, false, null);
            aliases[name] = alias;
            aliases["UnityEngine." + name] = alias;
        }

        private static HashSet<string> BuildKeywords() {
            string[] words = {
                "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
                "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
                "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
                "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
                "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
                "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof",
                "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
            };
            HashSet<string> keywords = new(StringComparer.Ordinal);
            for (int i = 0; i < words.Length; i++)
                keywords.Add(words[i]);

            return keywords;
        }

        private sealed class Alias {
            private readonly string _emitted;
            private readonly bool _needsUnityEngine;
            private readonly bool _needsMirror;
            private readonly Type _runtimeType;

            public Alias(string emitted, bool needsUnityEngine, bool needsMirror, Type runtimeType) {
                _emitted = emitted;
                _needsUnityEngine = needsUnityEngine;
                _needsMirror = needsMirror;
                _runtimeType = runtimeType;
            }

            public ResolvedNetworkType ToResolved() =>
                new() {
                    Kind = NetworkModelFieldKind.Scalar,
                    TypeName = _emitted,
                    NeedsUnityEngine = _needsUnityEngine,
                    NeedsMirror = _needsMirror,
                    RuntimeType = _runtimeType
                };
        }
    }
}
