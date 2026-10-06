using UnityEngine;

namespace Features.ShipModule.Scripts {
    // The walkable deck of the ship in ship-local space: walk boxes, deck edge, surface height and stand points.
    public sealed class ShipDeckGeometry {
        private readonly Transform _ship;
        private readonly BoxCollider _deck;
        private readonly BoxCollider[] _deckColliders;

        private BoxCollider[] _walkBoxes;

        public Bounds Bounds => _deck.bounds;

        internal ShipDeckGeometry(Transform ship, BoxCollider deck, BoxCollider[] deckColliders) {
            _ship = ship;
            _deck = deck;
            _deckColliders = deckColliders;
        }

        internal bool ContainsDeckWalk(Vector3 localOffset, float inset) {
            if (TryClosestDeckWalk(localOffset, out _, out float dx, out float dz) == false
                || dx * dx + dz * dz > 0.0001f)
                return false;

            Vector3 push = InsetPush(localOffset, inset);
            return push.x * push.x + push.z * push.z <= 0.0001f;
        }

        internal void ClampDeckWalk(ref Vector3 localOffset, float inset) {
            if (TryClosestDeckWalk(localOffset, out Vector3 closestLocal, out float dx, out float dz) == false)
                return;

            if (dx * dx + dz * dz > 0.0001f) {
                localOffset.x = closestLocal.x;
                localOffset.z = closestLocal.z;
            }

            Vector3 push = InsetPush(localOffset, inset);
            if (push.x * push.x + push.z * push.z <= 0.0001f)
                return;

            Vector3 pushed = localOffset + push;
            // On a deck narrower than twice the inset the push can leave the walk boxes: stay on the deck edge instead.
            if (TryClosestDeckWalk(pushed, out closestLocal, out dx, out dz) && dx * dx + dz * dz > 0.0001f)
                pushed = closestLocal;

            localOffset.x = pushed.x;
            localOffset.z = pushed.z;
        }

        internal bool TryGetDeckSurfaceY(Vector3 localOffset, out float surfaceY) {
            surfaceY = localOffset.y;
            BoxCollider[] boxes = WalkBoxes();
            Transform deckTransform = DeckTransform(boxes);
            if (deckTransform == null)
                return false;

            Vector3 deckLocal = deckTransform.InverseTransformPoint(_ship.TransformPoint(localOffset));
            BoxCollider box = FindClosestWalkBox(boxes, deckLocal, out float deckLocalX, out float deckLocalZ);
            if (box == null)
                return false;

            Vector3 deckTop = new Vector3(deckLocalX, box.center.y + box.size.y * 0.5f, deckLocalZ);
            surfaceY = _ship.InverseTransformPoint(deckTransform.TransformPoint(deckTop)).y;
            return true;
        }

        internal bool TryGetDeckStandPoint(out Vector3 shipLocal) {
            shipLocal = Vector3.up;
            BoxCollider[] boxes = WalkBoxes();
            BoxCollider best = null;
            float bestArea = -1f;
            if (boxes != null) {
                for (int i = 0; i < boxes.Length; i++) {
                    BoxCollider box = boxes[i];
                    if (box == null)
                        continue;

                    Vector3 size = Vector3.Scale(box.size, box.transform.lossyScale);
                    float area = Mathf.Abs(size.x * size.z);
                    if (area <= bestArea)
                        continue;

                    bestArea = area;
                    best = box;
                }
            }

            if (best == null)
                return false;

            shipLocal = _ship.InverseTransformPoint(best.transform.TransformPoint(best.center));
            if (TryGetDeckSurfaceY(shipLocal, out float surfaceY))
                shipLocal.y = surfaceY + 1.1f;

            return true;
        }

        internal bool TryClosestDeckWalk(Vector3 localOffset, out Vector3 closestLocal, out float dx, out float dz) {
            closestLocal = localOffset;
            dx = 0f;
            dz = 0f;
            BoxCollider[] boxes = WalkBoxes();
            Transform deckTransform = DeckTransform(boxes);
            if (deckTransform == null)
                return false;

            Vector3 deckLocal = deckTransform.InverseTransformPoint(_ship.TransformPoint(localOffset));
            if (FindClosestWalkBox(boxes, deckLocal, out float bestX, out float bestZ) == null)
                return false;

            closestLocal = _ship.InverseTransformPoint(deckTransform.TransformPoint(new Vector3(bestX, deckLocal.y, bestZ)));
            dx = closestLocal.x - localOffset.x;
            dz = closestLocal.z - localOffset.z;
            return true;
        }

        // The walk boxes overlap at their seams, so shrinking each box would cut gaps into the deck. Instead the point
        // probes the union one inset away along each ship axis and is pushed back by whatever sticks out.
        private Vector3 InsetPush(Vector3 localOffset, float inset) {
            Vector3 push = Vector3.zero;
            if (inset <= 0f)
                return push;

            AddInsetPush(localOffset + Vector3.right * inset, ref push);
            AddInsetPush(localOffset - Vector3.right * inset, ref push);
            AddInsetPush(localOffset + Vector3.forward * inset, ref push);
            AddInsetPush(localOffset - Vector3.forward * inset, ref push);
            return push;
        }

        private void AddInsetPush(Vector3 probe, ref Vector3 push) {
            if (TryClosestDeckWalk(probe, out _, out float dx, out float dz) == false)
                return;

            push.x += dx;
            push.z += dz;
        }

        private static BoxCollider FindClosestWalkBox(BoxCollider[] boxes, Vector3 deckLocal, out float bestX, out float bestZ) {
            float best = float.MaxValue;
            bestX = deckLocal.x;
            bestZ = deckLocal.z;
            BoxCollider closest = null;
            for (int i = 0; i < boxes.Length; i++) {
                BoxCollider box = boxes[i];
                if (box == null || box.enabled == false)
                    continue;

                Vector3 min = box.center - box.size * 0.5f;
                Vector3 max = box.center + box.size * 0.5f;
                float x = Mathf.Clamp(deckLocal.x, min.x, max.x);
                float z = Mathf.Clamp(deckLocal.z, min.z, max.z);
                float cx = x - deckLocal.x;
                float cz = z - deckLocal.z;
                float dist = cx * cx + cz * cz;
                if (dist >= best)
                    continue;

                best = dist;
                bestX = x;
                bestZ = z;
                closest = box;
            }

            return closest;
        }

        private Transform DeckTransform(BoxCollider[] boxes) {
            if (_deck != null)
                return _deck.transform;

            for (int i = 0; i < boxes.Length; i++) {
                if (boxes[i] != null)
                    return boxes[i].transform;
            }

            return null;
        }

        private BoxCollider[] WalkBoxes() {
            if (_walkBoxes != null)
                return _walkBoxes;

            if (_deck != null)
                _walkBoxes = _deck.GetComponents<BoxCollider>();
            else if (_deckColliders != null)
                _walkBoxes = _deckColliders;
            else
                _walkBoxes = new BoxCollider[0];

            return _walkBoxes;
        }
    }
}
