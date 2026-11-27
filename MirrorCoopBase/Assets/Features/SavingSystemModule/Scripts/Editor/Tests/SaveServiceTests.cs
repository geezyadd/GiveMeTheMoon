using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Features.SavingSystemModule.Editor.Tests {
    public sealed class SaveServiceTests {
        private const string PASSPHRASE = "test passphrase";
        private const string PROGRESS_FILE = "progress";
        private const string SETTINGS_FILE = "settings";

        private string _folderPath;

        [SetUp]
        public void SetUp() =>
            _folderPath = Path.Combine(Path.GetTempPath(), "SaveServiceTests_" + System.Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown() {
            if (Directory.Exists(_folderPath))
                Directory.Delete(_folderPath, true);
        }

        [Test]
        public void WhenDataIsSaved_ThenItLoadsBackBeforeAnyCommit() {
            SaveService service = CreateService();

            service.Save("coins", 125, PROGRESS_FILE);

            Assert.IsTrue(service.Contains("coins"));
            Assert.AreEqual(125, service.Load<int>("coins"));
        }

        [Test]
        public void WhenThereIsNoDataForTheId_ThenLoadReturnsDefaultAndTryLoadFails() {
            SaveService service = CreateService();

            Assert.AreEqual(0, service.Load<int>("missing"));
            Assert.IsFalse(service.TryLoad("missing", out int _));
        }

        [Test]
        public void WhenAFileIsCommitted_ThenANewServiceLoadsItsData() {
            SaveService service = CreateService();
            service.Save("progress", new TestProgress { Level = 3, Name = "crew" }, PROGRESS_FILE);
            service.Commit(PROGRESS_FILE);

            SaveService reloaded = CreateService();
            reloaded.LoadFiles();

            TestProgress progress = reloaded.Load<TestProgress>("progress");
            Assert.AreEqual(3, progress.Level);
            Assert.AreEqual("crew", progress.Name);
            Assert.IsTrue(File.Exists(Path.Combine(_folderPath, PROGRESS_FILE + SaveFileStorage.SAVE_FILE_EXTENSION)));
        }

        [Test]
        public void WhenAFileIsCommitted_ThenItsBytesDoNotHoldTheData() {
            SaveService service = CreateService();
            service.Save("name", "VisibleCrewName", PROGRESS_FILE);
            service.Commit(PROGRESS_FILE);

            byte[] bytes = File.ReadAllBytes(Path.Combine(_folderPath, PROGRESS_FILE + SaveFileStorage.SAVE_FILE_EXTENSION));
            StringAssert.DoesNotContain("VisibleCrewName", Encoding.UTF8.GetString(bytes));
        }

        [Test]
        public void WhenOnlyOneFileIsCommitted_ThenTheOtherFilesDataIsNotOnDisk() {
            SaveService service = CreateService();
            service.Save("coins", 125, PROGRESS_FILE);
            service.Save("volume", 0.5f, SETTINGS_FILE);
            service.Commit(PROGRESS_FILE);

            SaveService reloaded = CreateService();
            reloaded.LoadFiles();

            Assert.IsTrue(reloaded.Contains("coins"));
            Assert.IsFalse(reloaded.Contains("volume"));
        }

        [Test]
        public void WhenLoadedInto_ThenTheExistingObjectGetsTheSavedFields() {
            SaveService service = CreateService();
            service.Save("progress", new TestProgress { Level = 7, Name = "saved" }, PROGRESS_FILE);
            TestProgress target = new() { Level = 1, Name = "fresh" };

            service.LoadInto("progress", target);

            Assert.AreEqual(7, target.Level);
            Assert.AreEqual("saved", target.Name);
        }

        [Test]
        public void WhenARangeIsSaved_ThenLoadRangeReturnsOnlyTheIdsWithData() {
            SaveService service = CreateService();
            service.SaveRange(new Dictionary<string, object> { ["a"] = 1, ["b"] = 2 }, PROGRESS_FILE);

            Dictionary<string, int> loaded = service.LoadRange<int>(new[] { "a", "b", "missing" });

            Assert.AreEqual(2, loaded.Count);
            Assert.AreEqual(1, loaded["a"]);
            Assert.AreEqual(2, loaded["b"]);
        }

        [Test]
        public void WhenDeletedAndCommitted_ThenTheDataIsGoneAfterReload() {
            SaveService service = CreateService();
            service.SaveRange(new Dictionary<string, object> { ["a"] = 1, ["b"] = 2, ["c"] = 3 }, PROGRESS_FILE);
            service.Commit(PROGRESS_FILE);

            service.Delete("a");
            service.DeleteRange(new[] { "b" });
            service.Commit(PROGRESS_FILE);
            SaveService reloaded = CreateService();
            reloaded.LoadFiles();

            Assert.IsFalse(reloaded.Contains("a"));
            Assert.IsFalse(reloaded.Contains("b"));
            Assert.AreEqual(3, reloaded.Load<int>("c"));
        }

        [Test]
        public void WhenAnIdIsSavedIntoAnotherFile_ThenItMovesToThatFile() {
            SaveService service = CreateService();
            service.Save("coins", 1, PROGRESS_FILE);
            service.Save("coins", 2, SETTINGS_FILE);
            service.Commit(PROGRESS_FILE);
            service.Commit(SETTINGS_FILE);

            SaveService reloaded = CreateService();
            reloaded.LoadFiles();

            Assert.AreEqual(2, reloaded.Load<int>("coins"));
            reloaded.DeleteFile(SETTINGS_FILE);
            Assert.IsFalse(reloaded.Contains("coins"));
        }

        [Test]
        public void WhenAFileIsDeleted_ThenItIsGoneFromDiskAndMemory() {
            SaveService service = CreateService();
            service.Save("coins", 125, PROGRESS_FILE);
            service.Commit(PROGRESS_FILE);

            service.DeleteFile(PROGRESS_FILE);

            Assert.IsFalse(service.Contains("coins"));
            Assert.IsFalse(File.Exists(Path.Combine(_folderPath, PROGRESS_FILE + SaveFileStorage.SAVE_FILE_EXTENSION)));
        }

        [Test]
        public void WhenAFileWasWrittenWithAnotherPassphrase_ThenItIsSkippedWithAWarning() {
            SaveService service = CreateService("another passphrase");
            service.Save("coins", 125, PROGRESS_FILE);
            service.Commit(PROGRESS_FILE);
            SaveService reloaded = CreateService();

            LogAssert.Expect(LogType.Warning, new Regex("Skipped the save file"));
            reloaded.LoadFiles();

            Assert.IsFalse(reloaded.Contains("coins"));
            Assert.IsTrue(File.Exists(Path.Combine(_folderPath, PROGRESS_FILE + SaveFileStorage.SAVE_FILE_EXTENSION)));
        }

        [Test]
        public void WhenTheFileNameIsNotValid_ThenCommitThrows() {
            SaveService service = CreateService();

            Assert.Throws<System.ArgumentException>(() => service.Commit("bad/name"));
        }

        [UnityTest]
        public IEnumerator WhenAFileIsCommittedAsync_ThenANewServiceLoadsItAsync() => UniTask.ToCoroutine(async () => {
            SaveService service = CreateService();
            service.Save("coins", 125, PROGRESS_FILE);
            await service.CommitAsync(PROGRESS_FILE);
            service.Save("coins", 126, PROGRESS_FILE);
            await service.CommitAsync(PROGRESS_FILE);

            SaveService reloaded = CreateService();
            await reloaded.LoadFilesAsync();

            Assert.AreEqual(126, reloaded.Load<int>("coins"));
            Assert.IsEmpty(Directory.GetFiles(_folderPath, "*.tmp"));
        });

        private SaveService CreateService(string passphrase = PASSPHRASE) =>
            new(new SaveDataStore(), new SaveFileStorage(new FixedSaveFolderPath(_folderPath), new SaveFileCipher(passphrase)));

        private sealed class FixedSaveFolderPath : ISaveFolderPath {
            private readonly string _folderPath;

            public FixedSaveFolderPath(string folderPath) =>
                _folderPath = folderPath;

            public string GetFolderPath() =>
                _folderPath;
        }

        private sealed class TestProgress {
            public int Level;
            public string Name;
        }
    }
}
