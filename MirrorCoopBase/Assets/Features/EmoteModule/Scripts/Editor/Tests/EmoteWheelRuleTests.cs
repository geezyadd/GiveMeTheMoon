using NUnit.Framework;
using UnityEngine;

namespace Features.EmoteModule.Editor.Tests {
    public sealed class EmoteWheelRuleTests {
        private const float DeadZone = 30f;

        [Test]
        public void WhenThePointerIsInTheDeadZone_ThenNoSectorIsPicked() {
            Assert.AreEqual(EmoteWheelRule.NoSector, EmoteWheelRule.PickSector(Vector2.zero, 4, DeadZone));
            Assert.AreEqual(EmoteWheelRule.NoSector, EmoteWheelRule.PickSector(new Vector2(0f, 29f), 4, DeadZone));
            Assert.AreEqual(EmoteWheelRule.NoSector, EmoteWheelRule.PickSector(new Vector2(20f, -20f), 4, DeadZone));
        }

        [Test]
        public void WhenThePointerIsOnTheDeadZoneEdge_ThenItsSectorIsPicked() =>
            Assert.AreEqual(0, EmoteWheelRule.PickSector(new Vector2(0f, DeadZone), 4, DeadZone));

        [Test]
        public void WhenThereAreNoSectors_ThenNoneIsPicked() =>
            Assert.AreEqual(EmoteWheelRule.NoSector, EmoteWheelRule.PickSector(Vector2.up * 100f, 0, DeadZone));

        [Test]
        public void WhenFourSectors_ThenTheTopIsFirstAndTheRestGoClockwise() {
            Assert.AreEqual(0, EmoteWheelRule.PickSector(Vector2.up * 100f, 4, DeadZone));
            Assert.AreEqual(1, EmoteWheelRule.PickSector(Vector2.right * 100f, 4, DeadZone));
            Assert.AreEqual(2, EmoteWheelRule.PickSector(Vector2.down * 100f, 4, DeadZone));
            Assert.AreEqual(3, EmoteWheelRule.PickSector(Vector2.left * 100f, 4, DeadZone));
        }

        [Test]
        public void WhenThePointerIsJustEitherSideOfTheTop_ThenTheTopSectorIsPicked() {
            Assert.AreEqual(0, EmoteWheelRule.PickSector(Direction(44f) * 100f, 4, DeadZone));
            Assert.AreEqual(0, EmoteWheelRule.PickSector(Direction(-44f) * 100f, 4, DeadZone));
        }

        [Test]
        public void WhenThePointerCrossesASectorEdge_ThenTheNextSectorIsPicked() {
            Assert.AreEqual(0, EmoteWheelRule.PickSector(Direction(44.9f) * 100f, 4, DeadZone));
            Assert.AreEqual(1, EmoteWheelRule.PickSector(Direction(45.1f) * 100f, 4, DeadZone));
            Assert.AreEqual(3, EmoteWheelRule.PickSector(Direction(-45.1f) * 100f, 4, DeadZone));
        }

        [Test]
        public void WhenThreeSectors_ThenEachCoversAThirdOfTheTurn() {
            Assert.AreEqual(0, EmoteWheelRule.PickSector(Direction(59f) * 100f, 3, DeadZone));
            Assert.AreEqual(1, EmoteWheelRule.PickSector(Direction(61f) * 100f, 3, DeadZone));
            Assert.AreEqual(1, EmoteWheelRule.PickSector(Direction(179f) * 100f, 3, DeadZone));
            Assert.AreEqual(2, EmoteWheelRule.PickSector(Direction(181f) * 100f, 3, DeadZone));
            Assert.AreEqual(2, EmoteWheelRule.PickSector(Direction(299f) * 100f, 3, DeadZone));
            Assert.AreEqual(0, EmoteWheelRule.PickSector(Direction(301f) * 100f, 3, DeadZone));
        }

        [Test]
        public void WhenOneSector_ThenEveryDirectionPicksIt() {
            Assert.AreEqual(0, EmoteWheelRule.PickSector(Vector2.down * 100f, 1, DeadZone));
            Assert.AreEqual(0, EmoteWheelRule.PickSector(Direction(179.9f) * 100f, 1, DeadZone));
        }

        [Test]
        public void WhenASectorsCentreIsAsked_ThenItIsItsShareOfTheTurnClockwiseFromTheTop() {
            Assert.AreEqual(0f, EmoteWheelRule.SectorCenterDegrees(0, 4), 0.001f);
            Assert.AreEqual(90f, EmoteWheelRule.SectorCenterDegrees(1, 4), 0.001f);
            Assert.AreEqual(Vector2.right.x, EmoteWheelRule.SectorDirection(1, 4).x, 0.001f);
            Assert.AreEqual(Vector2.right.y, EmoteWheelRule.SectorDirection(1, 4).y, 0.001f);
        }

        [Test]
        public void WhenTheSectorCentreIsPicked_ThenItIsThatSector() {
            for (int count = 1; count <= 8; count++)
            for (int index = 0; index < count; index++)
                Assert.AreEqual(index, EmoteWheelRule.PickSector(EmoteWheelRule.SectorDirection(index, count) * 100f, count, DeadZone));
        }

        [Test]
        public void WhenThePointerMovesFarOut_ThenItStaysOnTheWheelsRadius() {
            Vector2 pointer = EmoteWheelRule.MovePointer(Vector2.zero, new Vector2(500f, 0f), 120f);
            Assert.AreEqual(120f, pointer.magnitude, 0.001f);

            pointer = EmoteWheelRule.MovePointer(pointer, new Vector2(-150f, 0f), 120f);
            Assert.AreEqual(-30f, pointer.x, 0.001f);
        }

        // A direction at the given degrees clockwise from the top, y up.
        private static Vector2 Direction(float degrees) {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
        }
    }
}
