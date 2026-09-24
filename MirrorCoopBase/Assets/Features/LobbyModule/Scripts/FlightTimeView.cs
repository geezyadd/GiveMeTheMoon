using System;
using Features.MvpModule;
using UnityEngine;
using UnityEngine.UI;

namespace Features.LobbyModule.Scripts {
    public abstract class FlightTimeViewBase : ViewBehaviour {
        public Action OnTick;

        public abstract void SetTimeText(string value);
    }

    public sealed class FlightTimeView : FlightTimeViewBase {
        [SerializeField] private Text _label;

        private void LateUpdate() {
            OnTick?.Invoke();
        }

        public override void SetTimeText(string value) {
            if (_label != null)
                _label.text = value;
        }
    }
}
