using Features.EffectPoolModule.Scripts;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.GameCoreModule.Contracts;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Targets {
    // The root of the balloon pop effect prefab: confetti and scraps tinted in the kind's colour (particle systems,
    // authored for a balloon of radius 1 m and scaled by the radius), coins that burst out and are sucked into the
    // ship, a "+N" text that rises and fades facing the viewer, and the pop and coin sound hooks (an AudioSource plays
    // only when a clip is set). It disables itself when all of it is over, so a disabled effect is free for the pool.
    public sealed class FieldTargetPopEffect : MonoBehaviour, ISelfDisablingEffect {
        private const string REWARD_FORMAT = "+{0}";
        // An irrational turn per coin spreads the coins evenly around the pop.
        private const float GOLDEN_ANGLE_DEGREES = 137.50776f;
        // Above the deck, where the coins disappear into the ship.
        private const float SHIP_TARGET_HEIGHT = 2f;
        private const float TEXT_START_HEIGHT_SHARE = 1.2f;
        // The coins leave in three rings: 30 degrees down, level and 30 degrees up.
        private const int COIN_RINGS = 3;
        private const float COIN_RING_SPREAD_DEGREES = 60f;
        private const int MAX_COINS = 32;

        [SerializeField] private ParticleSystem[] _tintedParticles;
        [SerializeField] private Transform _particleRoot;
        [SerializeField] private Transform[] _coins;
        [SerializeField] private TextMesh _rewardText;
        [SerializeField] private AudioSource _popSound;
        [SerializeField] private AudioSource _coinSound;

        private readonly Vector3[] _coinBurstEnds = new Vector3[MAX_COINS];

        private IShipPositionSource _shipPositionSource;
        private ILocalViewpointSource _localViewpointSource;
        private FieldTargetPopFeel _feel;
        private Vector3 _origin;
        private Vector3 _textStart;
        private Color _textColor;
        private float _seconds;
        private int _coinCount;
        private bool _isCoinSoundPlayed;

        public bool IsPlaying => gameObject.activeSelf;

        private void Update() {
            _seconds += Time.deltaTime;
            MoveCoins();
            MoveRewardText();
            if (IsOver())
                gameObject.SetActive(false);
        }

        public void Play(Vector3 position, float radius, int reward, Color color, FieldTargetPopFeel feel, IShipPositionSource shipPositionSource,
                         ILocalViewpointSource localViewpointSource) {
            _shipPositionSource = shipPositionSource;
            _localViewpointSource = localViewpointSource;
            _feel = feel;
            _origin = position;
            _textStart = position + Vector3.up * (radius * TEXT_START_HEIGHT_SHARE);
            _textColor = color;
            _seconds = 0f;
            _coinCount = Mathf.Min(feel.CoinCount, Mathf.Min(_coins.Length, MAX_COINS));
            _isCoinSoundPlayed = false;
            transform.SetPositionAndRotation(position, Quaternion.identity);
            _particleRoot.localScale = Vector3.one * radius;
            _rewardText.text = string.Format(REWARD_FORMAT, reward);
            _rewardText.characterSize = feel.RewardTextSizeMeters;
            PlaceCoins(radius);
            gameObject.SetActive(true);
            TintAndPlay(color);
            PlaySound(_popSound);
            MoveRewardText();
        }

        private void TintAndPlay(Color color) {
            foreach (ParticleSystem particles in _tintedParticles) {
                ParticleSystem.MainModule main = particles.main;
                main.startColor = new ParticleSystem.MinMaxGradient(color, Color.white);
                particles.Play(true);
            }
        }

        private void PlaceCoins(float radius) {
            for (int i = 0; i < _coins.Length; i++) {
                bool isUsed = i < _coinCount;
                _coins[i].gameObject.SetActive(isUsed);
                if (!isUsed)
                    continue;

                float elevation = COIN_RING_SPREAD_DEGREES * ((i % COIN_RINGS) / (float)(COIN_RINGS - 1) - 0.5f);
                Vector3 direction = Quaternion.Euler(elevation, i * GOLDEN_ANGLE_DEGREES, 0f) * Vector3.forward;
                _coins[i].position = _origin + direction * radius;
                _coinBurstEnds[i] = _origin + direction * (radius + _feel.CoinBurstSpeed * _feel.CoinBurstSeconds);
            }
        }

        // Out fast and slowing down, then pulled into the ship faster and faster.
        private void MoveCoins() {
            float burst = Mathf.Clamp01(_seconds / _feel.CoinBurstSeconds);
            float fly = Mathf.Clamp01((_seconds - _feel.CoinBurstSeconds) / _feel.CoinFlySeconds);
            bool hasShip = _shipPositionSource.TryGetShipPosition(out Vector3 ship);
            Vector3 target = hasShip ? ship + Vector3.up * SHIP_TARGET_HEIGHT : _origin + Vector3.up * _feel.RewardTextRiseMeters;
            for (int i = 0; i < _coinCount; i++) {
                Transform coin = _coins[i];
                if (!coin.gameObject.activeSelf)
                    continue;

                Vector3 burstPoint = Vector3.Lerp(_origin, _coinBurstEnds[i], 1f - (1f - burst) * (1f - burst));
                coin.position = Vector3.Lerp(burstPoint, target, fly * fly);
                if (fly >= 1f)
                    coin.gameObject.SetActive(false);
            }

            if (fly >= 1f && !_isCoinSoundPlayed) {
                _isCoinSoundPlayed = true;
                PlaySound(_coinSound);
            }
        }

        private void MoveRewardText() {
            float progress = Mathf.Clamp01(_seconds / _feel.RewardTextSeconds);
            Transform text = _rewardText.transform;
            text.position = _textStart + Vector3.up * (_feel.RewardTextRiseMeters * (1f - (1f - progress) * (1f - progress)));
            Vector3 fromViewer = text.position - _localViewpointSource.GetPosition();
            if (fromViewer.sqrMagnitude > 0f)
                text.rotation = Quaternion.LookRotation(fromViewer, Vector3.up);

            Color color = _textColor;
            color.a = 1f - progress * progress;
            _rewardText.color = color;
        }

        private bool IsOver() {
            bool areCoinsDone = _seconds >= _feel.CoinBurstSeconds + _feel.CoinFlySeconds;
            bool isTextDone = _seconds >= _feel.RewardTextSeconds;
            return areCoinsDone && isTextDone && !AreParticlesAlive();
        }

        private bool AreParticlesAlive() {
            foreach (ParticleSystem particles in _tintedParticles) {
                if (particles.IsAlive(true))
                    return true;
            }

            return false;
        }

        private static void PlaySound(AudioSource source) {
            if (source.clip != null)
                source.Play();
        }
    }
}
