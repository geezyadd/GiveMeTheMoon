using System;
using NUnit.Framework;

namespace Features.PlayerProfileModule.Scripts.Editor.Tests {
    public sealed class PlayerNameSanitizerTests {
        private const int MAX_LENGTH = 16;
        private const string FALLBACK_NAME = "Player 2";

        private PlayerNameSanitizer _sanitizer;

        [SetUp]
        public void SetUp() =>
            _sanitizer = new PlayerNameSanitizer();

        [Test]
        public void WhenNameIsPlain_ThenKept() =>
            Assert.That(Sanitize("Vasya"), Is.EqualTo("Vasya"));

        [Test]
        public void WhenNameHasOuterSpaces_ThenTrimmed() =>
            Assert.That(Sanitize("   Vasya  "), Is.EqualTo("Vasya"));

        [Test]
        public void WhenNameHasLineBreaksAndTabs_ThenTheyBecomeOneSpace() =>
            Assert.That(Sanitize("Vasya\r\n\tPupkin"), Is.EqualTo("Vasya Pupkin"));

        [Test]
        public void WhenNameHasControlCharacters_ThenRemoved() =>
            Assert.That(Sanitize("Va\u0000s\u0007ya\u001B"), Is.EqualTo("Vasya"));

        [Test]
        public void WhenNameHasInvisibleFormatCharacters_ThenRemoved() =>
            Assert.That(Sanitize("\u202EVa\u200Bsya\uFEFF"), Is.EqualTo("Vasya"));

        [Test]
        public void WhenNameIsNull_ThenFallback() =>
            Assert.That(Sanitize(null), Is.EqualTo(FALLBACK_NAME));

        [Test]
        public void WhenNameIsOnlyWhitespaceAndControls_ThenFallback() =>
            Assert.That(Sanitize(" \n\t\u0000\u200B "), Is.EqualTo(FALLBACK_NAME));

        [Test]
        public void WhenNameIsAtLimit_ThenKeptWhole() =>
            Assert.That(Sanitize("ABCDEFGHIJKLMNOP"), Is.EqualTo("ABCDEFGHIJKLMNOP"));

        [Test]
        public void WhenNameIsOverLimit_ThenCutWithEllipsisToLimit() {
            string sanitized = Sanitize(new string('A', 60));

            Assert.That(sanitized, Is.EqualTo(new string('A', MAX_LENGTH - 1) + "\u2026"));
        }

        [Test]
        public void WhenCutEndsOnSpace_ThenSpaceDroppedBeforeEllipsis() =>
            Assert.That(Sanitize("ABCDEFGHIJKLMN OPQRSTU"), Is.EqualTo("ABCDEFGHIJKLMN\u2026"));

        [Test]
        public void WhenCutSplitsSurrogatePair_ThenWholePairDropped() {
            string name = new string('A', MAX_LENGTH - 2) + "\U0001F600" + "BBB";

            Assert.That(Sanitize(name), Is.EqualTo(new string('A', MAX_LENGTH - 2) + "\u2026"));
        }

        [Test]
        public void WhenNameIsCyrillic_ThenKept() =>
            Assert.That(Sanitize("Вася Їжак"), Is.EqualTo("Вася Їжак"));

        [Test]
        public void WhenNameHasRichTextTags_ThenKeptAsText() =>
            Assert.That(Sanitize("<color=red>Vasya"), Is.EqualTo("<color=red>Vasya"));

        [Test]
        public void WhenLimitLeavesNoRoomForEllipsis_ThenThrows() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => _sanitizer.Sanitize("Vasya", 1, FALLBACK_NAME));

        private string Sanitize(string rawName) =>
            _sanitizer.Sanitize(rawName, MAX_LENGTH, FALLBACK_NAME);
    }
}
