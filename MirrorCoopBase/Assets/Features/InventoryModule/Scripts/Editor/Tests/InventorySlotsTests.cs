using Features.InventoryModule.Scripts;
using NUnit.Framework;

namespace Features.InventoryModule.Editor.Tests {
    // The slot rules: where a pickup goes, what a slot key does, the wheel's wrap and the hand item.
    public sealed class InventorySlotsTests {
        private const int SLOT_COUNT = 4;
        private const uint PISTOL = 11;
        private const uint WRENCH = 12;
        private const uint HAMMER = 13;
        private const uint ROCKET_LAUNCHER = 14;

        [Test]
        public void WhenTheSelectedSlotIsEmpty_ThenAPickupGoesThere() =>
            Assert.AreEqual(2, Slots(new uint[] { PISTOL, 0, 0, 0 }, 2, false).FindPickupSlot());

        [Test]
        public void WhenTheSelectedSlotIsTaken_ThenAPickupGoesToTheFirstFreeSlot() =>
            Assert.AreEqual(2, Slots(new uint[] { PISTOL, WRENCH, 0, 0 }, 0, false).FindPickupSlot());

        [Test]
        public void WhenTheSelectedSlotIsStowed_ThenAPickupStillGoesToTheFirstFreeSlot() =>
            Assert.AreEqual(1, Slots(new uint[] { PISTOL, 0, 0, 0 }, 0, true).FindPickupSlot());

        [Test]
        public void WhenEverySlotIsTaken_ThenThereIsNoPickupSlot() {
            InventorySlots slots = Slots(new uint[] { PISTOL, WRENCH, HAMMER, ROCKET_LAUNCHER }, 1, false);

            Assert.AreEqual(InventorySlots.NO_SLOT, slots.FindPickupSlot());
            Assert.IsTrue(slots.IsFull);
        }

        [Test]
        public void WhenTheSlotsAreNotSyncedYet_ThenTheyCountAsEmpty() {
            InventorySlots slots = Slots(new uint[0], 0, false);

            Assert.AreEqual(0, slots.FindPickupSlot());
            Assert.AreEqual(0u, slots.HandItemNetId);
        }

        [Test]
        public void WhenAnotherSlotIsSelected_ThenItsItemIsInTheHand() {
            InventorySlots slots = Slots(new uint[] { PISTOL, ROCKET_LAUNCHER, 0, 0 }, 0, false);

            InventorySelection selection = slots.Select(1);

            Assert.AreEqual(1, selection.Slot);
            Assert.IsFalse(selection.IsStowed);
            Assert.AreEqual(ROCKET_LAUNCHER, slots.HandItemNetIdFor(selection));
        }

        [Test]
        public void WhenAnotherSlotIsSelectedFromAStowedOne_ThenItsItemIsShown() =>
            Assert.IsFalse(Slots(new uint[] { PISTOL, WRENCH, 0, 0 }, 0, true).Select(1).IsStowed);

        [Test]
        public void WhenTheSelectedSlotIsPressedAgain_ThenItsItemIsStowedAndTheHandIsEmpty() {
            InventorySlots slots = Slots(new uint[] { PISTOL, 0, 0, 0 }, 0, false);

            InventorySelection selection = slots.Select(0);

            Assert.IsTrue(selection.IsStowed);
            Assert.AreEqual(0u, slots.HandItemNetIdFor(selection));
        }

        [Test]
        public void WhenAStowedSlotIsPressedAgain_ThenItsItemIsBackInTheHand() {
            InventorySlots slots = Slots(new uint[] { PISTOL, 0, 0, 0 }, 0, true);

            InventorySelection selection = slots.Select(0);

            Assert.IsFalse(selection.IsStowed);
            Assert.AreEqual(PISTOL, slots.HandItemNetIdFor(selection));
        }

        [Test]
        public void WhenAnEmptySelectedSlotIsPressedAgain_ThenNothingChanges() {
            InventorySelection selection = Slots(new uint[] { 0, PISTOL, 0, 0 }, 0, false).Select(0);

            Assert.AreEqual(0, selection.Slot);
            Assert.IsFalse(selection.IsStowed);
        }

        [Test]
        public void WhenTheWheelTurnsPastTheLastSlot_ThenItWrapsToTheFirst() =>
            Assert.AreEqual(0, Slots(new uint[4], 3, false).Cycle(3, 1));

        [Test]
        public void WhenTheWheelTurnsBeforeTheFirstSlot_ThenItWrapsToTheLast() =>
            Assert.AreEqual(3, Slots(new uint[4], 0, false).Cycle(0, -1));

        [Test]
        public void WhenAnItemIsLookedUp_ThenItsSlotIsFoundAndAnEmptyIdIsInNoSlot() {
            InventorySlots slots = Slots(new uint[] { 0, 0, HAMMER, 0 }, 0, false);

            Assert.AreEqual(2, slots.IndexOf(HAMMER));
            Assert.AreEqual(InventorySlots.NO_SLOT, slots.IndexOf(0));
            Assert.AreEqual(InventorySlots.NO_SLOT, slots.IndexOf(PISTOL));
        }

        [Test]
        public void WhenASlotIsOutOfRange_ThenItIsNotValid() {
            InventorySlots slots = Slots(new uint[4], 0, false);

            Assert.IsFalse(slots.IsValidSlot(-1));
            Assert.IsFalse(slots.IsValidSlot(SLOT_COUNT));
            Assert.IsTrue(slots.IsValidSlot(SLOT_COUNT - 1));
        }

        private static InventorySlots Slots(uint[] itemNetIds, int selected, bool isStowed) =>
            new InventorySlots(itemNetIds, SLOT_COUNT, new InventorySelection(selected, isStowed));
    }
}
