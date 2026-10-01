using System;

namespace Features.NetworkModelModule.Scripts.Editor {
    public sealed class ResolvedNetworkType {
        public NetworkModelFieldKind Kind { get; set; }
        public string TypeName { get; set; }
        public string KeyTypeName { get; set; }
        public string ValueTypeName { get; set; }
        public bool NeedsUnityEngine { get; set; }
        public bool NeedsMirror { get; set; }
        public Type RuntimeType { get; set; }
    }
}
