using UnityEngine;

namespace Features.PlayerLifeModule.Scripts {
    [CreateAssetMenu(
        fileName = nameof(PlayerDamageConfiguration) + "_Default",
        menuName = "Configurations/PlayerLifeModule/" + nameof(PlayerDamageConfiguration))]
    public sealed class PlayerDamageConfiguration : ScriptableObject {
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _killDepthBelowStation = 30f;
        [SerializeField] private float _killDepthBelowDeckInFlight = 30f;

        public float MaxHealth =>
            _maxHealth;

        public float KillDepthBelowStation =>
            _killDepthBelowStation;

        public float KillDepthBelowDeckInFlight =>
            _killDepthBelowDeckInFlight;
    }
}
