using UnityEngine;

namespace Features.SavingSystemModule {
    // Where the save files go and the passphrase their content is encrypted with.
    [CreateAssetMenu(
        fileName = nameof(SavingSystemConfiguration) + "_Default",
        menuName = "Configurations/SavingSystemModule/" + nameof(SavingSystemConfiguration))]
    public sealed class SavingSystemConfiguration : ScriptableObject {
        [Tooltip("Folder under Application.persistentDataPath that holds the save files. Steam Auto-Cloud is set up for this folder.")]
        [SerializeField] private string _saveFolderName = "Saves";
        [Tooltip("Passphrase the AES key of the save files is made from. Changing it makes the existing save files unreadable.")]
        [SerializeField] private string _encryptionPassphrase = "";

        public string SaveFolderName => _saveFolderName;
        public string EncryptionPassphrase => _encryptionPassphrase;
    }
}
