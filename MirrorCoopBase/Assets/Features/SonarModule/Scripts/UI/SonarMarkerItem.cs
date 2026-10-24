using Features.SonarModule.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Features.SonarModule.Scripts.UI {
    // One world marker: the kind's icon in a soft glow at a constant size with the distance under it, and an
    // arrow turned to the object while it is off screen.
    public sealed class SonarMarkerItem : MonoBehaviour {
        private const string DISTANCE_FORMAT = "{0:N0} m";
        private const int NO_METERS_SHOWN = -1;

        [SerializeField] private RectTransform _rect;
        [SerializeField] private Image _icon;
        [SerializeField] private Image _glow;
        [SerializeField] private RectTransform _arrow;
        [SerializeField] private Image _arrowImage;
        [SerializeField] private Text _distance;

        private int _shownMeters = NO_METERS_SHOWN;

        public void Show(SonarMarkerDisplay marker, Vector2 localPosition) {
            gameObject.SetActive(true);
            _rect.anchoredPosition = localPosition;
            _rect.sizeDelta = Vector2.one * marker.Kind.IconPixels;
            _icon.sprite = marker.Kind.Icon;
            _icon.color = marker.Kind.Color;
            Color glow = marker.Kind.Color;
            glow.a = _glow.color.a;
            _glow.color = glow;
            _arrow.gameObject.SetActive(marker.IsOnScreen == false);
            _arrow.localEulerAngles = new Vector3(0f, 0f, marker.ArrowDegrees);
            _arrowImage.color = marker.Kind.Color;
            ShowDistance(Mathf.RoundToInt(marker.DistanceMeters));
        }

        public void Hide() =>
            gameObject.SetActive(false);

        // The text is rebuilt only when the whole metres change.
        private void ShowDistance(int meters) {
            if (meters == _shownMeters)
                return;

            _shownMeters = meters;
            _distance.text = string.Format(DISTANCE_FORMAT, meters);
        }
    }
}
