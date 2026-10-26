using System.Collections.Generic;
using UnityEngine;

namespace Features.ShipModule.Scripts.Contracts {
    // The enabled components of one type in the loaded map, in the order they were enabled.
    public interface IReadOnlyComponentRegistry<T> where T : Component {
        public IReadOnlyList<T> Items { get; }
    }
}
