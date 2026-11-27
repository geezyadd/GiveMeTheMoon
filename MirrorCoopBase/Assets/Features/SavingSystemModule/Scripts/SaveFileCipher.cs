using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Features.SavingSystemModule {
    // AES-256-CBC over the file text, then an HMAC-SHA256 over the IV and the encrypted bytes.
    // Layout: IV (16 bytes) | encrypted text | HMAC (32 bytes). Both keys are SHA-256 of the passphrase with a
    // different prefix; every file gets a random IV. The HMAC makes a wrong passphrase or an edited file fail
    // for sure instead of decrypting into garbage. The passphrase ships with the game, so this stops casual
    // editing of the files, not a determined player.
    public sealed class SaveFileCipher {
        private const int IV_SIZE = 16;
        private const int HMAC_SIZE = 32;
        private const string ENCRYPTION_KEY_PREFIX = "encryption:";
        private const string AUTHENTICATION_KEY_PREFIX = "authentication:";

        private readonly byte[] _encryptionKey;
        private readonly byte[] _authenticationKey;

        public SaveFileCipher(string passphrase) {
            if (string.IsNullOrEmpty(passphrase))
                throw new ArgumentException("The save encryption passphrase is empty.", nameof(passphrase));

            _encryptionKey = DeriveKey(ENCRYPTION_KEY_PREFIX + passphrase);
            _authenticationKey = DeriveKey(AUTHENTICATION_KEY_PREFIX + passphrase);
        }

        public byte[] Encrypt(string text) {
            using Aes aes = CreateAes();
            aes.GenerateIV();

            using MemoryStream output = new();
            output.Write(aes.IV, 0, IV_SIZE);
            using (ICryptoTransform encryptor = aes.CreateEncryptor()) {
                byte[] textBytes = Encoding.UTF8.GetBytes(text);
                byte[] encrypted = encryptor.TransformFinalBlock(textBytes, 0, textBytes.Length);
                output.Write(encrypted, 0, encrypted.Length);
            }

            byte[] hmac = ComputeHmac(output.GetBuffer(), (int)output.Length);
            output.Write(hmac, 0, HMAC_SIZE);
            return output.ToArray();
        }

        // Throws CryptographicException when the bytes were not made with this passphrase or were changed.
        public string Decrypt(byte[] data) {
            if (data == null || data.Length <= IV_SIZE + HMAC_SIZE)
                throw new CryptographicException("The save file is too short to be encrypted data.");

            int signedLength = data.Length - HMAC_SIZE;
            if (HmacMatches(ComputeHmac(data, signedLength), data, signedLength) is false)
                throw new CryptographicException("The save file was not written with this passphrase or was changed.");

            using Aes aes = CreateAes();
            byte[] iv = new byte[IV_SIZE];
            Array.Copy(data, iv, IV_SIZE);
            aes.IV = iv;

            using ICryptoTransform decryptor = aes.CreateDecryptor();
            byte[] textBytes = decryptor.TransformFinalBlock(data, IV_SIZE, signedLength - IV_SIZE);
            return Encoding.UTF8.GetString(textBytes);
        }

        private Aes CreateAes() {
            Aes aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = _encryptionKey;
            return aes;
        }

        private byte[] ComputeHmac(byte[] data, int length) {
            using HMACSHA256 hmac = new(_authenticationKey);
            return hmac.ComputeHash(data, 0, length);
        }

        // Compares every byte, so the time taken does not tell how many leading bytes matched.
        private static bool HmacMatches(byte[] expected, byte[] data, int hmacOffset) {
            int difference = 0;
            for (int index = 0; index < HMAC_SIZE; index++)
                difference |= expected[index] ^ data[hmacOffset + index];

            return difference == 0;
        }

        private static byte[] DeriveKey(string text) {
            using SHA256 sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        }
    }
}
