using System.Collections.Generic;
using Features.NetworkModelModule.Scripts;

namespace Features.NetworkModelModule.Scripts.Editor {
    public sealed class NetworkModelSpec {
        public string ModelName { get; set; }
        public string Namespace { get; set; }
        public string Folder { get; set; }
        public bool Atomic { get; set; }
        public NetworkModelScope Scope { get; set; }
        public List<NetworkModelResolvedField> Fields { get; set; }
    }
}
