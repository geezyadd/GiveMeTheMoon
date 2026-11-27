using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Features.SavingSystemModule.Contracts {
    // Saved data by string ID, grouped into named save files (<fileName>.sav).
    // Save / Delete change the data in memory only; Commit writes one file to disk.
    // An ID lives in one file at a time: saving it into another file moves it there.
    public interface ISaveService {
        // Reads every save file into memory, replacing what is in memory. Call once at start.
        void LoadFiles();
        UniTask LoadFilesAsync();

        void Save(string id, object data, string fileName);
        void SaveRange(IReadOnlyDictionary<string, object> dataById, string fileName);

        void Commit(string fileName);
        UniTask CommitAsync(string fileName);

        bool Contains(string id);
        // Returns default when there is no data for the ID.
        T Load<T>(string id);
        bool TryLoad<T>(string id, out T data);
        // Writes the saved fields into an existing object; does nothing when there is no data for the ID.
        void LoadInto(string id, object target);
        // Holds only the IDs that have data.
        Dictionary<string, T> LoadRange<T>(IEnumerable<string> ids);

        void Delete(string id);
        void DeleteRange(IEnumerable<string> ids);
        // Removes the file from disk and its data from memory at once, no commit needed.
        void DeleteFile(string fileName);
    }
}
