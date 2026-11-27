namespace Features.SavingSystemModule {
    // The folder the save files are read from and written to.
    public interface ISaveFolderPath {
        string GetFolderPath();
    }
}
