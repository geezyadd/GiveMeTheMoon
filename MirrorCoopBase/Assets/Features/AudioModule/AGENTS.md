# AudioModule — playing sounds through FMOD

Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description
here, not in the root file.

## The idea
Every game sound goes through `IAudioService`, never through `RuntimeManager` or `AudioSource` in a feature module.
The service will grow: long and looping sounds, parameters, buses and VCAs for the settings. Sounds are local: each
peer plays a sound where it sees its cause, nothing goes over the network.

## Status
Started 2026-11-27: one-shots only. No sound goes over the network of its own: each hooks onto something every peer
already gets. Users now:
- ExplosionModule: `ExplosionPreset.Sound` at the centre when the look plays (all three presets on
  `event:/Weapon/explosion_mid` for now).
- WeaponModule: `WeaponFeelProfile.ShotSound` at the muzzle and `ImpactSound` where a shot strikes, played by
  `WeaponShotPresenter`, which shows each shot once on every peer. Pistol and knockdown gun: `blaster_shot` /
  `blaster_hit`; grenade and rocket launchers: `rocket_launch`, no impact sound (their explosion has its own); the
  turret tiers have none.
- GrabModule: `Grabbable.EquipSound` where an item comes into a hand (pickup or taken out of an inventory slot), played
  by `HeldItemRegistry` on every peer from the hand model's change. The four hand weapons use `weapon_equip`; any other
  item gets one by setting the field.

## FMOD
- The FMOD for Unity integration is in `Assets/Plugins/FMOD` (assembly `FMODUnity`); the FMOD Studio project is
  `Assets/FMOD/Project/GiveMeTheMoon/GiveMeTheMoon.fspro`, its banks are built into its `Build` folder and the editor
  reads them from there (FMOD settings: source project). Its `.cache/` and `.user/` and the integration's
  `Plugins/FMOD/Cache/` are git-ignored.
- A feature module holds a sound as an `FMODUnity.EventReference` field (picked in the inspector) and references
  `FMODUnity` and `Features.AudioModule` in its asmdef.

## Assembly
`Features.AudioModule` (layer 2; references Zenject, FMODUnity, Features.SettingsModule.Data). Bindings: `AudioModuleInstaller`, called from
GameCore's `GlobalSceneInstaller`.

## Parts
- `IAudioService` / `FmodAudioService`: `PlayOneShot(sound, position)` (3D, at a world point) and
  `PlayOneShot2D(sound)` (no position: the event should have no spatializer in FMOD Studio). An empty reference plays
  nothing.
- `SoundInstance` (`IAudioService.CreateInstance(sound)`): a long sound (music, ambience, loops) around one FMOD
  `EventInstance`: `Start`, `Stop(fadeOut)`, `IsPlaying`. Its owner must `Dispose` it: that stops it with its fade and
  releases the instance. An empty reference gives an instance that does nothing.
- `Ambience/AmbienceConfiguration` (`GameResources/Configurations/AmbienceConfiguration_Default`, Addressables group
  `Configurations`, bound in `ConfigurationInstaller`): `SessionMusic` (`event:/Music/tension_music`). GameCore's
  `AmbienceSystem` (bound in `GlobalSceneInstaller`) starts it on entering `SessionGameFlowState` (the lobby comes first)
  and stops it on leaving to the menu; it keeps playing in the game scene. It disposes the instance with the container.
- `SetBusVolume(busPath, volume)`: an FMOD Studio bus's volume (0–1); a bus the banks lack is warned about once.
- `Volume/AudioVolumeSettingsApplier` (bound in `AudioModuleInstaller`): the settings `audio.master_volume`,
  `audio.music_volume`, `audio.sfx_volume` (0–100 %) into the buses `bus:/All`, `bus:/All/Music`, `bus:/All/Sfx`; at
  startup and on every change. Ambiance has no slider of its own and follows All. Renaming a group in FMOD Studio
  means changing its path here.
