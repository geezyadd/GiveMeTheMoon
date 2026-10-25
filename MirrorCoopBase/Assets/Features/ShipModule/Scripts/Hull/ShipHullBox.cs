using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.ShipModule.Scripts.Hull {
    // One box of the hull volume in ship space.
    [Serializable]
    public struct ShipHullBox {
        public Vector3 Center;
        public Vector3 HalfExtents;

        // One box around all the boxes: what the field is asked about before every box is tested on its own.
        public static ShipHullBox Enclose(IReadOnlyList<ShipHullBox> boxes) {
            Bounds bounds = new(boxes[0].Center, boxes[0].HalfExtents * 2f);
            for (int i = 1; i < boxes.Count; i++)
                bounds.Encapsulate(new Bounds(boxes[i].Center, boxes[i].HalfExtents * 2f));

            return new ShipHullBox { Center = bounds.center, HalfExtents = bounds.extents };
        }
    }
}
