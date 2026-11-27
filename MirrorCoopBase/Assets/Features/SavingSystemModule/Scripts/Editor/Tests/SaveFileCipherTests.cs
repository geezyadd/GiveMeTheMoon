using System;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;

namespace Features.SavingSystemModule.Editor.Tests {
    public sealed class SaveFileCipherTests {
        private const string TEXT = "{\"coins\":\"125\"}";

        [Test]
        public void WhenTextIsEncrypted_ThenItDecryptsBackToTheSameText() {
            SaveFileCipher cipher = new("passphrase");

            Assert.AreEqual(TEXT, cipher.Decrypt(cipher.Encrypt(TEXT)));
        }

        [Test]
        public void WhenTextIsEncrypted_ThenTheBytesDoNotHoldTheText() {
            byte[] encrypted = new SaveFileCipher("passphrase").Encrypt(TEXT);

            StringAssert.DoesNotContain("coins", Encoding.UTF8.GetString(encrypted));
        }

        [Test]
        public void WhenTheSameTextIsEncryptedTwice_ThenTheBytesDiffer() {
            SaveFileCipher cipher = new("passphrase");

            CollectionAssert.AreNotEqual(cipher.Encrypt(TEXT), cipher.Encrypt(TEXT));
        }

        [Test]
        public void WhenDecryptedWithAnotherPassphrase_ThenItThrows() {
            byte[] encrypted = new SaveFileCipher("passphrase").Encrypt(TEXT);

            Assert.Throws(Is.InstanceOf<CryptographicException>(),
                () => new SaveFileCipher("another passphrase").Decrypt(encrypted));
        }

        [Test]
        public void WhenAByteOfTheEncryptedDataIsChanged_ThenDecryptThrows() {
            SaveFileCipher cipher = new("passphrase");
            byte[] encrypted = cipher.Encrypt(TEXT);
            encrypted[20] ^= 1;

            Assert.Throws(Is.InstanceOf<CryptographicException>(), () => cipher.Decrypt(encrypted));
        }

        [Test]
        public void WhenPassphraseIsEmpty_ThenCreationThrows() =>
            Assert.Throws<ArgumentException>(() => new SaveFileCipher(""));
    }
}
