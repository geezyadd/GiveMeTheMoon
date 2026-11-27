using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.SavingSystemModule {
    // Encrypted <fileName>.sav files in the save folder. A write goes to a temporary file first and then
    // replaces the old one, so a crash in the middle of a write keeps the previous save.
    // A file that cannot be decrypted is skipped with a warning and left on disk as it is.
    public sealed class SaveFileStorage {
        public const string SAVE_FILE_EXTENSION = ".sav";
        private const string TEMPORARY_FILE_EXTENSION = ".tmp";

        private readonly ISaveFolderPath _folderPath;
        private readonly SaveFileCipher _cipher;
        // One write at a time, so two commits of the same file never share its temporary file.
        private readonly SemaphoreSlim _writeLock = new(1, 1);

        public SaveFileStorage(ISaveFolderPath folderPath, SaveFileCipher cipher) {
            _folderPath = folderPath ?? throw new ArgumentNullException(nameof(folderPath));
            _cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
        }

        public void Write(string fileName, string content) {
            byte[] encrypted = _cipher.Encrypt(content);
            _writeLock.Wait();
            try {
                string filePath = PrepareFilePath(fileName);
                File.WriteAllBytes(filePath + TEMPORARY_FILE_EXTENSION, encrypted);
                ReplaceWithTemporaryFile(filePath);
            }
            finally {
                _writeLock.Release();
            }
        }

        public async UniTask WriteAsync(string fileName, string content) {
            byte[] encrypted = _cipher.Encrypt(content);
            // ConfigureAwait(false): the lock is released off the main thread, so a sync Write waiting on the
            // main thread cannot block the end of this one.
            await _writeLock.WaitAsync().ConfigureAwait(false);
            try {
                string filePath = PrepareFilePath(fileName);
                await File.WriteAllBytesAsync(filePath + TEMPORARY_FILE_EXTENSION, encrypted).ConfigureAwait(false);
                ReplaceWithTemporaryFile(filePath);
            }
            finally {
                _writeLock.Release();
            }

            await UniTask.SwitchToMainThread();
        }

        // File name (without the extension) → decrypted content.
        public Dictionary<string, string> ReadAll() {
            Dictionary<string, string> contentByFileName = new();
            foreach (string filePath in GetSaveFilePaths())
                if (TryDecrypt(filePath, File.ReadAllBytes(filePath), out string content))
                    contentByFileName[Path.GetFileNameWithoutExtension(filePath)] = content;

            return contentByFileName;
        }

        public async UniTask<Dictionary<string, string>> ReadAllAsync() {
            Dictionary<string, string> contentByFileName = new();
            foreach (string filePath in GetSaveFilePaths()) {
                byte[] encrypted = await File.ReadAllBytesAsync(filePath);
                if (TryDecrypt(filePath, encrypted, out string content))
                    contentByFileName[Path.GetFileNameWithoutExtension(filePath)] = content;
            }

            return contentByFileName;
        }

        public void Delete(string fileName) {
            string filePath = GetFilePath(fileName);
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        private IEnumerable<string> GetSaveFilePaths() {
            string folderPath = _folderPath.GetFolderPath();
            // The extension is checked again: on Windows "*.sav" also matches longer extensions such as ".save".
            return Directory.Exists(folderPath)
                ? Directory.GetFiles(folderPath, "*" + SAVE_FILE_EXTENSION)
                    .Where(path => Path.GetExtension(path) == SAVE_FILE_EXTENSION)
                : Enumerable.Empty<string>();
        }

        private bool TryDecrypt(string filePath, byte[] encrypted, out string content) {
            try {
                content = _cipher.Decrypt(encrypted);
                return true;
            }
            catch (CryptographicException exception) {
                Debug.LogWarning($"[SaveFileStorage] Skipped the save file {filePath}: it cannot be decrypted ({exception.Message}).");
                content = null;
                return false;
            }
        }

        private string PrepareFilePath(string fileName) {
            Directory.CreateDirectory(_folderPath.GetFolderPath());
            return GetFilePath(fileName);
        }

        private string GetFilePath(string fileName) {
            if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException($"'{fileName}' is not a valid save file name.", nameof(fileName));

            return Path.Combine(_folderPath.GetFolderPath(), fileName + SAVE_FILE_EXTENSION);
        }

        private static void ReplaceWithTemporaryFile(string filePath) {
            string temporaryPath = filePath + TEMPORARY_FILE_EXTENSION;
            if (File.Exists(filePath))
                File.Replace(temporaryPath, filePath, null);
            else
                File.Move(temporaryPath, filePath);
        }
    }
}
