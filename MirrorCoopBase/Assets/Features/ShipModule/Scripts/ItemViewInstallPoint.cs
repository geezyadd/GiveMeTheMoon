using System;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    [Serializable]
    public sealed class ItemViewInstallPoint {
        [SerializeField] private ItemViewId _view;
        [SerializeField] private Transform _point;

        public ItemViewId View => _view;
        public Transform Point => _point;
    }
}
