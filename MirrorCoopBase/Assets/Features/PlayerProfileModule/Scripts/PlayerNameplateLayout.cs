using UnityEngine;

namespace Features.PlayerProfileModule.Scripts {
    public sealed class PlayerNameplateLayout : IPlayerNameplateLayout {
        private readonly PlayerNameplateConfiguration _playerNameplateConfiguration;

        public PlayerNameplateLayout(PlayerNameplateConfiguration playerNameplateConfiguration) =>
            _playerNameplateConfiguration = playerNameplateConfiguration;

        public float CalculateAlpha(float distance) =>
            1f - Mathf.InverseLerp(
                _playerNameplateConfiguration.FadeStartDistance,
                _playerNameplateConfiguration.FadeEndDistance,
                distance);

        public float CalculateScale(float distance) {
            float nearDistance = _playerNameplateConfiguration.ConstantScreenSizeFromDistance;
            float farDistance = _playerNameplateConfiguration.ConstantScreenSizeToDistance;
            return Mathf.Clamp(distance, nearDistance, farDistance) / nearDistance;
        }
    }
}
