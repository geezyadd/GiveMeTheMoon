using System.Collections;
using Features.NetworkModelModule.Scripts;
using Features.NetworkModelModule.Scripts.Samples;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.NetworkModelModule.Samples {
    public sealed class PlayerStatsSample : NetworkBehaviour {
        [SerializeField] private PlayerStatsBridge _bridge;

        private IPlayerIdentityService _identity;
        private PlayerStatsRegistry _registry;

        public int ObservedScore { get; private set; }
        public string ObservedTitle { get; private set; }

        [Inject]
        private void InjectDependencies(IPlayerIdentityService identity, PlayerStatsRegistry registry) {
            _identity = identity;
            _registry = registry;
        }

        public override void OnStartServer() =>
            StartCoroutine(SeedWhenBound());

        private IEnumerator SeedWhenBound() {
            yield return null;
            PlayerKey key = _identity.GetKey(connectionToClient);
            if (_registry.TryGet(key, out IReadOnlyPlayerStatsModel model) == false)
                yield break;

            if (model.Score == 0)
                _bridge.ServerSetScore(7);

            if (string.IsNullOrEmpty(model.Title))
                _bridge.ServerSetTitle("kept");

            ObservedScore = model.Score;
            ObservedTitle = model.Title;
        }
    }
}
