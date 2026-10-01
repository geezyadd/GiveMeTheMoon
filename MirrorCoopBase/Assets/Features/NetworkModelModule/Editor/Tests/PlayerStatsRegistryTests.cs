using Features.NetworkModelModule.Scripts;
using Features.NetworkModelModule.Scripts.Samples;
using NUnit.Framework;

namespace Features.NetworkModelModule.Scripts.Editor.Tests {
    public sealed class PlayerStatsRegistryTests {
        [Test]
        public void WhenOfflineAndReboundWithSameKey_ThenScoreStays() {
            PlayerKey key = new PlayerKey("client:abc");
            PlayerStatsRegistry registry = new PlayerStatsRegistry();
            int added = 0;
            int removed = 0;
            int onlineChanges = 0;
            registry.OnPlayerAdded += (playerKey, model) => added++;
            registry.OnPlayerRemoved += playerKey => removed++;
            registry.OnOnlineChanged += (playerKey, online) => onlineChanges++;

            PlayerStatsModel first = registry.Bind(key, true, true);
            first.ApplyScore(7);
            first.ApplyTitle("kept");
            registry.MarkOffline(key);

            Assert.That(registry.IsOnline(key), Is.False);
            Assert.That(registry.TryGet(key, out IReadOnlyPlayerStatsModel stored), Is.True);
            Assert.That(stored.Score, Is.EqualTo(7));

            PlayerStatsModel second = registry.Bind(key, true, true);

            Assert.That(second, Is.SameAs(first));
            Assert.That(second.Score, Is.EqualTo(7));
            Assert.That(second.Title, Is.EqualTo("kept"));
            Assert.That(registry.IsOnline(key), Is.True);
            Assert.That(registry.Local, Is.SameAs(first));
            Assert.That(added, Is.EqualTo(1));
            Assert.That(removed, Is.EqualTo(0));
            Assert.That(onlineChanges, Is.EqualTo(3));
        }
    }
}
