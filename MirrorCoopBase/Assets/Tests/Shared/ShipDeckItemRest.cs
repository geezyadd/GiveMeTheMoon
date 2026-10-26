#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Features.ShipModule.Scripts;
using UnityEngine;

namespace Tests.Shared {
    // How an item lies on the deck, in ship space: the gap between its box's lowest corner and the deck surface under
    // it. Zero for an item lying on the deck, the height it hangs at for one frozen in the air. Shared by the loop test
    // and the coop test client, so the host and the client measure the same.
    public static class ShipDeckItemRest {
        private const int BOX_CORNERS = 8;

        public static float MeasureGapAboveDeck(ShipBase ship, BoxCollider box) {
            Transform shipTransform = ship.transform;
            Vector3 local = shipTransform.InverseTransformPoint(box.transform.TransformPoint(box.center));
            if (ship.DeckGeometry.TryGetDeckSurfaceY(local, out float surfaceY) == false)
                throw new InvalidOperationException($"{ship.name} has no deck surface under {box.name} at {local}.");

            return LowestCornerY(shipTransform, box) - surfaceY;
        }

        private static float LowestCornerY(Transform ship, BoxCollider box) {
            Vector3 half = box.size * 0.5f;
            float lowest = float.PositiveInfinity;
            for (int corner = 0; corner < BOX_CORNERS; corner++) {
                Vector3 sign = new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f);
                Vector3 world = box.transform.TransformPoint(box.center + Vector3.Scale(half, sign));
                lowest = Mathf.Min(lowest, ship.InverseTransformPoint(world).y);
            }

            return lowest;
        }
    }
}
#endif
