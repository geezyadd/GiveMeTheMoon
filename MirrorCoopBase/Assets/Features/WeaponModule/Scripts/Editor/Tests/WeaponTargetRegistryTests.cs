using System;
using Features.WeaponModule.Contracts;
using Features.WeaponModule.Scripts.Targets;
using NUnit.Framework;

namespace Features.WeaponModule.Editor.Tests {
    // The server's target lookup: a target registered by its id, or a source answering for its own id range.
    public sealed class WeaponTargetRegistryTests {
        private const ulong TARGET_ID = 42UL;
        private const ulong SOURCE_ID = 0xF100000000000007UL;
        private const ulong UNKNOWN_ID = 7UL;

        [Test]
        public void WhenASourceAnswersForAnId_ThenTheLookupFindsItsTarget() {
            WeaponTargetRegistry registry = new WeaponTargetRegistry();
            CountingWeaponTarget sourced = new CountingWeaponTarget();
            registry.RegisterSource(new SingleIdTargetSource(SOURCE_ID, sourced));

            Assert.IsTrue(registry.TryGet(SOURCE_ID, out IWeaponTarget found));
            Assert.AreSame(sourced, found);
            Assert.IsFalse(registry.TryGet(UNKNOWN_ID, out _));
        }

        [Test]
        public void WhenAnIdIsRegisteredAndASourceAnswersToo_ThenTheRegisteredTargetWins() {
            WeaponTargetRegistry registry = new WeaponTargetRegistry();
            CountingWeaponTarget registered = new CountingWeaponTarget();
            registry.Register(TARGET_ID, registered);
            registry.RegisterSource(new SingleIdTargetSource(TARGET_ID, new CountingWeaponTarget()));

            Assert.IsTrue(registry.TryGet(TARGET_ID, out IWeaponTarget found));
            Assert.AreSame(registered, found);
        }

        [Test]
        public void WhenASourceIsUnregistered_ThenItsIdsAreUnknown() {
            WeaponTargetRegistry registry = new WeaponTargetRegistry();
            SingleIdTargetSource source = new SingleIdTargetSource(SOURCE_ID, new CountingWeaponTarget());
            registry.RegisterSource(source);

            registry.UnregisterSource(source);

            Assert.IsFalse(registry.TryGet(SOURCE_ID, out _));
        }

        [Test]
        public void WhenASourceRegistersTwice_ThenItThrows() {
            WeaponTargetRegistry registry = new WeaponTargetRegistry();
            SingleIdTargetSource source = new SingleIdTargetSource(SOURCE_ID, new CountingWeaponTarget());
            registry.RegisterSource(source);

            Assert.Throws<InvalidOperationException>(() => registry.RegisterSource(source));
        }
    }
}
