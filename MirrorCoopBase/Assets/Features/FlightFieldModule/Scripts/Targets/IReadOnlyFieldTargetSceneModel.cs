using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Targets {
    public interface IReadOnlyFieldTargetSceneModel {
        public Transform Root { get; }
        public bool HasRoot { get; }
        public int BuiltNumber { get; }
        public IReadOnlyList<FieldTargetView> Views { get; }
        public bool TryGetView(int index, out FieldTargetView view);
    }
}
