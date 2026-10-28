using Features.WeaponModule.Contracts;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Ore {
    // The root of an ore prefab: the trigger a rope strikes (layer FieldRock) with its weapon target id, a glowing
    // look that turns slowly and dims as the piece fades, and the rope line drawn from the hooker's weapon while the
    // piece is pulled. The builder binds it while the piece is in the field and unbinds it when it goes back to the
    // pool (collider off, look reset). The server never reads it: a reported hit is checked against the live field.
    public sealed class FieldOreView : MonoBehaviour, IWeaponTargetCollider {
        private const string EMISSION_COLOR = "_EmissionColor";
        private const int ROPE_POINT_COUNT = 2;

        private static readonly int _emissionColorId = Shader.PropertyToID(EMISSION_COLOR);

        [SerializeField] private Collider _collider;
        [SerializeField] private Transform _visual;
        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private LineRenderer _rope;

        private MaterialPropertyBlock _propertyBlock;
        private Color[] _emissions;
        private float _spinDegreesPerSecond;
        private float _shownShare = -1f;

        public ulong TargetId { get; private set; }

        private void Awake() {
            _propertyBlock = new MaterialPropertyBlock();
            _emissions = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
                _emissions[i] = _renderers[i].sharedMaterial.GetColor(_emissionColorId);

            _rope.useWorldSpace = true;
            _rope.positionCount = ROPE_POINT_COUNT;
        }

        private void Update() =>
            _visual.Rotate(Vector3.up, _spinDegreesPerSecond * Time.deltaTime, Space.Self);

        public void Bind(ulong targetId, float spinDegreesPerSecond) {
            TargetId = targetId;
            _spinDegreesPerSecond = spinDegreesPerSecond;
            _collider.enabled = true;
            HideRope();
        }

        public void Unbind() {
            TargetId = 0;
            _collider.enabled = false;
            HideRope();
            Show(1f);
        }

        // The piece shrinks and dims together as it fades.
        public void Place(Vector3 position, float visibleShare) {
            transform.position = position;
            Show(visibleShare);
        }

        public void ShowRope(Vector3 from) {
            _rope.enabled = true;
            _rope.SetPosition(0, from);
            _rope.SetPosition(1, transform.position);
        }

        public void HideRope() =>
            _rope.enabled = false;

        private void Show(float visibleShare) {
            if (Mathf.Approximately(visibleShare, _shownShare))
                return;

            _shownShare = visibleShare;
            _visual.localScale = Vector3.one * visibleShare;
            for (int i = 0; i < _renderers.Length; i++) {
                _renderers[i].GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(_emissionColorId, _emissions[i] * visibleShare);
                _renderers[i].SetPropertyBlock(_propertyBlock);
            }
        }
    }
}
