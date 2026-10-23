using System.Collections.Generic;
using Features.WeaponModule.Scripts.Parts.Delivery;
using UnityEngine;

namespace Features.WeaponModule.Editor.Tests {
    // Records every ray and answers with a fixed result.
    public sealed class FakeWeaponRaycast : IWeaponRaycast {
        private readonly List<Vector3> _directions = new List<Vector3>();
        private readonly bool _isHitting;
        private readonly WeaponRaycastHit _hit;

        public IReadOnlyList<Vector3> Directions => _directions;

        public FakeWeaponRaycast(bool isHitting, WeaponRaycastHit hit) {
            _isHitting = isHitting;
            _hit = hit;
        }

        public bool TryRaycast(Vector3 origin, Vector3 direction, float maxDistance, out WeaponRaycastHit hit) {
            _directions.Add(direction);
            hit = _hit;
            return _isHitting;
        }
    }
}
