using Features.MvpModule;
using UnityEngine;
using UnityEngine.UI;

namespace Features.PlayerLifeModule.Scripts.Spectator {
    public abstract class SpectatorHudViewBase : ViewBehaviour {
        public abstract void SetLabel(string value);
    }

    public sealed class SpectatorHudView : SpectatorHudViewBase {
        [SerializeField] private Text _label;

        public override void SetLabel(string value) =>
            _label.text = value;
    }
}
