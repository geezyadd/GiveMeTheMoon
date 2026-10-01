namespace Features.NetworkModelModule.Scripts.Editor {
    public sealed class NetworkModelResolvedField {
        public string PropertyName { get; set; }
        public string FieldName { get; set; }
        public NetworkModelFieldKind Kind { get; set; }
        public string TypeName { get; set; }
        public string KeyTypeName { get; set; }
        public string ValueTypeName { get; set; }
        public string DefaultExpression { get; set; }
        public bool NeedsUnityEngine { get; set; }
        public bool NeedsMirror { get; set; }
    }
}
