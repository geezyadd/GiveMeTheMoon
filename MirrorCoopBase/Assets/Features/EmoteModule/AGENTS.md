# EmoteModule — the emote wheel and the player's emotes

Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description
here, not in the root file.

## The idea
A player holds **G**: a wheel opens in the middle of the screen with one sector per emote of the catalog (360° split
equally, the first at the top, then clockwise). The mouse moves the wheel's pointer (the camera does not turn while the
wheel is open) and highlights the sector it is in; releasing **G** plays the highlighted emote, releasing it in the dead
zone around the centre cancels. The emote plays through the player's animator (`PlayerAnimationDriver`), on every peer,
the same way as the slap: an emote count and the emote's index ride the body state packets, and every peer plays the
emote on its own copy of the body. An emote stops at once when the body starts to move.

## Status
Built on `demo/emote-wheel` (2026-11-27). The four emotes (Wave, Dance, Point, Laugh) are placeholders: they reuse
existing clips (`PlayerSlap`, the KevinIglesias idles and talk) and have no icons, so the wheel shows their names. Not
checked in Play mode or with two players yet.

## Assemblies
- `Features.EmoteModule.Contracts` (layer 1; no references): `IEmotePlays` (`IsAvailable`, `EmoteCount`,
  `EmoteIndex`): where a player's emote plays come from on this peer. PlayerReplicaModule reads the owner's through it
  for the body state packet.
- `Features.EmoteModule.Data` (layer 1; no references): the catalog, so the animator builder
  (`Features.PlayerAnimationModule.Editor`) can make the Emote layer from it without referencing this module.
- `Features.EmoteModule` (layer 4; references `EmoteModule.Contracts`, `EmoteModule.Data`, `PlayerAnimationModule`,
  `PlayerReplicaModule`, `CharacterMovableModule`, `CameraModule`, `GameFlowStateMachineModule`, `MvpModule`, Input,
  Mirror, Zenject, `UnityEngine.UI`). `EmoteModuleInstaller` (installed in `GlobalSceneInstaller`) binds
  `EmoteWheelSystem`; `EmoteWheelModel` is bound in `DataInstaller`, the window in `WindowsModuleInstaller`.
- `Features.EmoteModule.Editor`: `EmoteAssetsBuilder`. Tests: `Features.EmoteModule.Editor.Tests`.

## Parts
- `Data/EmoteCatalog` (Addressables `EmoteCatalog_Default`, bound in `ConfigurationInstaller`;
  `GameResources/Configurations/EmoteCatalog_Default.asset`): the list of `EmoteEntry`: `Display Name` (shown when
  there is no icon), `Icon` (Sprite, may be empty), `Trigger Name` (the animator trigger and the Emote layer's state of
  the same name; must be unique and not clash with `Forward`, `Fly`, `Slap`, `EmoteStop`) and `Clip` (what the animator
  builder puts on that state). An emote's **index** in the list is what goes over the network. Placeholders: Wave
  `EmoteWave` (`PlayerSlap`), Dance `EmoteDance` (`HumanM@Idle01-Idle02`), Point `EmotePoint` (`HumanM@Idle02`), Laugh
  `EmoteLaugh` (`HumanM@Talk01`).
- `EmoteWheelConfiguration` (Addressables `EmoteWheelConfiguration_Default`, bound in `ConfigurationInstaller`):
  `Pointer Radius` 120 and `Dead Zone Radius` 35, in the look input's units (mouse pixels).
- `EmoteWheelRule` (pure): `PickSector` (the sector a pointer offset is in, the first one centred at the top, then
  clockwise; `NoSector` inside the dead zone), `SectorCenterDegrees` / `SectorDirection` (for the layout) and
  `MovePointer` (the pointer moved by a mouse delta, kept within the radius).
- `EmoteWheelModel` (plain, local): `IsOpen`, `HighlightedIndex`, `OnEmoteChosen(index)`. Only the system writes it.
- `EmoteWheelSystem`: on the `Emote` input (G, `MovementMap`) pressed it opens the wheel when the player is in a session
  (`SessionGameFlowState`), the catalog is not empty, nothing blocks the controls (`PlayerControlBlockModel`: the sonar)
  and no window frees the cursor (`CursorModel`); it locks the camera look (`CameraLookLockModel`; the cursor stays
  locked and hidden) and opens `EmoteWheelWindow`. Every look delta moves the pointer and sets the highlighted sector.
  Released: the window closes, the look lock goes, and a highlighted sector raises `OnEmoteChosen`. Leaving the
  session closes the wheel. The shop disables the movement map, so G does nothing there.
- `UI/EmoteWheelWindow` (prefab `GameResources/Prefabs/EmoteWheelWindow`, Addressables `Windows`), `EmoteWheelView` /
  `EmoteWheelPresenter`: the view copies its `SectorTemplate` once per emote; `EmoteWheelSector` places its
  `EmoteSectorGraphic` (a ring piece drawn as a mesh, inner 70 / outer 220 px, a 1.5° gap) and its icon or name 150 px
  from the centre, and colours the highlighted one.
- `Player/PlayerEmotePlays` (`IEmotePlays`, on `DummyPlayer`'s root next to `BodyStateChannel`; refs: the movement, the
  `BodyCopyDriver`, the `EmotePlayer`): on the owner's peer, a chosen emote grows the count and sets the index only when
  `EmotePlayer.CanStart` (the animator runs and the body stands still: no walk input above the idle cutoff, not in the
  air), so a refused pick never reaches the copies; on another peer, `BodyCopyDriver.ShownEmoteCount` /
  `ShownEmoteIndex` (the packet the copy shows now) while the copy is simulated.
- `Player/EmotePlayer` (on `DummyPlayer`'s root; refs: the `PlayerAnimationDriver`, the movement, the plays): every
  frame on every peer that simulates the body, `EmotePlayCounter` turns the count into plays (the first count read after
  the plays were unavailable is no play, so a copy that starts or a late joiner does not emote by itself); a play
  resets `EmoteStop` and fires the emote's trigger (`PlayerAnimationDriver.Play`). While an emote plays, a walk input
  above the idle cutoff (`PlayerAnimationDriver.IsWalking`), a jump (`CharacterMovableBase.JumpCount` changed) or the
  air (`PlayerAnimationDriver.IsFlying`) stops it: the emote's trigger is reset and `EmoteStop` fires. **No stop
  message:** a copy's walk input comes from the owner's packets (`PacketMovementControls`), its jumps from the jump
  counter in them and its ground from its own sensor, so every peer sees the same movement and stops the emote itself.
- `EmotePlayCounter` (pure): the count to plays, as the slap's `AttackPressCounter`.
- Editor: `EmoteAssetsBuilder` (Tools > Emotes > Build Emote Assets) creates the catalog, the configuration and the
  window prefab once (existing ones are left as they are), puts them in their Addressables groups, adds
  `EmotePlayer` and `PlayerEmotePlays` to `DummyPlayer` and sets their references, then rebuilds the player animator.

## The animator contract
Built by `PlayerAnimatorBuilder` (PlayerAnimationModule) from the catalog; never edit the controller by hand.
- Layer `Emote` (override, no mask: the whole body, weight 1), between `Fly` and `Slap`. Whole body because an emote
  stops as soon as the player walks, jumps or is in the air, so nothing else needs the legs while one plays, and a dance
  or a laugh moves the hips and the legs too; the `Slap` layer stays above it, so a slap still shows on the upper body.
- States: `Empty` (default, a blend tree with no motions, as in the slap layer) and one state per catalog emote, named
  by its trigger.
- Parameters: one trigger per emote (`EmoteWave`, `EmoteDance`, `EmotePoint`, `EmoteLaugh`) and `EmoteStop`
  (`PlayerAnimatorParameters.EmoteStopName`).
- Transitions: Any State → emote on its trigger (no exit time, can transition to itself, so a new emote replaces the
  one that plays); emote → `Empty` at its exit time; emote → `Empty` on `EmoteStop` (no exit time). Durations are the
  editor's defaults.

### Where the real clips go
1. Put the clip anywhere in the project (our own clips: `PlayerAnimationModule/GameResources/Animations/`; a pack's
   into `Assets/Global/Packages/<Publisher>/`). It must be a humanoid clip.
2. Select `EmoteModule/GameResources/Configurations/EmoteCatalog_Default` and drop the clip into the emote's `Clip`
   (set the `Icon` too, if there is one). A new emote is a new entry with its own `Trigger Name`.
3. Run Tools > Animation > Build Player Animator (or Tools > Emotes > Build Emote Assets). The controller is rebuilt in
   place; an emote without a clip gets the idle.
A looping clip still goes back to `Empty` after one loop (the exit time); an emote that should loop until the player
moves needs its exit transition changed in `PlayerAnimatorBuilder.AddEmoteLayer`.

## Known limits
- Not checked in Play mode: the wheel's look, the pointer's feel, the placeholder clips on the ragdoll, and whether the
  `Empty` blend tree of a whole body layer leaves the locomotion alone as the slap layer's does.
- A seated player's copy is not simulated (the seat holds it), so the others do not see a seated player's emote.
- The catalog's order is the network's emote index: reordering it while players are connected mixes emotes up.

## Tests
- `Features.EmoteModule.Editor.Tests`: `EmoteWheelRuleTests` (the dead zone and its edge, no sectors, four sectors
  from the top clockwise, both sides of the top, the edge between two sectors, three sectors, one sector, every
  sector's centre for 1 to 8 sectors, the pointer kept on the radius); `EmotePlayCounterTests` (the first count, a
  grown count, several missed, a wrapped count, after the plays were unavailable).
- `Features.PlayerReplicaModule.Editor.Tests`: `BodySendScheduleTests.WhenTheOwnerEmoted_ThenThePacketGoesAtOnce`.
