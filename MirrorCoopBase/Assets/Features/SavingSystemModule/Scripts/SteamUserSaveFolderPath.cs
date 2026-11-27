using System;
using System.IO;
using Steamworks;
using UnityEngine;

namespace Features.SavingSystemModule {
    // persistentDataPath/<save folder>/<SteamID>, or .../local when Steam is not initialized.
    // The folder is picked on first use and kept for the whole run, so files never split between two folders.
    public sealed class SteamUserSaveFolderPath : ISaveFolderPath {
        private const string NO_STEAM_USER_FOLDER_NAME = "local";

        private readonly SavingSystemConfiguration _configuration;
        private string _folderPath;

        public SteamUserSaveFolderPath(SavingSystemConfiguration configuration) =>
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        public string GetFolderPath() =>
            _folderPath ??= Path.Combine(Application.persistentDataPath, _configuration.SaveFolderName, GetUserFolderName());

        private static string GetUserFolderName() {
            try {
                CSteamID steamId = SteamUser.GetSteamID();
                return steamId.IsValid() ? steamId.m_SteamID.ToString() : NO_STEAM_USER_FOLDER_NAME;
            }
            catch (Exception) {
                // Steam is not running or SteamAPI is not initialized yet.
                return NO_STEAM_USER_FOLDER_NAME;
            }
        }
    }
}
