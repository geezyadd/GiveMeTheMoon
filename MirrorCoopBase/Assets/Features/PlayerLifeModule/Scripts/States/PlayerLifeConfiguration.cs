using UnityEngine;

namespace Features.PlayerLifeModule.Scripts {
    [CreateAssetMenu(
        fileName = nameof(PlayerLifeConfiguration) + "_Default",
        menuName = "Configurations/PlayerLifeModule/" + nameof(PlayerLifeConfiguration))]
    public sealed class PlayerLifeConfiguration : ScriptableObject {
        [SerializeField, Min(0f)] private float _allDeadReturnDelay = 3f;

        public float AllDeadReturnDelay =>
            _allDeadReturnDelay;
    }
}
