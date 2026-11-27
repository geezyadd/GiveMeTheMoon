using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Features.SavingSystemModule.Contracts;
using Newtonsoft.Json;
using UnityEngine;

namespace Features.SavingSystemModule {
    // ISaveService over the data in memory (SaveDataStore) and the encrypted files (SaveFileStorage).
    public sealed class SaveService : ISaveService {
        private readonly SaveDataStore _store;
        private readonly SaveFileStorage _storage;

        public SaveService(SaveDataStore store, SaveFileStorage storage) {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public void LoadFiles() =>
            ReadIntoStore(_storage.ReadAll());

        public async UniTask LoadFilesAsync() =>
            ReadIntoStore(await _storage.ReadAllAsync());

        public void Save(string id, object data, string fileName) =>
            _store.Set(id, data, fileName);

        public void SaveRange(IReadOnlyDictionary<string, object> dataById, string fileName) {
            foreach (KeyValuePair<string, object> entry in dataById)
                _store.Set(entry.Key, entry.Value, fileName);
        }

        public void Commit(string fileName) =>
            _storage.Write(fileName, _store.WriteFileContent(fileName));

        public UniTask CommitAsync(string fileName) =>
            _storage.WriteAsync(fileName, _store.WriteFileContent(fileName));

        public bool Contains(string id) =>
            _store.Contains(id);

        public T Load<T>(string id) =>
            _store.TryGet(id, out T data) ? data : default;

        public bool TryLoad<T>(string id, out T data) =>
            _store.TryGet(id, out data);

        public void LoadInto(string id, object target) =>
            _store.TryPopulate(id, target);

        public Dictionary<string, T> LoadRange<T>(IEnumerable<string> ids) {
            Dictionary<string, T> dataById = new();
            foreach (string id in ids)
                if (_store.TryGet(id, out T data))
                    dataById[id] = data;

            return dataById;
        }

        public void Delete(string id) =>
            _store.Remove(id);

        public void DeleteRange(IEnumerable<string> ids) {
            foreach (string id in ids)
                _store.Remove(id);
        }

        public void DeleteFile(string fileName) {
            _storage.Delete(fileName);
            _store.RemoveFile(fileName);
        }

        private void ReadIntoStore(Dictionary<string, string> contentByFileName) {
            _store.Clear();
            foreach (KeyValuePair<string, string> file in contentByFileName) {
                try {
                    _store.ReadFileContent(file.Key, file.Value);
                }
                catch (JsonException exception) {
                    Debug.LogWarning($"[SaveService] Skipped the save file '{file.Key}': its content is not valid ({exception.Message}).");
                }
            }
        }
    }
}
