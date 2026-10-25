using UnityEngine;

namespace Features.ShipModule.Scripts.Contracts {
    // The write side of a registry of components in the loaded map: a component adds itself while it is enabled.
    public interface IComponentRegistry<in T> where T : Component {
        public void Add(T component);
        public void Remove(T component);
    }
}
