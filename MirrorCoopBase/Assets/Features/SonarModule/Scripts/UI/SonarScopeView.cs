using System.Collections.Generic;
using Features.SonarModule.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Features.SonarModule.Scripts.UI {
    // The round scope: range rings, the ship's nose up in the middle, the ping wave as a growing ring, the echoes as
    // buttons from a pool, the ping button with its cooldown fill and the status line.
    public sealed class SonarScopeView : SonarScopeViewBase {
        private const float HALF = 0.5f;

        [SerializeField] private RectTransform _scope;
        [SerializeField] private Image _ringTemplate;
        [SerializeField] private RectTransform _wave;
        [SerializeField] private Image _waveImage;
        [SerializeField] private SonarBlipButton _blipTemplate;
        [SerializeField] private RectTransform _blipRoot;
        [SerializeField] private Button _pingButton;
        [SerializeField] private Image _cooldownFill;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Text _status;
        [SerializeField] private GameObject _noise;
        [Tooltip("The ping sound's place: plays its clip on every ping when one is set.")]
        [SerializeField] private AudioSource _pingAudio;

        private readonly List<SonarBlipButton> _blips = new();
        private readonly List<Image> _rings = new();

        private float ScopeRadiusPixels => _scope.rect.width * HALF;

        protected override void OnEnable() {
            base.OnEnable();
            _pingButton.onClick.AddListener(OnPingButtonClicked);
            _closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        protected override void OnDisable() {
            _pingButton.onClick.RemoveListener(OnPingButtonClicked);
            _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            base.OnDisable();
        }

        public override void DisposeView() {
            foreach (SonarBlipButton blip in _blips)
                blip.OnClicked -= OnBlipButtonClicked;

            base.DisposeView();
        }

        public override void SetRings(int ringCount) {
            _ringTemplate.gameObject.SetActive(false);
            for (int i = 1; i <= ringCount; i++) {
                Image ring = Instantiate(_ringTemplate, _ringTemplate.transform.parent);
                ring.gameObject.SetActive(true);
                ring.rectTransform.localScale = Vector3.one * ((float)i / ringCount);
                _rings.Add(ring);
            }
        }

        public override void ShowBlips(IReadOnlyList<SonarBlip> blips) {
            while (_blips.Count < blips.Count)
                _blips.Add(CreateBlip());

            float radius = ScopeRadiusPixels;
            for (int i = 0; i < _blips.Count; i++) {
                if (i < blips.Count)
                    _blips[i].Show(blips[i], radius);
                else
                    _blips[i].Hide();
            }
        }

        public override void ShowWave(float share) {
            bool isShown = share >= 0f;
            _wave.gameObject.SetActive(isShown);
            if (isShown == false)
                return;

            _wave.localScale = Vector3.one * share;
            Color color = _waveImage.color;
            color.a = 1f - share;
            _waveImage.color = color;
        }

        public override void ShowCooldown(float share) {
            _cooldownFill.fillAmount = share;
            _pingButton.interactable = share >= 1f;
        }

        public override void ShowStatus(string text) =>
            _status.text = text;

        public override void ShowNoise(bool isShown) =>
            _noise.SetActive(isShown);

        public override void PlayPing() {
            if (_pingAudio.clip != null)
                _pingAudio.Play();
        }

        private SonarBlipButton CreateBlip() {
            SonarBlipButton blip = Instantiate(_blipTemplate, _blipRoot);
            blip.OnClicked += OnBlipButtonClicked;
            return blip;
        }

        private void OnPingButtonClicked() =>
            InvokePingClicked();

        private void OnCloseButtonClicked() =>
            InvokeCloseClicked();

        private void OnBlipButtonClicked(string contactId) =>
            InvokeBlipClicked(contactId);
    }
}
