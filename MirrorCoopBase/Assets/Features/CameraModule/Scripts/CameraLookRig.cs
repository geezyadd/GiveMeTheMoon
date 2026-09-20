using UnityEngine;

namespace Features.CameraModule.Scripts {
    public sealed class CameraLookRig : MonoBehaviour {
        public Transform Anchor { get; set; }
        public float Yaw { get; set; }
        public float Pitch { get; set; }

        private void LateUpdate() {
            Transform anchor = Anchor;
            if (anchor == null)
                return;

            if (transform.parent != anchor) {
                transform.SetParent(anchor, false);
                transform.localScale = Vector3.one;
            }

            transform.localPosition = Vector3.zero;
            transform.rotation = Quaternion.Euler(Pitch, Yaw, 0f);
        }
    }
}
