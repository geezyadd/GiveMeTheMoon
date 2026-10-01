using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.NetworkModelModule.Scripts {
    [CreateAssetMenu(fileName = "NetworkModel", menuName = "Network Models/Definition")]
    public sealed class NetworkModelDefinition : ScriptableObject {
        [SerializeField] private string _modelName;
        [SerializeField] private string _namespace;
        [SerializeField] private string _folder;
        [SerializeField] private bool _atomic;
        [SerializeField] private NetworkModelScope _scope;
        [SerializeField] private List<Field> _fields = new();

        public string ModelName => _modelName;
        public string Namespace => _namespace;
        public string Folder => _folder;
        public bool Atomic => _atomic;
        public NetworkModelScope Scope => _scope;
        public IReadOnlyList<Field> Fields => _fields;

        [Serializable]
        public sealed class Field {
            [SerializeField] private string _name;
            [SerializeField] private string _typeName;
            [SerializeField] private string _defaultValue;

            public string Name => _name;
            public string TypeName => _typeName;
            public string DefaultValue => _defaultValue;
        }
    }
}
