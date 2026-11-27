using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Features.SavingSystemModule {
    // The saved data in memory: file name → (ID → the data as JSON). An ID lives in one file at a time.
    public sealed class SaveDataStore {
        private readonly Dictionary<string, Dictionary<string, string>> _jsonByIdByFileName = new();
        private readonly Dictionary<string, string> _fileNameById = new();

        public void Set(string id, object data, string fileName) {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("The save ID is empty.", nameof(id));

            Remove(id);
            if (_jsonByIdByFileName.TryGetValue(fileName, out Dictionary<string, string> jsonById) is false)
                _jsonByIdByFileName[fileName] = jsonById = new Dictionary<string, string>();

            jsonById[id] = JsonConvert.SerializeObject(data);
            _fileNameById[id] = fileName;
        }

        public bool Contains(string id) =>
            _fileNameById.ContainsKey(id);

        public bool TryGet<T>(string id, out T data) {
            if (TryGetJson(id, out string json) is false) {
                data = default;
                return false;
            }

            data = JsonConvert.DeserializeObject<T>(json);
            return true;
        }

        public bool TryPopulate(string id, object target) {
            if (TryGetJson(id, out string json) is false)
                return false;

            JsonConvert.PopulateObject(json, target);
            return true;
        }

        public void Remove(string id) {
            if (_fileNameById.Remove(id, out string fileName))
                _jsonByIdByFileName[fileName].Remove(id);
        }

        public void RemoveFile(string fileName) {
            if (_jsonByIdByFileName.Remove(fileName, out Dictionary<string, string> jsonById) is false)
                return;

            foreach (string id in jsonById.Keys)
                _fileNameById.Remove(id);
        }

        public void Clear() {
            _jsonByIdByFileName.Clear();
            _fileNameById.Clear();
        }

        // The whole file's content: a JSON object of ID → data JSON. An empty object for an unknown file.
        public string WriteFileContent(string fileName) =>
            JsonConvert.SerializeObject(
                _jsonByIdByFileName.TryGetValue(fileName, out Dictionary<string, string> jsonById)
                    ? jsonById
                    : new Dictionary<string, string>());

        // Throws JsonException when the content is not a file content; the memory is left as it was then.
        public void ReadFileContent(string fileName, string content) {
            Dictionary<string, string> jsonById = JsonConvert.DeserializeObject<Dictionary<string, string>>(content)
                ?? new Dictionary<string, string>();

            RemoveFile(fileName);
            foreach (KeyValuePair<string, string> entry in jsonById) {
                Remove(entry.Key);
                _fileNameById[entry.Key] = fileName;
            }

            _jsonByIdByFileName[fileName] = jsonById;
        }

        private bool TryGetJson(string id, out string json) {
            json = null;
            return _fileNameById.TryGetValue(id, out string fileName)
                && _jsonByIdByFileName[fileName].TryGetValue(id, out json);
        }
    }
}
