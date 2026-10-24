using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Scene {
    // What one placement or debris cell has built on this peer: every pooled object (returned together) and its rocks
    // by rock index, so a broken rock can be hidden.
    public sealed class FieldBuiltObjects {
        private readonly List<GameObject> _objects = new();
        private readonly List<FieldRockVisual> _rocks = new();

        public IReadOnlyList<GameObject> Objects => _objects;
        public IReadOnlyList<FieldRockVisual> Rocks => _rocks;

        public void AddObject(GameObject builtObject) =>
            _objects.Add(builtObject);

        public void AddRock(FieldRockVisual rock) {
            _objects.Add(rock.gameObject);
            _rocks.Add(rock);
        }

        public void Clear() {
            _objects.Clear();
            _rocks.Clear();
        }
    }
}
