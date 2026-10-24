using System;
using Features.WeaponModule.Scripts.Network;
using NUnit.Framework;
using UnityEngine;

namespace Features.WeaponModule.Editor.Tests {
    public sealed class WeaponHitQueueTests {
        private const int WEAPON_INDEX = 2;
        private const int QUEUE_CAPACITY = 32;
        private const int WAITING_HITS = 6;

        [Test]
        public void WhenSixHitsWait_ThenFirstBatchCarriesFourInOrderAndTheNextTheRest() {
            WeaponHitQueue queue = new WeaponHitQueue();
            for (int i = 0; i < WAITING_HITS; i++)
                queue.TryEnqueue(CreateHit(i));

            WeaponHitBatch first = queue.DequeueBatch(WEAPON_INDEX);
            WeaponHitBatch second = queue.DequeueBatch(WEAPON_INDEX);

            Assert.AreEqual(WeaponHitBatch.CAPACITY, first.Count);
            Assert.AreEqual(WEAPON_INDEX, first.WeaponIndex);
            for (int i = 0; i < first.Count; i++)
                Assert.AreEqual((ulong)i, first.Get(i).TargetId);

            Assert.AreEqual(WAITING_HITS - WeaponHitBatch.CAPACITY, second.Count);
            Assert.AreEqual((ulong)WeaponHitBatch.CAPACITY, second.Get(0).TargetId);
            Assert.AreEqual(0, queue.Count);
        }

        [Test]
        public void WhenQueueIsFull_ThenNewerHitsAreDropped() {
            WeaponHitQueue queue = new WeaponHitQueue();
            for (int i = 0; i < QUEUE_CAPACITY; i++)
                Assert.IsTrue(queue.TryEnqueue(CreateHit(i)));

            Assert.IsFalse(queue.TryEnqueue(CreateHit(QUEUE_CAPACITY)));
            Assert.AreEqual(0UL, queue.DequeueBatch(WEAPON_INDEX).Get(0).TargetId);
        }

        [Test]
        public void WhenBatchSlotIsOutsideTheBatch_ThenItThrows() =>
            Assert.Throws<ArgumentOutOfRangeException>(ReadSlotPastCapacity);

        private static void ReadSlotPastCapacity() =>
            new WeaponHitBatch().Get(WeaponHitBatch.CAPACITY);

        private static WeaponHitReport CreateHit(int targetId) =>
            new WeaponHitReport((ulong)targetId, Vector3.one * targetId, Vector3.up);
    }
}
