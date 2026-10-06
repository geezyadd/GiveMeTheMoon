using System.Collections.Generic;
using Features.NetworkModelModule.Scripts;
using NUnit.Framework;

namespace Features.NetworkModelModule.Scripts.Editor.Tests {
    public sealed class NetworkModelCodeEmitterTests {
        [Test]
        public void Resolver_AcceptsSupportedTypes() {
            Assert.That(NetworkModelTypeResolver.TryResolveType("long", out ResolvedNetworkType scalar, out _), Is.True);
            Assert.That(scalar.Kind, Is.EqualTo(NetworkModelFieldKind.Scalar));
            Assert.That(NetworkModelTypeResolver.TryResolveType("Vector3", out ResolvedNetworkType vector, out _), Is.True);
            Assert.That(vector.NeedsUnityEngine, Is.True);
            Assert.That(NetworkModelTypeResolver.TryResolveType("List<int>", out ResolvedNetworkType list, out _), Is.True);
            Assert.That(list.Kind, Is.EqualTo(NetworkModelFieldKind.List));
            Assert.That(NetworkModelTypeResolver.TryResolveType("Dictionary<string, int>", out ResolvedNetworkType dictionary, out _), Is.True);
            Assert.That(dictionary.Kind, Is.EqualTo(NetworkModelFieldKind.Dictionary));
            Assert.That(NetworkModelTypeResolver.TryResolveType("HashSet<int>", out ResolvedNetworkType set, out _), Is.True);
            Assert.That(set.Kind, Is.EqualTo(NetworkModelFieldKind.HashSet));
            Assert.That(NetworkModelTypeResolver.TryResolveType("NetworkIdentity", out ResolvedNetworkType identity, out _), Is.True);
            Assert.That(identity.NeedsMirror, Is.True);
        }

        [Test]
        public void Resolver_RejectsUnknownAndNestedCollections() {
            Assert.That(NetworkModelTypeResolver.TryResolveType("NotARealTypeXYZ", out _, out string unknown), Is.False);
            Assert.That(unknown, Does.Contain("Unknown type"));
            Assert.That(NetworkModelTypeResolver.TryResolveType("List<List<int>>", out _, out string nested), Is.False);
            Assert.That(nested, Does.Contain("Nested"));
        }

        [Test]
        public void Emit_Wallet_ContainsSyncVarHookAndReadOnlySurface() {
            string bridge = Bridge(Spec("Wallet", false, Field("Balance", NetworkModelFieldKind.Scalar, "long")));
            string readable = InterfaceText(Spec("Wallet", false, Field("Balance", NetworkModelFieldKind.Scalar, "long")));
            Assert.That(bridge, Does.Contain("[SyncVar(hook = nameof(OnBalanceSynced))]"));
            Assert.That(bridge, Does.Contain("public void ServerSetBalance(long value)"));
            Assert.That(bridge, Does.Contain("Model.ApplyBalance(current)"));
            Assert.That(bridge, Does.Contain("PushFullState()"));
            Assert.That(readable, Does.Contain("public interface IReadOnlyWalletModel"));
            Assert.That(readable, Does.Contain("public event Action OnBalanceChanged;"));
            Assert.That(readable, Does.Contain("public event Action OnChanged;"));
        }

        [Test]
        public void Emit_DebugCounter_CoversScalarStringVectorAndList() {
            NetworkModelSpec spec = Spec(
                "DebugCounter",
                false,
                Field("Count", NetworkModelFieldKind.Scalar, "int"),
                Field("Label", NetworkModelFieldKind.Scalar, "string"),
                Field("Position", NetworkModelFieldKind.Scalar, "Vector3", true),
                Field("Scores", NetworkModelFieldKind.List, "int"));
            string bridge = Bridge(spec);
            string model = ModelText(spec);
            Assert.That(bridge, Does.Contain("SyncList<int> _scores"));
            Assert.That(bridge, Does.Contain("ServerAddScores"));
            Assert.That(bridge, Does.Contain("ServerClearScores"));
            Assert.That(bridge, Does.Contain("OnCountSynced"));
            Assert.That(model, Does.Contain("public event Action<int, int> OnScoresAdded;"));
            Assert.That(model, Does.Contain("public event Action<int, int> OnScoresRemoved;"));
            Assert.That(model, Does.Contain("public event Action<int, int> OnScoresSet;"));
            Assert.That(model, Does.Contain("public event Action OnScoresCleared;"));
            Assert.That(model, Does.Contain("IReadOnlyList<int> Scores"));
        }

        [Test]
        public void Emit_Atomic_PutsScalarsInOneSyncVar() {
            NetworkModelSpec spec = Spec(
                "AtomicPair",
                true,
                Field("Left", NetworkModelFieldKind.Scalar, "int"),
                Field("Right", NetworkModelFieldKind.Scalar, "int"));
            string bridge = Bridge(spec);
            string state = StateText(spec);
            Assert.That(state, Does.Contain("public struct AtomicPairState"));
            Assert.That(bridge, Does.Contain("[SyncVar(hook = nameof(OnStateSynced))]"));
            Assert.That(bridge, Does.Contain("private AtomicPairState _state;"));
            Assert.That(CountOf(bridge, "[SyncVar"), Is.EqualTo(1));
            Assert.That(bridge, Does.Contain("state.Left = value;"));
            Assert.That(bridge, Does.Contain("state.Right = value;"));
        }

        [Test]
        public void Emit_AtomicWithUnityType_StateImportsUnityEngine() {
            NetworkModelSpec spec = Spec("AtomicPose", true, Field("Position", NetworkModelFieldKind.Scalar, "Vector3", true));
            Assert.That(StateText(spec), Does.Contain("using UnityEngine;"));
        }

        [Test]
        public void Emit_DictionaryHashSetAndNetworkIdentity() {
            NetworkModelSpec spec = Spec(
                "SyncCoverage",
                false,
                Field("Target", NetworkModelFieldKind.Scalar, "NetworkIdentity", false, true),
                Field("Marker", NetworkModelFieldKind.Scalar, "GameObject", true, false),
                Field("TargetNetId", NetworkModelFieldKind.Scalar, "uint"),
                Field("Prices", NetworkModelFieldKind.Dictionary, valueType: "int", keyType: "string"),
                Field("Tags", NetworkModelFieldKind.HashSet, "int"));
            string bridge = Bridge(spec);
            string model = ModelText(spec);
            Assert.That(bridge, Does.Contain("private NetworkIdentity _target;"));
            Assert.That(bridge, Does.Contain("private GameObject _marker;"));
            Assert.That(bridge, Does.Contain("private uint _targetNetId;"));
            Assert.That(bridge, Does.Contain("SyncDictionary<string, int> _prices"));
            Assert.That(bridge, Does.Contain("SyncHashSet<int> _tags"));
            Assert.That(bridge, Does.Contain("ServerSetPrices"));
            Assert.That(bridge, Does.Contain("ServerAddTags"));
            Assert.That(model, Does.Contain("IReadOnlyDictionary<string, int> Prices"));
            Assert.That(model, Does.Contain("IReadOnlyCollection<int> Tags"));
            Assert.That(model, Does.Contain("OnPricesAdded"));
            Assert.That(model, Does.Contain("OnTagsRemoved"));
        }

        private static NetworkModelSpec Spec(string name, bool atomic, params NetworkModelResolvedField[] fields) =>
            new() {
                ModelName = name,
                Namespace = "Features.NetworkModelModule.Scripts.Generated",
                Folder = "Assets/Features/NetworkModelModule/Scripts/Generated",
                Atomic = atomic,
                Fields = new List<NetworkModelResolvedField>(fields)
            };

        private static NetworkModelResolvedField Field(
            string name,
            NetworkModelFieldKind kind,
            string typeName = null,
            bool unity = false,
            bool mirror = false,
            string keyType = null,
            string valueType = null) =>
            new() {
                PropertyName = name,
                FieldName = "_" + char.ToLowerInvariant(name[0]) + name.Substring(1),
                Kind = kind,
                TypeName = typeName,
                KeyTypeName = keyType,
                ValueTypeName = valueType,
                NeedsUnityEngine = unity,
                NeedsMirror = mirror
            };

        private static string Bridge(NetworkModelSpec spec) =>
            Find(spec, spec.ModelName + "Bridge_g.cs");

        private static string ModelText(NetworkModelSpec spec) =>
            Find(spec, spec.ModelName + "Model_g.cs");

        private static string InterfaceText(NetworkModelSpec spec) =>
            Find(spec, "IReadOnly" + spec.ModelName + "Model_g.cs");

        [Test]
        public void Emit_PerPlayer_EmitsRegistryAndPlayerBridge() {
            NetworkModelSpec spec = Spec(
                "PlayerStats",
                false,
                Field("Score", NetworkModelFieldKind.Scalar, "int"),
                Field("Title", NetworkModelFieldKind.Scalar, "string"));
            spec.Scope = NetworkModelScope.PerPlayer;
            string bridge = Bridge(spec);
            string registry = Find(spec, "PlayerStatsRegistry_g.cs");
            string readable = Find(spec, "IReadOnlyPlayerStatsRegistry_g.cs");
            string installer = Find(spec, "PlayerStatsModelInstaller_g.cs");
            Assert.That(bridge, Does.Contain("NetworkPlayerModelBridge<PlayerStatsModel>"));
            Assert.That(bridge, Does.Contain("protected override void PullFromModel()"));
            Assert.That(bridge, Does.Contain("_registry.Bind(key, true, false)"));
            Assert.That(bridge, Does.Contain("_registry.Bind(key, true, local)"));
            Assert.That(bridge, Does.Contain("_registry.MarkOffline(key)"));
            Assert.That(bridge, Does.Contain("public void ServerSetScore(int value)"));
            Assert.That(bridge, Does.Contain("if (Bound == null || SuppressModelWrites)"));
            Assert.That(registry, Does.Contain("public PlayerStatsModel Bind(PlayerKey key, bool online, bool local)"));
            Assert.That(registry, Does.Contain("public void Remove(PlayerKey key)"));
            Assert.That(readable, Does.Contain("bool TryGet(PlayerKey key, out IReadOnlyPlayerStatsModel model);"));
            Assert.That(readable, Does.Contain("IReadOnlyPlayerStatsModel Local { get; }"));
            Assert.That(readable, Does.Contain("bool IsOnline(PlayerKey key);"));
            Assert.That(readable, Does.Contain("event Action<PlayerKey, IReadOnlyPlayerStatsModel> OnPlayerAdded;"));
            Assert.That(readable, Does.Contain("event Action<PlayerKey> OnPlayerRemoved;"));
            Assert.That(readable, Does.Contain("event Action<PlayerKey, bool> OnOnlineChanged;"));
            Assert.That(installer, Does.Contain("BindInterfacesAndSelfTo<PlayerStatsRegistry>().AsSingle()"));
            Assert.That(installer, Does.Not.Contain("PlayerStatsModel>().AsSingle()"));
        }

        private static string StateText(NetworkModelSpec spec) =>
            Find(spec, spec.ModelName + "State_g.cs");

        private static string Find(NetworkModelSpec spec, string fileName) {
            List<NetworkModelGeneratedFile> files = NetworkModelCodeEmitter.Emit(spec);
            for (int i = 0; i < files.Count; i++) {
                if (files[i].FileName == fileName)
                    return files[i].Text;
            }

            Assert.Fail("Missing " + fileName);
            return string.Empty;
        }

        private static int CountOf(string text, string token) {
            int count = 0;
            int index = 0;
            while (index >= 0) {
                index = text.IndexOf(token, index, System.StringComparison.Ordinal);
                if (index < 0)
                    break;

                count++;
                index += token.Length;
            }

            return count;
        }
    }
}
