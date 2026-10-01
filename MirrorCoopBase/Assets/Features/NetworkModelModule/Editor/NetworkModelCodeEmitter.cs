using System;
using System.Collections.Generic;
using Features.NetworkModelModule.Scripts;

namespace Features.NetworkModelModule.Scripts.Editor {
    public static class NetworkModelCodeEmitter {
        private const string BASE_NAMESPACE = "Features.NetworkModelModule.Scripts";

        public static List<NetworkModelGeneratedFile> Emit(NetworkModelSpec spec) {
            List<NetworkModelGeneratedFile> files = new() {
                File("IReadOnly" + spec.ModelName + "Model_g.cs", EmitInterface(spec)),
                File(spec.ModelName + "Model_g.cs", EmitModel(spec)),
                File(spec.ModelName + "Bridge_g.cs", EmitBridge(spec)),
                File(spec.ModelName + "ModelInstaller_g.cs", EmitInstaller(spec))
            };

            if (spec.Atomic)
                files.Add(File(spec.ModelName + "State_g.cs", EmitState(spec)));

            if (spec.Scope == NetworkModelScope.PerPlayer) {
                files.Add(File("IReadOnly" + spec.ModelName + "Registry_g.cs", EmitRegistryInterface(spec)));
                files.Add(File(spec.ModelName + "Registry_g.cs", EmitRegistry(spec)));
            }

            return files;
        }

        public static string ContextFileName(string folder, string fileName) {
            string path = folder.Replace('\\', '/').Trim('/');
            const string ASSETS_PREFIX = "Assets/";
            if (path.StartsWith(ASSETS_PREFIX, StringComparison.Ordinal))
                path = path.Substring(ASSETS_PREFIX.Length);
            else if (path == "Assets")
                path = string.Empty;

            if (path.Length == 0)
                return fileName;

            return path + "/" + fileName;
        }

        private static NetworkModelGeneratedFile File(string fileName, string text) =>
            new() {
                FileName = fileName,
                Text = text
            };

        private static string EmitInterface(NetworkModelSpec spec) {
            NetworkModelCodeWriter writer = Begin(UsingsForSurface(spec, false));
            writer.Open("namespace " + spec.Namespace);
            writer.Open("public interface IReadOnly" + spec.ModelName + "Model");
            for (int i = 0; i < spec.Fields.Count; i++) {
                NetworkModelResolvedField field = spec.Fields[i];
                writer.Line("public " + ReadOnlyType(field) + " " + field.PropertyName + " { get; }");
            }

            writer.Line("public bool IsAvailable { get; }");
            writer.Line();
            WriteInterfaceEvents(writer, spec);
            writer.Line("public event Action OnChanged;");
            writer.Line("public event Action OnAvailableChanged;");
            writer.Close();
            writer.Close();
            return writer.ToString();
        }

        private static string EmitModel(NetworkModelSpec spec) {
            NetworkModelCodeWriter writer = Begin(UsingsForSurface(spec, true));
            writer.Open("namespace " + spec.Namespace);
            writer.Open("public sealed class " + spec.ModelName + "Model : NetworkModelBase, IReadOnly" + spec.ModelName + "Model");
            WriteModelFields(writer, spec);
            if (spec.Atomic)
                WriteApplyState(writer, spec);
            else
                WriteScalarApplies(writer, spec);

            WriteCollectionMutators(writer, spec);
            writer.Close();
            writer.Close();
            return writer.ToString();
        }

        private static string EmitState(NetworkModelSpec spec) {
            List<NetworkModelResolvedField> scalars = Scalars(spec);
            NetworkModelCodeWriter writer = Begin(new List<string> { "System", "System.Collections.Generic" });
            writer.Open("namespace " + spec.Namespace);
            writer.Open("public struct " + StateName(spec) + " : IEquatable<" + StateName(spec) + ">");
            for (int i = 0; i < scalars.Count; i++)
                writer.Line("public " + scalars[i].TypeName + " " + scalars[i].PropertyName + ";");

            writer.Line();
            writer.Open("public bool Equals(" + StateName(spec) + " other)");
            if (scalars.Count == 0) {
                writer.Line("return true;");
            }
            else {
                writer.Line("return " + ScalarEqualsChain(scalars) + ";");
            }

            writer.Close();
            writer.Line();
            writer.Line("public override bool Equals(object obj) =>");
            writer.Line("    obj is " + StateName(spec) + " other && Equals(other);");
            writer.Line();
            writer.Open("public override int GetHashCode()");
            writer.Line("HashCode hash = new HashCode();");
            for (int i = 0; i < scalars.Count; i++)
                writer.Line("hash.Add(" + scalars[i].PropertyName + ");");

            writer.Line("return hash.ToHashCode();");
            writer.Close();
            writer.Close();
            writer.Close();
            return writer.ToString();
        }

        private static string EmitBridge(NetworkModelSpec spec) {
            List<string> usings = new() { "Mirror", BASE_NAMESPACE };
            if (HasCollection(spec)) {
                usings.Add("System");
                usings.Add("System.Collections.Generic");
            }

            if (spec.Scope == NetworkModelScope.PerPlayer)
                usings.Add("Zenject");

            if (NeedsUnity(spec))
                usings.Add("UnityEngine");

            string modelName = spec.ModelName + "Model";
            string baseType = spec.Scope == NetworkModelScope.PerPlayer
                ? "NetworkPlayerModelBridge<" + modelName + ">"
                : "NetworkModelBridge<" + modelName + ">";
            NetworkModelCodeWriter writer = Begin(usings);
            writer.Open("namespace " + spec.Namespace);
            writer.Open("public class " + spec.ModelName + "Bridge : " + baseType);
            if (spec.Scope == NetworkModelScope.PerPlayer) {
                writer.Line("[SyncVar]");
                writer.Line("private string _playerKey;");
                writer.Line();
                writer.Line("private " + spec.ModelName + "Registry _registry;");
                writer.Line();
                writer.Line("protected override string PlayerKeyId");
                writer.Line("{");
                writer.Line("    get => _playerKey;");
                writer.Line("    set => _playerKey = value;");
                writer.Line("}");
                writer.Line();
                writer.Line("[Inject]");
                writer.Line("private void InjectRegistry(" + spec.ModelName + "Registry registry) =>");
                writer.Line("    _registry = registry;");
                writer.Line();
            }

            WriteSyncFields(writer, spec);
            WriteServerMethods(writer, spec);
            WritePushAndClear(writer, spec);
            WriteCallbackBindings(writer, spec);
            WriteHooks(writer, spec);
            writer.Close();
            writer.Close();
            return writer.ToString();
        }

        private static string EmitInstaller(NetworkModelSpec spec) {
            string bound = spec.Scope == NetworkModelScope.PerPlayer
                ? spec.ModelName + "Registry"
                : spec.ModelName + "Model";
            NetworkModelCodeWriter writer = Begin(new List<string> { "Zenject" });
            writer.Open("namespace " + spec.Namespace);
            writer.Open("public static class " + spec.ModelName + "ModelInstaller");
            writer.Line("public static void Install(DiContainer container) =>");
            writer.Line("    container.BindInterfacesAndSelfTo<" + bound + ">().AsSingle();");
            writer.Close();
            writer.Close();
            return writer.ToString();
        }

        private static string EmitRegistryInterface(NetworkModelSpec spec) {
            List<string> usings = new() { "System", BASE_NAMESPACE };
            NetworkModelCodeWriter writer = Begin(usings);
            writer.Open("namespace " + spec.Namespace);
            writer.Open("public interface IReadOnly" + spec.ModelName + "Registry");
            writer.Line("bool TryGet(PlayerKey key, out IReadOnly" + spec.ModelName + "Model model);");
            writer.Line("IReadOnly" + spec.ModelName + "Model Local { get; }");
            writer.Line("bool IsOnline(PlayerKey key);");
            writer.Line("event Action<PlayerKey, IReadOnly" + spec.ModelName + "Model> OnPlayerAdded;");
            writer.Line("event Action<PlayerKey> OnPlayerRemoved;");
            writer.Line("event Action<PlayerKey, bool> OnOnlineChanged;");
            writer.Close();
            writer.Close();
            return writer.ToString();
        }

        private static string EmitRegistry(NetworkModelSpec spec) {
            string model = spec.ModelName + "Model";
            string readable = "IReadOnly" + spec.ModelName + "Model";
            List<string> usings = new() { "System", "System.Collections.Generic", BASE_NAMESPACE };
            NetworkModelCodeWriter writer = Begin(usings);
            writer.Open("namespace " + spec.Namespace);
            writer.Open("public sealed class " + spec.ModelName + "Registry : IReadOnly" + spec.ModelName + "Registry");
            writer.Line("private readonly Dictionary<PlayerKey, " + model + "> _models = new();");
            writer.Line("private readonly Dictionary<PlayerKey, bool> _online = new();");
            writer.Line("private PlayerKey _localKey;");
            writer.Line("private bool _hasLocal;");
            writer.Line();
            writer.Line("public " + readable + " Local =>");
            writer.Line("    _hasLocal && _models.TryGetValue(_localKey, out " + model + " local) ? local : null;");
            writer.Line();
            writer.Line("public event Action<PlayerKey, " + readable + "> OnPlayerAdded;");
            writer.Line("public event Action<PlayerKey> OnPlayerRemoved;");
            writer.Line("public event Action<PlayerKey, bool> OnOnlineChanged;");
            writer.Line();
            writer.Open("public bool TryGet(PlayerKey key, out " + readable + " model)");
            writer.Open("if (_models.TryGetValue(key, out " + model + " stored))");
            writer.Line("model = stored;");
            writer.Line("return true;");
            writer.Close();
            writer.Line();
            writer.Line("model = null;");
            writer.Line("return false;");
            writer.Close();
            writer.Line();
            writer.Line("public bool IsOnline(PlayerKey key) =>");
            writer.Line("    _online.TryGetValue(key, out bool online) && online;");
            writer.Line();
            writer.Open("public " + model + " Bind(PlayerKey key, bool online, bool local)");
            writer.Open("if (_models.TryGetValue(key, out " + model + " model) == false)");
            writer.Line("model = new " + model + "();");
            writer.Line("_models.Add(key, model);");
            writer.Line("OnPlayerAdded?.Invoke(key, model);");
            writer.Close();
            writer.Line();
            writer.Line("SetOnline(key, online);");
            writer.Open("if (local)");
            writer.Line("_localKey = key;");
            writer.Line("_hasLocal = true;");
            writer.Close();
            writer.Line();
            writer.Line("return model;");
            writer.Close();
            writer.Line();
            writer.Line("public void MarkOffline(PlayerKey key) =>");
            writer.Line("    SetOnline(key, false);");
            writer.Line();
            writer.Open("public void ReleaseLocal(PlayerKey key, bool local)");
            writer.Line("MarkOffline(key);");
            writer.Open("if (local && _hasLocal && _localKey.Equals(key))");
            writer.Line("_hasLocal = false;");
            writer.Close();
            writer.Close();
            writer.Line();
            writer.Open("public void Remove(PlayerKey key)");
            writer.Line("if (_models.Remove(key) == false)");
            writer.Line("    return;");
            writer.Line();
            writer.Line("_online.Remove(key);");
            writer.Open("if (_hasLocal && _localKey.Equals(key))");
            writer.Line("_hasLocal = false;");
            writer.Close();
            writer.Line();
            writer.Line("OnPlayerRemoved?.Invoke(key);");
            writer.Close();
            writer.Line();
            writer.Open("private void SetOnline(PlayerKey key, bool online)");
            writer.Line("bool previous = IsOnline(key);");
            writer.Line("_online[key] = online;");
            writer.Line("if (previous != online)");
            writer.Line("    OnOnlineChanged?.Invoke(key, online);");
            writer.Close();
            writer.Close();
            writer.Close();
            return writer.ToString();
        }

        private static string Target(NetworkModelSpec spec) =>
            spec.Scope == NetworkModelScope.PerPlayer ? "Bound" : "Model";

        private static void WriteInterfaceEvents(NetworkModelCodeWriter writer, NetworkModelSpec spec) {
            for (int i = 0; i < spec.Fields.Count; i++) {
                NetworkModelResolvedField field = spec.Fields[i];
                switch (field.Kind) {
                    case NetworkModelFieldKind.Scalar:
                        writer.Line("public event Action On" + field.PropertyName + "Changed;");
                        break;
                    case NetworkModelFieldKind.List:
                        writer.Line("public event Action<int, " + field.TypeName + "> On" + field.PropertyName + "Added;");
                        writer.Line("public event Action<int, " + field.TypeName + "> On" + field.PropertyName + "Removed;");
                        writer.Line("public event Action<int, " + field.TypeName + "> On" + field.PropertyName + "Set;");
                        writer.Line("public event Action On" + field.PropertyName + "Cleared;");
                        writer.Line("public event Action On" + field.PropertyName + "Changed;");
                        break;
                    case NetworkModelFieldKind.Dictionary:
                        writer.Line("public event Action<" + field.KeyTypeName + ", " + field.ValueTypeName + "> On" + field.PropertyName + "Added;");
                        writer.Line("public event Action<" + field.KeyTypeName + ", " + field.ValueTypeName + "> On" + field.PropertyName + "Set;");
                        writer.Line("public event Action<" + field.KeyTypeName + "> On" + field.PropertyName + "Removed;");
                        writer.Line("public event Action On" + field.PropertyName + "Cleared;");
                        writer.Line("public event Action On" + field.PropertyName + "Changed;");
                        break;
                    case NetworkModelFieldKind.HashSet:
                        writer.Line("public event Action<" + field.TypeName + "> On" + field.PropertyName + "Added;");
                        writer.Line("public event Action<" + field.TypeName + "> On" + field.PropertyName + "Removed;");
                        writer.Line("public event Action On" + field.PropertyName + "Cleared;");
                        writer.Line("public event Action On" + field.PropertyName + "Changed;");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(field.Kind), field.Kind, null);
                }
            }
        }

        private static void WriteModelFields(NetworkModelCodeWriter writer, NetworkModelSpec spec) {
            for (int i = 0; i < spec.Fields.Count; i++) {
                NetworkModelResolvedField field = spec.Fields[i];
                if (field.Kind == NetworkModelFieldKind.Scalar) {
                    writer.Line("public " + field.TypeName + " " + field.PropertyName + " { get; private set; }");
                    continue;
                }

                writer.Line("private readonly " + BackingType(field) + " " + field.FieldName + " = new();");
                writer.Line("public " + ReadOnlyType(field) + " " + field.PropertyName + " => " + field.FieldName + ";");
            }

            writer.Line();
            WriteModelEvents(writer, spec);
        }

        private static void WriteModelEvents(NetworkModelCodeWriter writer, NetworkModelSpec spec) {
            for (int i = 0; i < spec.Fields.Count; i++) {
                NetworkModelResolvedField field = spec.Fields[i];
                switch (field.Kind) {
                    case NetworkModelFieldKind.Scalar:
                        writer.Line("public event Action On" + field.PropertyName + "Changed;");
                        break;
                    case NetworkModelFieldKind.List:
                        writer.Line("public event Action<int, " + field.TypeName + "> On" + field.PropertyName + "Added;");
                        writer.Line("public event Action<int, " + field.TypeName + "> On" + field.PropertyName + "Removed;");
                        writer.Line("public event Action<int, " + field.TypeName + "> On" + field.PropertyName + "Set;");
                        writer.Line("public event Action On" + field.PropertyName + "Cleared;");
                        writer.Line("public event Action On" + field.PropertyName + "Changed;");
                        break;
                    case NetworkModelFieldKind.Dictionary:
                        writer.Line("public event Action<" + field.KeyTypeName + ", " + field.ValueTypeName + "> On" + field.PropertyName + "Added;");
                        writer.Line("public event Action<" + field.KeyTypeName + ", " + field.ValueTypeName + "> On" + field.PropertyName + "Set;");
                        writer.Line("public event Action<" + field.KeyTypeName + "> On" + field.PropertyName + "Removed;");
                        writer.Line("public event Action On" + field.PropertyName + "Cleared;");
                        writer.Line("public event Action On" + field.PropertyName + "Changed;");
                        break;
                    case NetworkModelFieldKind.HashSet:
                        writer.Line("public event Action<" + field.TypeName + "> On" + field.PropertyName + "Added;");
                        writer.Line("public event Action<" + field.TypeName + "> On" + field.PropertyName + "Removed;");
                        writer.Line("public event Action On" + field.PropertyName + "Cleared;");
                        writer.Line("public event Action On" + field.PropertyName + "Changed;");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(field.Kind), field.Kind, null);
                }
            }
        }

        private static void WriteScalarApplies(NetworkModelCodeWriter writer, NetworkModelSpec spec) {
            List<NetworkModelResolvedField> scalars = Scalars(spec);
            for (int i = 0; i < scalars.Count; i++) {
                NetworkModelResolvedField field = scalars[i];
                writer.Line();
                writer.Open("internal void Apply" + field.PropertyName + "(" + field.TypeName + " value)");
                writer.Line("if (EqualityComparer<" + field.TypeName + ">.Default.Equals(" + field.PropertyName + ", value))");
                writer.Line("    return;");
                writer.Line();
                writer.Line(field.PropertyName + " = value;");
                writer.Line("On" + field.PropertyName + "Changed?.Invoke();");
                writer.Line("RaiseChanged();");
                writer.Close();
            }
        }

        private static void WriteApplyState(NetworkModelCodeWriter writer, NetworkModelSpec spec) {
            List<NetworkModelResolvedField> scalars = Scalars(spec);
            writer.Line();
            writer.Open("internal void ApplyState(" + StateName(spec) + " state)");
            writer.Line("bool changed = false;");
            for (int i = 0; i < scalars.Count; i++) {
                NetworkModelResolvedField field = scalars[i];
                writer.Open("if (EqualityComparer<" + field.TypeName + ">.Default.Equals(" + field.PropertyName + ", state." + field.PropertyName + ") == false)");
                writer.Line(field.PropertyName + " = state." + field.PropertyName + ";");
                writer.Line("On" + field.PropertyName + "Changed?.Invoke();");
                writer.Line("changed = true;");
                writer.Close();
            }

            writer.Line("if (changed)");
            writer.Line("    RaiseChanged();");
            writer.Close();
        }

        private static void WriteCollectionMutators(NetworkModelCodeWriter writer, NetworkModelSpec spec) {
            for (int i = 0; i < spec.Fields.Count; i++) {
                NetworkModelResolvedField field = spec.Fields[i];
                if (field.Kind == NetworkModelFieldKind.List)
                    WriteListMutators(writer, field);
                else if (field.Kind == NetworkModelFieldKind.Dictionary)
                    WriteDictionaryMutators(writer, field);
                else if (field.Kind == NetworkModelFieldKind.HashSet)
                    WriteHashSetMutators(writer, field);
            }
        }

        private static void WriteListMutators(NetworkModelCodeWriter writer, NetworkModelResolvedField field) {
            writer.Line();
            writer.Open("internal void Add" + field.PropertyName + "(int index, " + field.TypeName + " value)");
            writer.Line(field.FieldName + ".Insert(index, value);");
            writer.Line("On" + field.PropertyName + "Added?.Invoke(index, value);");
            writer.Line("On" + field.PropertyName + "Changed?.Invoke();");
            writer.Line("RaiseChanged();");
            writer.Close();
            writer.Line();
            writer.Open("internal void Set" + field.PropertyName + "(int index, " + field.TypeName + " value)");
            writer.Line("if (EqualityComparer<" + field.TypeName + ">.Default.Equals(" + field.FieldName + "[index], value))");
            writer.Line("    return;");
            writer.Line();
            writer.Line(field.FieldName + "[index] = value;");
            writer.Line("On" + field.PropertyName + "Set?.Invoke(index, value);");
            writer.Line("On" + field.PropertyName + "Changed?.Invoke();");
            writer.Line("RaiseChanged();");
            writer.Close();
            writer.Line();
            writer.Open("internal void Remove" + field.PropertyName + "At(int index, " + field.TypeName + " removed)");
            writer.Line("if (index >= 0 && index < " + field.FieldName + ".Count)");
            writer.Line("    " + field.FieldName + ".RemoveAt(index);");
            writer.Line();
            writer.Line("On" + field.PropertyName + "Removed?.Invoke(index, removed);");
            writer.Line("On" + field.PropertyName + "Changed?.Invoke();");
            writer.Line("RaiseChanged();");
            writer.Close();
            writer.Line();
            writer.Open("internal void Clear" + field.PropertyName + "()");
            writer.Line("if (" + field.FieldName + ".Count == 0)");
            writer.Line("    return;");
            writer.Line();
            writer.Line(field.FieldName + ".Clear();");
            writer.Line("On" + field.PropertyName + "Cleared?.Invoke();");
            writer.Line("On" + field.PropertyName + "Changed?.Invoke();");
            writer.Line("RaiseChanged();");
            writer.Close();
            writer.Line();
            writer.Open("internal void Replace" + field.PropertyName + "(IReadOnlyList<" + field.TypeName + "> values)");
            writer.Line("if (" + field.PropertyName + "Match(values))");
            writer.Line("    return;");
            writer.Line();
            writer.Line(field.FieldName + ".Clear();");
            writer.Line("On" + field.PropertyName + "Cleared?.Invoke();");
            writer.Open("for (int i = 0; i < values.Count; i++)");
            writer.Line(field.TypeName + " value = values[i];");
            writer.Line(field.FieldName + ".Add(value);");
            writer.Line("On" + field.PropertyName + "Added?.Invoke(i, value);");
            writer.Close();
            writer.Line("On" + field.PropertyName + "Changed?.Invoke();");
            writer.Line("RaiseChanged();");
            writer.Close();
            writer.Line();
            writer.Open("private bool " + field.PropertyName + "Match(IReadOnlyList<" + field.TypeName + "> values)");
            writer.Line("if (" + field.FieldName + ".Count != values.Count)");
            writer.Line("    return false;");
            writer.Line();
            writer.Open("for (int i = 0; i < " + field.FieldName + ".Count; i++)");
            writer.Line("if (EqualityComparer<" + field.TypeName + ">.Default.Equals(" + field.FieldName + "[i], values[i]) == false)");
            writer.Line("    return false;");
            writer.Close();
            writer.Line();
            writer.Line("return true;");
            writer.Close();
        }

        private static void WriteDictionaryMutators(NetworkModelCodeWriter writer, NetworkModelResolvedField field) {
            string pair = "KeyValuePair<" + field.KeyTypeName + ", " + field.ValueTypeName + ">";
            string readOnly = "IReadOnlyDictionary<" + field.KeyTypeName + ", " + field.ValueTypeName + ">";
            writer.Line();
            writer.Open("internal void Add" + field.PropertyName + "(" + field.KeyTypeName + " key, " + field.ValueTypeName + " value)");
            writer.Line(field.FieldName + ".Add(key, value);");
            writer.Line("On" + field.PropertyName + "Added?.Invoke(key, value);");
            writer.Line("On" + field.PropertyName + "Changed?.Invoke();");
            writer.Line("RaiseChanged();");
            writer.Close();
            writer.Line();
            writer.Open("internal void Set" + field.PropertyName + "(" + field.KeyTypeName + " key, " + field.ValueTypeName + " value)");
            writer.Line("if (" + field.FieldName + ".ContainsKey(key) && EqualityComparer<" + field.ValueTypeName + ">.Default.Equals(" + field.FieldName + "[key], value))");
            writer.Line("    return;");
            writer.Line();
            writer.Line(field.FieldName + "[key] = value;");
            writer.Line("On" + field.PropertyName + "Set?.Invoke(key, value);");
            writer.Line("On" + field.PropertyName + "Changed?.Invoke();");
            writer.Line("RaiseChanged();");
            writer.Close();
            writer.Line();
            writer.Open("internal void Remove" + field.PropertyName + "(" + field.KeyTypeName + " key)");
            writer.Line("if (" + field.FieldName + ".Remove(key) == false)");
            writer.Line("    return;");
            writer.Line();
            writer.Line("On" + field.PropertyName + "Removed?.Invoke(key);");
            writer.Line("On" + field.PropertyName + "Changed?.Invoke();");
            writer.Line("RaiseChanged();");
            writer.Close();
            writer.Line();
            writer.Open("internal void Clear" + field.PropertyName + "()");
            writer.Line("if (" + field.FieldName + ".Count == 0)");
            writer.Line("    return;");
            writer.Line();
            writer.Line(field.FieldName + ".Clear();");
            writer.Line("On" + field.PropertyName + "Cleared?.Invoke();");
            writer.Line("On" + field.PropertyName + "Changed?.Invoke();");
            writer.Line("RaiseChanged();");
            writer.Close();
            writer.Line();
            writer.Open("internal void Replace" + field.PropertyName + "(" + readOnly + " values)");
            writer.Line("if (" + field.PropertyName + "Match(values))");
            writer.Line("    return;");
            writer.Line();
            writer.Line(field.FieldName + ".Clear();");
            writer.Line("On" + field.PropertyName + "Cleared?.Invoke();");
            writer.Open("foreach (" + pair + " pair in values)");
            writer.Line(field.FieldName + ".Add(pair.Key, pair.Value);");
            writer.Line("On" + field.PropertyName + "Added?.Invoke(pair.Key, pair.Value);");
            writer.Close();
            writer.Line("On" + field.PropertyName + "Changed?.Invoke();");
            writer.Line("RaiseChanged();");
            writer.Close();
            writer.Line();
            writer.Open("private bool " + field.PropertyName + "Match(" + readOnly + " values)");
            writer.Line("if (" + field.FieldName + ".Count != values.Count)");
            writer.Line("    return false;");
            writer.Line();
            writer.Open("foreach (" + pair + " pair in " + field.FieldName + ")");
            writer.Line("if (values.TryGetValue(pair.Key, out " + field.ValueTypeName + " value) == false)");
            writer.Line("    return false;");
            writer.Line();
            writer.Line("if (EqualityComparer<" + field.ValueTypeName + ">.Default.Equals(pair.Value, value) == false)");
            writer.Line("    return false;");
            writer.Close();
            writer.Line();
            writer.Line("return true;");
            writer.Close();
        }

        private static void WriteHashSetMutators(NetworkModelCodeWriter writer, NetworkModelResolvedField field) {
            writer.Line();
            writer.Open("internal void Add" + field.PropertyName + "(" + field.TypeName + " value)");
            writer.Line("if (" + field.FieldName + ".Add(value) == false)");
            writer.Line("    return;");
            writer.Line();
            writer.Line("On" + field.PropertyName + "Added?.Invoke(value);");
            writer.Line("On" + field.PropertyName + "Changed?.Invoke();");
            writer.Line("RaiseChanged();");
            writer.Close();
            writer.Line();
            writer.Open("internal void Remove" + field.PropertyName + "(" + field.TypeName + " value)");
            writer.Line("if (" + field.FieldName + ".Remove(value) == false)");
            writer.Line("    return;");
            writer.Line();
            writer.Line("On" + field.PropertyName + "Removed?.Invoke(value);");
            writer.Line("On" + field.PropertyName + "Changed?.Invoke();");
            writer.Line("RaiseChanged();");
            writer.Close();
            writer.Line();
            writer.Open("internal void Clear" + field.PropertyName + "()");
            writer.Line("if (" + field.FieldName + ".Count == 0)");
            writer.Line("    return;");
            writer.Line();
            writer.Line(field.FieldName + ".Clear();");
            writer.Line("On" + field.PropertyName + "Cleared?.Invoke();");
            writer.Line("On" + field.PropertyName + "Changed?.Invoke();");
            writer.Line("RaiseChanged();");
            writer.Close();
            writer.Line();
            writer.Open("internal void Replace" + field.PropertyName + "(IEnumerable<" + field.TypeName + "> values)");
            writer.Line("List<" + field.TypeName + "> copy = new List<" + field.TypeName + ">(values);");
            writer.Line("if (" + field.PropertyName + "Match(copy))");
            writer.Line("    return;");
            writer.Line();
            writer.Line(field.FieldName + ".Clear();");
            writer.Line("On" + field.PropertyName + "Cleared?.Invoke();");
            writer.Open("for (int i = 0; i < copy.Count; i++)");
            writer.Line("if (" + field.FieldName + ".Add(copy[i]) == false)");
            writer.Line("    continue;");
            writer.Line();
            writer.Line("On" + field.PropertyName + "Added?.Invoke(copy[i]);");
            writer.Close();
            writer.Line("On" + field.PropertyName + "Changed?.Invoke();");
            writer.Line("RaiseChanged();");
            writer.Close();
            writer.Line();
            writer.Open("private bool " + field.PropertyName + "Match(List<" + field.TypeName + "> values)");
            writer.Line("if (" + field.FieldName + ".Count != values.Count)");
            writer.Line("    return false;");
            writer.Line();
            writer.Open("for (int i = 0; i < values.Count; i++)");
            writer.Line("if (" + field.FieldName + ".Contains(values[i]) == false)");
            writer.Line("    return false;");
            writer.Close();
            writer.Line();
            writer.Line("return true;");
            writer.Close();
        }

        private static void WriteSyncFields(NetworkModelCodeWriter writer, NetworkModelSpec spec) {
            if (spec.Atomic) {
                writer.Line("[SyncVar(hook = nameof(OnStateSynced))]");
                writer.Line("private " + StateName(spec) + " _state" + StateInitializer(spec) + ";");
            }
            else {
                List<NetworkModelResolvedField> scalars = Scalars(spec);
                for (int i = 0; i < scalars.Count; i++) {
                    NetworkModelResolvedField field = scalars[i];
                    writer.Line("[SyncVar(hook = nameof(On" + field.PropertyName + "Synced))]");
                    string initializer = string.IsNullOrEmpty(field.DefaultExpression) ? string.Empty : " = " + field.DefaultExpression;
                    writer.Line("private " + field.TypeName + " " + field.FieldName + initializer + ";");
                }
            }

            for (int i = 0; i < spec.Fields.Count; i++) {
                NetworkModelResolvedField field = spec.Fields[i];
                if (field.Kind == NetworkModelFieldKind.Scalar)
                    continue;

                writer.Line("private readonly " + SyncType(field) + " " + field.FieldName + " = new();");
            }
        }

        private static void WriteServerMethods(NetworkModelCodeWriter writer, NetworkModelSpec spec) {
            if (spec.Atomic) {
                List<NetworkModelResolvedField> scalars = Scalars(spec);
                for (int i = 0; i < scalars.Count; i++)
                    WriteAtomicServerSet(writer, spec, scalars[i]);

                if (scalars.Count > 0)
                    WriteAssignState(writer, spec);
            }
            else {
                List<NetworkModelResolvedField> scalars = Scalars(spec);
                for (int i = 0; i < scalars.Count; i++)
                    WriteScalarServerSet(writer, spec, scalars[i]);
            }

            for (int i = 0; i < spec.Fields.Count; i++) {
                NetworkModelResolvedField field = spec.Fields[i];
                if (field.Kind == NetworkModelFieldKind.List)
                    WriteListServerMethods(writer, field);
                else if (field.Kind == NetworkModelFieldKind.Dictionary)
                    WriteDictionaryServerMethods(writer, field);
                else if (field.Kind == NetworkModelFieldKind.HashSet)
                    WriteHashSetServerMethods(writer, field);
            }
        }

        private static void WriteScalarServerSet(NetworkModelCodeWriter writer, NetworkModelSpec spec, NetworkModelResolvedField field) {
            writer.Line();
            writer.Line("[Server]");
            writer.Open("public void ServerSet" + field.PropertyName + "(" + field.TypeName + " value)");
            writer.Line(field.FieldName + " = value;");
            writer.Line("if (NetworkServer.activeHost == false)");
            writer.Line("    " + Target(spec) + ".Apply" + field.PropertyName + "(value);");
            writer.Close();
        }

        private static void WriteAtomicServerSet(NetworkModelCodeWriter writer, NetworkModelSpec spec, NetworkModelResolvedField field) {
            writer.Line();
            writer.Line("[Server]");
            writer.Open("public void ServerSet" + field.PropertyName + "(" + field.TypeName + " value)");
            writer.Line(StateName(spec) + " state = _state;");
            writer.Line("state." + field.PropertyName + " = value;");
            writer.Line("AssignState(state);");
            writer.Close();
        }

        private static void WriteAssignState(NetworkModelCodeWriter writer, NetworkModelSpec spec) {
            writer.Line();
            writer.Open("private void AssignState(" + StateName(spec) + " state)");
            writer.Line("_state = state;");
            writer.Line("if (NetworkServer.activeHost == false)");
            writer.Line("    " + Target(spec) + ".ApplyState(state);");
            writer.Close();
        }

        private static void WriteListServerMethods(NetworkModelCodeWriter writer, NetworkModelResolvedField field) {
            writer.Line();
            writer.Line("[Server]");
            writer.Line("public void ServerAdd" + field.PropertyName + "(" + field.TypeName + " value) =>");
            writer.Line("    " + field.FieldName + ".Add(value);");
            writer.Line();
            writer.Line("[Server]");
            writer.Line("public void ServerInsert" + field.PropertyName + "(int index, " + field.TypeName + " value) =>");
            writer.Line("    " + field.FieldName + ".Insert(index, value);");
            writer.Line();
            writer.Line("[Server]");
            writer.Line("public void ServerSet" + field.PropertyName + "(int index, " + field.TypeName + " value) =>");
            writer.Line("    " + field.FieldName + "[index] = value;");
            writer.Line();
            writer.Line("[Server]");
            writer.Line("public bool ServerRemove" + field.PropertyName + "(" + field.TypeName + " value) =>");
            writer.Line("    " + field.FieldName + ".Remove(value);");
            writer.Line();
            writer.Line("[Server]");
            writer.Line("public void ServerRemove" + field.PropertyName + "At(int index) =>");
            writer.Line("    " + field.FieldName + ".RemoveAt(index);");
            writer.Line();
            writer.Line("[Server]");
            writer.Line("public void ServerClear" + field.PropertyName + "() =>");
            writer.Line("    " + field.FieldName + ".Clear();");
        }

        private static void WriteDictionaryServerMethods(NetworkModelCodeWriter writer, NetworkModelResolvedField field) {
            writer.Line();
            writer.Line("[Server]");
            writer.Open("public void ServerSet" + field.PropertyName + "(" + field.KeyTypeName + " key, " + field.ValueTypeName + " value)");
            writer.Line("if (" + field.FieldName + ".ContainsKey(key))");
            writer.Line("    " + field.FieldName + "[key] = value;");
            writer.Line("else");
            writer.Line("    " + field.FieldName + ".Add(key, value);");
            writer.Close();
            writer.Line();
            writer.Line("[Server]");
            writer.Line("public bool ServerRemove" + field.PropertyName + "(" + field.KeyTypeName + " key) =>");
            writer.Line("    " + field.FieldName + ".Remove(key);");
            writer.Line();
            writer.Line("[Server]");
            writer.Line("public void ServerClear" + field.PropertyName + "() =>");
            writer.Line("    " + field.FieldName + ".Clear();");
        }

        private static void WriteHashSetServerMethods(NetworkModelCodeWriter writer, NetworkModelResolvedField field) {
            writer.Line();
            writer.Line("[Server]");
            writer.Line("public bool ServerAdd" + field.PropertyName + "(" + field.TypeName + " value) =>");
            writer.Line("    " + field.FieldName + ".Add(value);");
            writer.Line();
            writer.Line("[Server]");
            writer.Line("public bool ServerRemove" + field.PropertyName + "(" + field.TypeName + " value) =>");
            writer.Line("    " + field.FieldName + ".Remove(value);");
            writer.Line();
            writer.Line("[Server]");
            writer.Line("public void ServerClear" + field.PropertyName + "() =>");
            writer.Line("    " + field.FieldName + ".Clear();");
        }

        private static void WritePushAndClear(NetworkModelCodeWriter writer, NetworkModelSpec spec) {
            string target = Target(spec);
            if (spec.Scope == NetworkModelScope.PerPlayer)
                WritePull(writer, spec);

            writer.Line();
            writer.Open("protected override void PushFullState()");
            WriteCopyToModel(writer, spec, target);
            writer.Close();
            if (spec.Scope == NetworkModelScope.PerPlayer) {
                WritePlayerBindOverrides(writer, spec);
                return;
            }

            writer.Line();
            writer.Open("protected override void ClearLocal()");
            if (spec.Atomic)
                writer.Line(target + ".ApplyState(" + ClearedState(spec) + ");");
            else {
                List<NetworkModelResolvedField> scalars = Scalars(spec);
                for (int i = 0; i < scalars.Count; i++)
                    writer.Line(target + ".Apply" + scalars[i].PropertyName + "(" + ClearValue(scalars[i]) + ");");
            }

            for (int i = 0; i < spec.Fields.Count; i++) {
                NetworkModelResolvedField field = spec.Fields[i];
                if (field.Kind != NetworkModelFieldKind.Scalar)
                    writer.Line(target + ".Clear" + field.PropertyName + "();");
            }

            writer.Close();
        }

        private static void WriteCopyToModel(NetworkModelCodeWriter writer, NetworkModelSpec spec, string target) {
            if (spec.Atomic)
                writer.Line(target + ".ApplyState(_state);");
            else {
                List<NetworkModelResolvedField> scalars = Scalars(spec);
                for (int i = 0; i < scalars.Count; i++)
                    writer.Line(target + ".Apply" + scalars[i].PropertyName + "(" + scalars[i].FieldName + ");");
            }

            for (int i = 0; i < spec.Fields.Count; i++) {
                NetworkModelResolvedField field = spec.Fields[i];
                if (field.Kind != NetworkModelFieldKind.Scalar)
                    writer.Line(target + ".Replace" + field.PropertyName + "(" + field.FieldName + ");");
            }
        }

        private static void WritePull(NetworkModelCodeWriter writer, NetworkModelSpec spec) {
            writer.Line();
            writer.Open("protected override void PullFromModel()");
            if (spec.Atomic) {
                List<NetworkModelResolvedField> scalars = Scalars(spec);
                writer.Line(StateName(spec) + " state = _state;");
                for (int i = 0; i < scalars.Count; i++)
                    writer.Line("state." + scalars[i].PropertyName + " = Bound." + scalars[i].PropertyName + ";");

                writer.Line("_state = state;");
            }
            else {
                List<NetworkModelResolvedField> scalars = Scalars(spec);
                for (int i = 0; i < scalars.Count; i++)
                    writer.Line(scalars[i].FieldName + " = Bound." + scalars[i].PropertyName + ";");
            }

            for (int i = 0; i < spec.Fields.Count; i++) {
                NetworkModelResolvedField field = spec.Fields[i];
                if (field.Kind == NetworkModelFieldKind.List)
                    WritePullList(writer, field);
                else if (field.Kind == NetworkModelFieldKind.Dictionary)
                    WritePullDictionary(writer, field);
                else if (field.Kind == NetworkModelFieldKind.HashSet)
                    WritePullHashSet(writer, field);
            }

            writer.Close();
        }

        private static void WritePullList(NetworkModelCodeWriter writer, NetworkModelResolvedField field) {
            writer.Line(field.FieldName + ".Clear();");
            writer.Open("for (int i = 0; i < Bound." + field.PropertyName + ".Count; i++)");
            writer.Line(field.FieldName + ".Add(Bound." + field.PropertyName + "[i]);");
            writer.Close();
        }

        private static void WritePullDictionary(NetworkModelCodeWriter writer, NetworkModelResolvedField field) {
            writer.Line(field.FieldName + ".Clear();");
            writer.Open("foreach (KeyValuePair<" + field.KeyTypeName + ", " + field.ValueTypeName + "> pair in Bound." + field.PropertyName + ")");
            writer.Line(field.FieldName + ".Add(pair.Key, pair.Value);");
            writer.Close();
        }

        private static void WritePullHashSet(NetworkModelCodeWriter writer, NetworkModelResolvedField field) {
            writer.Line(field.FieldName + ".Clear();");
            writer.Open("foreach (" + field.TypeName + " value in Bound." + field.PropertyName + ")");
            writer.Line(field.FieldName + ".Add(value);");
            writer.Close();
        }

        private static void WritePlayerBindOverrides(NetworkModelCodeWriter writer, NetworkModelSpec spec) {
            string model = spec.ModelName + "Model";
            writer.Line();
            writer.Line("protected override " + model + " BindServer(PlayerKey key) =>");
            writer.Line("    _registry.Bind(key, true, false);");
            writer.Line();
            writer.Line("protected override " + model + " BindClient(PlayerKey key, bool local) =>");
            writer.Line("    _registry.Bind(key, true, local);");
            writer.Line();
            writer.Line("protected override void ReleaseServer(PlayerKey key) =>");
            writer.Line("    _registry.MarkOffline(key);");
            writer.Line();
            writer.Line("protected override void ReleaseClient(PlayerKey key, bool local) =>");
            writer.Line("    _registry.ReleaseLocal(key, local);");
        }

        private static void WriteCallbackBindings(NetworkModelCodeWriter writer, NetworkModelSpec spec) {
            if (HasCollection(spec) == false)
                return;

            writer.Line();
            writer.Open("protected override void BindCallbacks()");
            for (int i = 0; i < spec.Fields.Count; i++) {
                NetworkModelResolvedField field = spec.Fields[i];
                if (field.Kind == NetworkModelFieldKind.List)
                    writer.Line(field.FieldName + ".Callback += Handle" + field.PropertyName + "Changed;");
                else if (field.Kind != NetworkModelFieldKind.Scalar)
                    writer.Line(field.FieldName + ".OnChange += Handle" + field.PropertyName + "Changed;");
            }

            writer.Close();
            writer.Line();
            writer.Open("protected override void ReleaseCallbacks()");
            for (int i = 0; i < spec.Fields.Count; i++) {
                NetworkModelResolvedField field = spec.Fields[i];
                if (field.Kind == NetworkModelFieldKind.List)
                    writer.Line(field.FieldName + ".Callback -= Handle" + field.PropertyName + "Changed;");
                else if (field.Kind != NetworkModelFieldKind.Scalar)
                    writer.Line(field.FieldName + ".OnChange -= Handle" + field.PropertyName + "Changed;");
            }

            writer.Close();
        }

        private static void WriteHooks(NetworkModelCodeWriter writer, NetworkModelSpec spec) {
            string target = Target(spec);
            if (spec.Atomic)
                WriteScalarHook(writer, spec, "OnStateSynced", StateName(spec), target + ".ApplyState(current);");
            else {
                List<NetworkModelResolvedField> scalars = Scalars(spec);
                for (int i = 0; i < scalars.Count; i++) {
                    NetworkModelResolvedField field = scalars[i];
                    WriteScalarHook(
                        writer,
                        spec,
                        "On" + field.PropertyName + "Synced",
                        field.TypeName,
                        target + ".Apply" + field.PropertyName + "(current);");
                }
            }

            for (int i = 0; i < spec.Fields.Count; i++) {
                NetworkModelResolvedField field = spec.Fields[i];
                if (field.Kind == NetworkModelFieldKind.List)
                    WriteListHook(writer, spec, field);
                else if (field.Kind == NetworkModelFieldKind.Dictionary)
                    WriteDictionaryHook(writer, spec, field);
                else if (field.Kind == NetworkModelFieldKind.HashSet)
                    WriteHashSetHook(writer, spec, field);
            }
        }

        private static void WriteScalarHook(
            NetworkModelCodeWriter writer,
            NetworkModelSpec spec,
            string methodName,
            string typeName,
            string apply) {
            writer.Line();
            if (spec.Scope != NetworkModelScope.PerPlayer) {
                writer.Line("private void " + methodName + "(" + typeName + " previous, " + typeName + " current) =>");
                writer.Line("    " + apply);
                return;
            }

            writer.Open("private void " + methodName + "(" + typeName + " previous, " + typeName + " current)");
            writer.Line("if (Bound == null || SuppressModelWrites)");
            writer.Line("    return;");
            writer.Line();
            writer.Line(apply);
            writer.Close();
        }

        private static void WriteModelGuard(NetworkModelCodeWriter writer, NetworkModelSpec spec) {
            if (spec.Scope != NetworkModelScope.PerPlayer)
                return;

            writer.Line("if (Bound == null || SuppressModelWrites)");
            writer.Line("    return;");
            writer.Line();
        }

        private static void WriteListHook(NetworkModelCodeWriter writer, NetworkModelSpec spec, NetworkModelResolvedField field) {
            string operation = "SyncList<" + field.TypeName + ">.Operation";
            string target = Target(spec);
            writer.Line();
            writer.Open("private void Handle" + field.PropertyName + "Changed(" + operation + " operation, int index, " + field.TypeName + " oldValue, " + field.TypeName + " newValue)");
            WriteModelGuard(writer, spec);
            writer.Open("switch (operation)");
            writer.Line("case " + operation + ".OP_ADD:");
            writer.Line("case " + operation + ".OP_INSERT:");
            writer.Line("    " + target + ".Add" + field.PropertyName + "(index, newValue);");
            writer.Line("    break;");
            writer.Line("case " + operation + ".OP_SET:");
            writer.Line("    " + target + ".Set" + field.PropertyName + "(index, newValue);");
            writer.Line("    break;");
            writer.Line("case " + operation + ".OP_REMOVEAT:");
            writer.Line("    " + target + ".Remove" + field.PropertyName + "At(index, oldValue);");
            writer.Line("    break;");
            writer.Line("case " + operation + ".OP_CLEAR:");
            writer.Line("    " + target + ".Clear" + field.PropertyName + "();");
            writer.Line("    break;");
            writer.Line("default:");
            writer.Line("    throw new ArgumentOutOfRangeException(nameof(operation), operation, null);");
            writer.Close();
            writer.Close();
        }

        private static void WriteDictionaryHook(NetworkModelCodeWriter writer, NetworkModelSpec spec, NetworkModelResolvedField field) {
            string operation = "SyncIDictionary<" + field.KeyTypeName + ", " + field.ValueTypeName + ">.Operation";
            string target = Target(spec);
            writer.Line();
            writer.Open("private void Handle" + field.PropertyName + "Changed(" + operation + " operation, " + field.KeyTypeName + " key, " + field.ValueTypeName + " value)");
            WriteModelGuard(writer, spec);
            writer.Open("switch (operation)");
            writer.Line("case " + operation + ".OP_ADD:");
            writer.Line("    " + target + ".Add" + field.PropertyName + "(key, value);");
            writer.Line("    break;");
            writer.Line("case " + operation + ".OP_SET:");
            writer.Line("    " + target + ".Set" + field.PropertyName + "(key, " + field.FieldName + "[key]);");
            writer.Line("    break;");
            writer.Line("case " + operation + ".OP_REMOVE:");
            writer.Line("    " + target + ".Remove" + field.PropertyName + "(key);");
            writer.Line("    break;");
            writer.Line("case " + operation + ".OP_CLEAR:");
            writer.Line("    " + target + ".Clear" + field.PropertyName + "();");
            writer.Line("    break;");
            writer.Line("default:");
            writer.Line("    throw new ArgumentOutOfRangeException(nameof(operation), operation, null);");
            writer.Close();
            writer.Close();
        }

        private static void WriteHashSetHook(NetworkModelCodeWriter writer, NetworkModelSpec spec, NetworkModelResolvedField field) {
            string operation = "SyncSet<" + field.TypeName + ">.Operation";
            string target = Target(spec);
            writer.Line();
            writer.Open("private void Handle" + field.PropertyName + "Changed(" + operation + " operation, " + field.TypeName + " value)");
            WriteModelGuard(writer, spec);
            writer.Open("switch (operation)");
            writer.Line("case " + operation + ".OP_ADD:");
            writer.Line("    " + target + ".Add" + field.PropertyName + "(value);");
            writer.Line("    break;");
            writer.Line("case " + operation + ".OP_REMOVE:");
            writer.Line("    " + target + ".Remove" + field.PropertyName + "(value);");
            writer.Line("    break;");
            writer.Line("case " + operation + ".OP_CLEAR:");
            writer.Line("    " + target + ".Clear" + field.PropertyName + "();");
            writer.Line("    break;");
            writer.Line("default:");
            writer.Line("    throw new ArgumentOutOfRangeException(nameof(operation), operation, null);");
            writer.Close();
            writer.Close();
        }

        private static NetworkModelCodeWriter Begin(List<string> usings) {
            NetworkModelCodeWriter writer = new();
            writer.Line("// <auto-generated>");
            writer.Line("// Generated by NetworkModelGenerator. Do not edit.");
            writer.Line();
            WriteUsings(writer, usings);
            return writer;
        }

        private static void WriteUsings(NetworkModelCodeWriter writer, List<string> usings) {
            usings.Sort(CompareUsings);
            string previous = null;
            for (int i = 0; i < usings.Count; i++) {
                if (usings[i] == previous)
                    continue;

                writer.Line("using " + usings[i] + ";");
                previous = usings[i];
            }

            if (usings.Count > 0)
                writer.Line();
        }

        private static int CompareUsings(string left, string right) {
            int rank = Rank(left).CompareTo(Rank(right));
            if (rank != 0)
                return rank;

            return string.CompareOrdinal(left, right);
        }

        private static int Rank(string value) =>
            value.StartsWith("System", StringComparison.Ordinal) ? 0 : 1;

        private static List<string> UsingsForSurface(NetworkModelSpec spec, bool model) {
            List<string> usings = new() { "System" };
            if (model || HasCollection(spec))
                usings.Add("System.Collections.Generic");

            if (model)
                usings.Add(BASE_NAMESPACE);

            if (NeedsUnity(spec))
                usings.Add("UnityEngine");

            if (NeedsMirror(spec))
                usings.Add("Mirror");

            return usings;
        }

        private static bool NeedsUnity(NetworkModelSpec spec) {
            for (int i = 0; i < spec.Fields.Count; i++) {
                if (spec.Fields[i].NeedsUnityEngine)
                    return true;
            }

            return false;
        }

        private static bool NeedsMirror(NetworkModelSpec spec) {
            for (int i = 0; i < spec.Fields.Count; i++) {
                if (spec.Fields[i].NeedsMirror)
                    return true;
            }

            return false;
        }

        private static bool HasCollection(NetworkModelSpec spec) {
            for (int i = 0; i < spec.Fields.Count; i++) {
                if (spec.Fields[i].Kind != NetworkModelFieldKind.Scalar)
                    return true;
            }

            return false;
        }

        private static List<NetworkModelResolvedField> Scalars(NetworkModelSpec spec) {
            List<NetworkModelResolvedField> fields = new();
            for (int i = 0; i < spec.Fields.Count; i++) {
                if (spec.Fields[i].Kind == NetworkModelFieldKind.Scalar)
                    fields.Add(spec.Fields[i]);
            }

            return fields;
        }

        private static string ReadOnlyType(NetworkModelResolvedField field) {
            switch (field.Kind) {
                case NetworkModelFieldKind.Scalar:
                    return field.TypeName;
                case NetworkModelFieldKind.List:
                    return "IReadOnlyList<" + field.TypeName + ">";
                case NetworkModelFieldKind.Dictionary:
                    return "IReadOnlyDictionary<" + field.KeyTypeName + ", " + field.ValueTypeName + ">";
                case NetworkModelFieldKind.HashSet:
                    return "IReadOnlyCollection<" + field.TypeName + ">";
                default:
                    throw new ArgumentOutOfRangeException(nameof(field.Kind), field.Kind, null);
            }
        }

        private static string BackingType(NetworkModelResolvedField field) {
            switch (field.Kind) {
                case NetworkModelFieldKind.List:
                    return "List<" + field.TypeName + ">";
                case NetworkModelFieldKind.Dictionary:
                    return "Dictionary<" + field.KeyTypeName + ", " + field.ValueTypeName + ">";
                case NetworkModelFieldKind.HashSet:
                    return "HashSet<" + field.TypeName + ">";
                default:
                    throw new ArgumentOutOfRangeException(nameof(field.Kind), field.Kind, null);
            }
        }

        private static string SyncType(NetworkModelResolvedField field) {
            switch (field.Kind) {
                case NetworkModelFieldKind.List:
                    return "SyncList<" + field.TypeName + ">";
                case NetworkModelFieldKind.Dictionary:
                    return "SyncDictionary<" + field.KeyTypeName + ", " + field.ValueTypeName + ">";
                case NetworkModelFieldKind.HashSet:
                    return "SyncHashSet<" + field.TypeName + ">";
                default:
                    throw new ArgumentOutOfRangeException(nameof(field.Kind), field.Kind, null);
            }
        }

        private static string StateName(NetworkModelSpec spec) =>
            spec.ModelName + "State";

        private static string StateInitializer(NetworkModelSpec spec) {
            List<NetworkModelResolvedField> scalars = Scalars(spec);
            bool any = false;
            for (int i = 0; i < scalars.Count; i++) {
                if (string.IsNullOrEmpty(scalars[i].DefaultExpression) == false)
                    any = true;
            }

            if (any == false)
                return string.Empty;

            string text = " = new " + StateName(spec) + " { ";
            bool first = true;
            for (int i = 0; i < scalars.Count; i++) {
                if (string.IsNullOrEmpty(scalars[i].DefaultExpression))
                    continue;

                if (first == false)
                    text += ", ";

                text += scalars[i].PropertyName + " = " + scalars[i].DefaultExpression;
                first = false;
            }

            return text + " }";
        }

        private static string ClearedState(NetworkModelSpec spec) {
            List<NetworkModelResolvedField> scalars = Scalars(spec);
            string text = "new " + StateName(spec) + " { ";
            for (int i = 0; i < scalars.Count; i++) {
                if (i > 0)
                    text += ", ";

                text += scalars[i].PropertyName + " = " + ClearValue(scalars[i]);
            }

            return text + " }";
        }

        private static string ClearValue(NetworkModelResolvedField field) =>
            string.IsNullOrEmpty(field.DefaultExpression) ? "default" : field.DefaultExpression;

        private static string ScalarEqualsChain(List<NetworkModelResolvedField> scalars) {
            string text = string.Empty;
            for (int i = 0; i < scalars.Count; i++) {
                if (i > 0)
                    text += " && ";

                text += "EqualityComparer<" + scalars[i].TypeName + ">.Default.Equals(" + scalars[i].PropertyName + ", other." + scalars[i].PropertyName + ")";
            }

            return text;
        }
    }
}
