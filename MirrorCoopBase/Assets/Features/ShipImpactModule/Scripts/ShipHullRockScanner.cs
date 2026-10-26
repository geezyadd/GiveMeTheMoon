using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.ShipModule.Scripts.Contracts;
using Features.ShipModule.Scripts.Hull;
using UnityEngine;

namespace Features.ShipImpactModule.Scripts {
    // Asks the field data (not the scene: the host may not have the rocks built) for the rocks under the swept hull:
    // one query around all the boxes, then every box on its own. A rock is touched at a share of its data sphere per
    // kind: the sphere holds the turned mesh with room to spare, so the whole sphere hits rocks the hull visibly
    // passes by. The query list is reused, so a scan allocates nothing once it has grown.
    public sealed class ShipHullRockScanner : IShipHullRockScanner {
        private readonly IFieldRockQuery _fieldRockQuery;
        private readonly IShipRockImpactClassifier _shipRockImpactClassifier;
        private readonly IShipRockContactRule _shipRockContactRule;
        private readonly ShipRockImpactConfiguration _shipRockImpactConfiguration;
        private readonly List<FieldRockDescription> _rocks = new();

        public ShipHullRockScanner(IFieldRockQuery fieldRockQuery, IShipRockImpactClassifier shipRockImpactClassifier, IShipRockContactRule shipRockContactRule,
                                   ShipRockImpactConfiguration shipRockImpactConfiguration) {
            _fieldRockQuery = fieldRockQuery;
            _shipRockImpactClassifier = shipRockImpactClassifier;
            _shipRockContactRule = shipRockContactRule;
            _shipRockImpactConfiguration = shipRockImpactConfiguration;
        }

        public void Scan(IReadOnlyList<ShipHullBox> boxes, Pose from, Pose to, Vector3 largeLookAhead, List<ShipRockContact> contacts) {
            contacts.Clear();
            float largeMargin = _shipRockImpactConfiguration.LargeContactMarginMeters;
            Pose ahead = new(to.position + largeLookAhead, to.rotation);
            ShipHullSweep query = ShipHullSweep.Create(ShipHullBox.Enclose(boxes), from, ahead).Grow(largeMargin);
            _fieldRockQuery.FindInBox(query.Center, query.HalfExtents, query.Rotation, _rocks);
            foreach (FieldRockDescription rock in _rocks) {
                ShipRockImpactKind kind = _shipRockImpactClassifier.Classify(rock);
                float contactRadius = _shipRockContactRule.CalculateContactRadius(kind, rock.Radius);
                bool isTouching = kind == ShipRockImpactKind.Small
                    ? TryTouch(boxes, from, to, 0f, rock.Center, contactRadius, out Vector3 hullPoint)
                    : TryTouch(boxes, from, ahead, largeMargin, rock.Center, contactRadius, out hullPoint);
                if (isTouching)
                    contacts.Add(new ShipRockContact(rock, kind, hullPoint));
            }
        }

        private static bool TryTouch(IReadOnlyList<ShipHullBox> boxes, Pose from, Pose to, float margin, Vector3 rockCenter, float rockRadius, out Vector3 hullPoint) {
            hullPoint = default;
            for (int i = 0; i < boxes.Count; i++) {
                ShipHullSweep sweep = ShipHullSweep.Create(boxes[i], from, to).Grow(margin);
                if (!sweep.Touches(rockCenter, rockRadius))
                    continue;

                hullPoint = sweep.ClosestPoint(rockCenter);
                return true;
            }

            return false;
        }
    }
}
