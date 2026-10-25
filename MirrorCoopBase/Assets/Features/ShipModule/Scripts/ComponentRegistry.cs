using System.Collections.Generic;
using Features.ShipModule.Scripts.Contracts;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    // The enabled components of one type in the loaded map (deck spawn points, hull damage points, tool racks, impact
    // syncs): each adds itself in OnEnable and removes itself in OnDisable.
    public sealed class ComponentRegistry<T> : IComponentRegistry<T>, IReadOnlyComponentRegistry<T> where T : Component {
        private readonly List<T> _items = new();

        public IReadOnlyList<T> Items =>
            _items;

        public void Add(T component) {
            if (_items.Contains(component) == false)
                _items.Add(component);
        }

        public void Remove(T component) =>
            _items.Remove(component);
    }
}
