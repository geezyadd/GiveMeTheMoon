using System.Collections.Generic;
using Features.SonarModule.Contracts;
using Features.SonarModule.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Features.SonarModule.Scripts.UI {
    // The round scope: range rings with their distances, the ship's nose up in the middle, north and the station's
    // bearing on the edge, the ping wave as a growing ring, the echoes as buttons from a pool (an echo is as big as
    // its contact, never smaller than its kind's icon), the legend of the kinds, the hint, the ping button with its
    // cooldown fill and the status line.
    public sealed class SonarScopeView : SonarScopeViewBase {
        private const float HALF = 0.5f;

        [SerializeField] private RectTransform _scope;
        [SerializeField] private Image _ringTemplate;
        [SerializeField] private Text _ringLabelTemplate;
        [SerializeField] private SonarLegendRow _legendRowTemplate;
        [SerializeField] private Text _hint;
        [SerializeField] private RectTransform _north;
        [SerializeField] private Text _heading;
        [SerializeField] private RectTransform _stationBearing;
        [SerializeField] private Text _stationDistance;
        [SerializeField] private RectTransform _wave;
        [SerializeField] private Image _waveImage;
        [SerializeField] private SonarBlipButton _blipTemplate;
        [SerializeField] private RectTransform _blipRoot;
        [SerializeField] private Button _pingButton;
        [SerializeField] private Image _cooldownFill;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Text _status;
        [SerializeField] private GameObject _noise;

        private readonly List<SonarBlipButton> _blips = new();
        private readonly List<Text> _ringLabels = new();

        private float ScopeRadiusPixels => _scope.rect.width * HALF;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Test hooks: the loop test presses the window's own buttons, as a player clicks them.
        public Button DebugPingButton => _pingButton;
        public Button DebugLeaveButton => _closeButton;
#endif

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
            _ringLabelTemplate.gameObject.SetActive(false);
            for (int i = 1; i <= ringCount; i++) {
                float share = (float)i / ringCount;
                Image ring = Instantiate(_ringTemplate, _ringTemplate.transform.parent);
                ring.gameObject.SetActive(true);
                ring.rectTransform.localScale = Vector3.one * share;
                Text label = Instantiate(_ringLabelTemplate, _ringLabelTemplate.transform.parent);
                label.gameObject.SetActive(true);
                label.rectTransform.anchoredPosition = Vector2.up * (ScopeRadiusPixels * share);
                _ringLabels.Add(label);
            }
        }

        public override void ShowRingLabel(int ringIndex, string text) =>
            _ringLabels[ringIndex].text = text;

        public override void ShowLegend(IReadOnlyList<SonarContactKind> kinds) {
            _legendRowTemplate.gameObject.SetActive(false);
            foreach (SonarContactKind kind in kinds)
                Instantiate(_legendRowTemplate, _legendRowTemplate.transform.parent).Show(kind);
        }

        public override void ShowHint(string text) =>
            _hint.text = text;

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

        public override void ShowNorth(Vector2 scopePosition) =>
            _north.anchoredPosition = scopePosition * ScopeRadiusPixels;

        public override void ShowHeading(string text) =>
            _heading.text = text;

        public override void ShowStationBearing(bool isShown, Vector2 scopePosition) {
            _stationBearing.gameObject.SetActive(isShown);
            _stationDistance.gameObject.SetActive(isShown);
            if (isShown == false)
                return;

            _stationBearing.anchoredPosition = scopePosition * ScopeRadiusPixels;
            _stationBearing.localRotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.up, scopePosition));
        }

        public override void ShowStationDistance(string text) =>
            _stationDistance.text = text;

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
