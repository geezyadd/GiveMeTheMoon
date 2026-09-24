using Features.MvpModule;
using UnityEngine;

namespace Features.LobbyModule.Scripts {
    public abstract class RadarViewBase : ViewBehaviour {
        public abstract RectTransform Slot { get; }
    }

    public sealed class RadarView : RadarViewBase {
        [SerializeField] private RectTransform _slot;

        public override RectTransform Slot => _slot != null ? _slot : (RectTransform)transform;
    }
}
