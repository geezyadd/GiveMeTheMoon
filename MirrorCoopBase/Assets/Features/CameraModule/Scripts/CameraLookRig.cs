using UnityEngine;

namespace Features.CameraModule.Scripts {
    public sealed class CameraLookRig : MonoBehaviour {
        public Transform Anchor { get; set; }
        public float Yaw { get; set; }
        public float Pitch { get; set; }

        public void Apply() {
            Transform anchor = Anchor;
            if (anchor == null)
                return;

            if (transform.parent != anchor) {
                transform.SetParent(anchor, false);
                transform.localScale = Vector3.one;
            }

            transform.localPosition = Vector3.zero;
            // Local to the rider, who is parented to the ship, so look does not counter-rotate when the ship turns.
            transform.localRotation = Quaternion.Euler(Pitch, Yaw, 0f);
        }
    }
}
