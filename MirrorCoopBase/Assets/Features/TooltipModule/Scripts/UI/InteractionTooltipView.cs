using Features.MvpModule;
using UnityEngine;
using UnityEngine.UI;

namespace Features.TooltipModule.Scripts.UI {
    public sealed class InteractionTooltipView : InteractionTooltipViewBase {
        [SerializeField] private Text _title;
        [SerializeField] private Text _description;
        [SerializeField] private Text _actionHint;

        public override void SetContent(string title, string description, string actionHint) {
            _title.text = title;
            _description.text = description;
            _actionHint.text = actionHint;
        }
    }
}
