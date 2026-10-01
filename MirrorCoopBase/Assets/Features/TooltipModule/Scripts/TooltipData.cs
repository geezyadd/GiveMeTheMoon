using UnityEngine;

namespace Features.TooltipModule.Scripts {
    [CreateAssetMenu(
        fileName = nameof(TooltipData),
        menuName = "Configurations/TooltipModule/" + nameof(TooltipData))]
    public sealed class TooltipData : ScriptableObject {
        [SerializeField] private string _title;
        [SerializeField] private string _description;
        [SerializeField] private string _actionHint;

        public string Title => _title;
        public string Description => _description;
        public string ActionHint => _actionHint;
    }
}
