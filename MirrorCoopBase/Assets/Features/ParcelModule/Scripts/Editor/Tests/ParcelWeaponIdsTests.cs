using Features.ParcelModule.Scripts.Services;
using NUnit.Framework;

namespace Features.ParcelModule.Scripts.Editor.Tests {
    // A parcel crate's weapon target id comes back to its crate; another family's id is no parcel.
    public sealed class ParcelWeaponIdsTests {
        private const uint CRATE_NET_ID = 4242;
        private const ulong BUG_TARGET_ID = 0xB000000000000001UL;

        private readonly ParcelWeaponIds _ids = new();

        [Test]
        public void WhenACrateBecomesAWeaponTargetId_ThenItComesBackAsTheSameCrate() {
            Assert.IsTrue(_ids.TryGetCrateNetId(_ids.GetTargetId(CRATE_NET_ID), out uint back));
            Assert.AreEqual(CRATE_NET_ID, back);
        }

        [Test]
        public void WhenABugsWeaponTargetIdIsRead_ThenItIsNoParcel() =>
            Assert.IsFalse(_ids.TryGetCrateNetId(BUG_TARGET_ID, out _));
    }
}
