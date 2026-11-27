# SavingSystemModule — local save files (PC, Steam)

Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description
here, not in the root file.

## The idea
Data is saved by a string ID into a named save file. `ISaveService.Save` / `SaveRange` / `Delete` / `DeleteRange`
change the data in memory only; `Commit(fileName)` / `CommitAsync(fileName)` writes that one file to disk. At start a
caller runs `LoadFiles()` / `LoadFilesAsync()` once to read every file into memory; after that `Load<T>`, `TryLoad`,
`LoadInto` (writes the saved fields into an existing object), `LoadRange` and `Contains` read from memory by ID,
whatever file the ID is in. An ID lives in one file at a time: saving it into another file moves it there (commit both
files). `DeleteFile` removes a file from disk and its data from memory at once.

Data goes through Newtonsoft JSON: save plain data classes (public fields or properties). Unity types such as
`Vector3` are not covered by any converter here.

Nothing uses the module yet: it is bound in DI and waits for its first caller.

## Files
- Folder: `Application.persistentDataPath/<SaveFolderName>/<SteamID>/`, one `<fileName>.sav` per file. Without an
  initialized Steam (no Steam client, or before `SteamManager` in `GlobalScene` has run `SteamAPI.Init`) the last part
  is `local`. The folder is picked on the first file operation and kept for the whole run, so call `LoadFiles` after
  Steam is up.
- Content: a JSON object of ID → data JSON, encrypted with AES-256-CBC and signed with HMAC-SHA256
  (`IV | encrypted | HMAC`). Both keys are SHA-256 of the configuration's passphrase with different prefixes; every
  write gets a random IV. A wrong passphrase or an edited file fails the HMAC check. The passphrase ships with the
  game, so this stops casual editing, not a determined player. **Changing the passphrase makes all existing saves
  unreadable.**
- A write goes to `<fileName>.sav.tmp` and then replaces the old file, so a crash mid-write keeps the previous save.
  Writes run one at a time.
- A file that cannot be decrypted or parsed is skipped with a warning and left on disk untouched.

## Steam Cloud
Steam Auto-Cloud, no code. To turn it on, in the Steamworks app admin → Cloud: set the byte and file quotas, then add
a root override / path: root `WinAppDataLocalLow` (where `persistentDataPath` is on Windows), subdirectory
`<companyName>/<productName>/<SaveFolderName>/{64BitSteamID}`
(now `DefaultCompany/MirrorCoopBase/Saves/{64BitSteamID}`: set the real company and product names in Player Settings first,
they move the folder), pattern `*.sav`, OS Windows. Not set up yet.

## Assemblies
- `Features.SavingSystemModule.Contracts` (layer 1; UniTask): `ISaveService`.
- `Features.SavingSystemModule` (layer 2; Contracts, UniTask, Zenject, Steamworks.NET, Newtonsoft.Json):
  `SaveService` (the `ISaveService`), `SaveDataStore` (data in memory), `SaveFileStorage` (the `.sav` files),
  `SaveFileCipher` (AES), `ISaveFolderPath` / `SteamUserSaveFolderPath`, `SavingSystemConfiguration`,
  `SavingSystemModuleInstaller` (called from GameCore's `ProjectContextInstaller`, so the service lives the whole game).

## Key places
| What | Where |
|---|---|
| Configuration | `GameResources/Configurations/SavingSystemConfiguration_Default.asset` (Addressables `Configurations` group, bound in `ConfigurationInstaller`): save folder name (`Saves`), encryption passphrase |
| UniTask | `com.cysharp.unitask` 2.5.10 in `Packages/manifest.json`, added with this module (2026-11-27) |

## Tests
`Features.SavingSystemModule.Editor.Tests` (EditMode, a temporary folder instead of the Steam folder): the cipher
(round trip, random IV, wrong passphrase, a changed byte, empty passphrase) and the service (load before commit, commit and reload,
encrypted bytes, one file per commit, load into an object, ranges, delete and commit, an ID moving between files,
delete file, a file with another passphrase skipped, an invalid file name, async commit and load).

## Known limits
- The Steam folder part is not checked with a real Steam user yet; Auto-Cloud is not set up in Steamworks.
- No save versioning or migration: a renamed field loads as its default.
