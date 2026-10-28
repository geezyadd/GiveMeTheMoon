# PlayMode loop tests

`LoopSmokeTest` and its checks, the ride test and the soak test. Read the [root AGENTS.md](../../../../../AGENTS.md) first. The two-player test is in [Tests/Coop](../../Coop/AGENTS.md).

## Loop autotest
The PlayMode test `LoopSmokeTest` (`Assets/Tests/PlayMode/LoopSmoke/`) plays the M1 loop on a host by itself:
menu → Host → lobby → Start → `GameScene`, installs 2 engines, takes the radar off, drops, picks up and reinstalls it by hand through the real third-person
aim (`LoopSmokeHandCheck`), then plays the mechanic by hand (`LoopSmokeRepairCheck`: the debug break entry (`IShipDamageDebug`) breaks the left engine, the
synced speed drops and the condition shows it; the wrench from the deck rack is grabbed through the real aim, a hold on
the engine released early repairs nothing, a full hold repairs it and the speed is back; after a small hull hit the
hammer patches a breach and the hull is full again; in the first cruise a random module breaks and a breach opens, and
the landing repairs both), takes off and lands twice (in the first cruise `LoopSmokeHullCheck` hits the hull through
the debug window as a player does: it clicks the corner button (the panel opens, the cursor is freed), types the damage
into "Hit the hull" and clicks it, clicks the corner button again (closed, cursor released), and checks the armor-reduced drop in the synced model, after the landing that the hull is full again), and checks the phases,
`LoopIndex`, engines after landing, the ship on the new pad, one shop kiosk per pad, the player on deck, no wreckage.
At the first station it reaches it tops the wallet up through the debug window as a player does (`LoopSmokeDebugMoneyGrant`:
the corner button, Shop → "+1000", the synced balance grows by `WalletConfiguration.DebugGrantAmount`, the corner button
again; a normal run starts with only 300) and buys back a dead crewmate with a second player, a bot on the host (`LoopSmokeReviveCheck`:
the price is taken once for a double click, no buy-back without money, the revived player stands at a deck spawn
point; then the host dies and the bot buys them back). The bot joins through the server's real receive path
(`LoopSmokeBot`, `LoopSmokeTransportInput`). `LoopSmokeRejoinCheck`: a new player joins at that station on a deck spawn
point, dies, leaves in flight, a join in flight is turned away with `InFlight`, and after landing it comes back dead, in
the Crew list, under its new name. After the second landing everyone dies, the crew returns to
the lobby and `LoopSmokeRunLeakCheck` checks that no object of the first run is left (and that run 2 starts with the same
station items and spawns the same pad and drops on its first landing). Then it starts a second run, flies five loops (after the first one it checks the station's crew terminal and puts the
player back on the deck)
in a row and at the fifth station tops the wallet up the same way and buys 5 items at the kiosk through the real aim and the window (`LoopSmokeShopCheck`: the mouse wheel over an item row scrolls the list until the last item is in view, delivery
point, window closes when walking away, the bought items come to rest within a timeout, a far purchase is refused), takes off a sixth time
and destroys the hull in that cruise (`LoopSmokeRunEnd.HullLossInCruise`, `LoopSmokeSession.ReturnToLobbyByHullLossCoroutine`:
hull 0 -> the crew in the lobby; `LoopSmokeHullLossCheck`: the host stands by the minigun before the takeoff and sits at it
in the cruise through the seat's use command, so the hull is lost with the host seated and riding; back in the lobby
it is not riding, not seated, not kinematic, has collisions, hover and its network body back, is alive with unblocked
controls, stands still on the lobby scene's ground and walks under the scripted move input; at the first station of
run 3 it walks on the deck under that input and stays on it), returns to the menu and checks that there are no
errors in the log. Before that check `LoopSmokeRejectCheck` joins a silent loopback "host", hands the client an
`InFlight` answer and checks the reason text in the menu. `LoopSmokeCrewInviteCheck`: one "Hire crew" terminal in the
lobby and one per pad, inactive with "Steam is not running." under the real aim (no Steam in the test; E on it asks
`LoopSmokeSteamLobbySpy`, which stands in for `ISteamLobbyService`, for no overlay), the
`CrewRoster` counts filler bots up to the slot limit (crew full) and back, and closes the join window in flight. Every
flight runs `LoopSmokeFieldCheck`: in cruise a new
flight field stands with an active local object for every local placement within the build radius of the camera (none beyond the release radius) and one Mirror object per networked placement;
after landing nothing of it is left (what a player joining at that station gets); in run 2 loop 3 a forced world shift
in cruise keeps the field in place against the ship (`LoopSmokeFieldRunCheck`, which also asserts at the fifth station of
run 2 that a networked placement was checked); the field pool never grows past what the fields needed. Before
the first launch `LoopSmokeSonarCheck` stands the host behind the reinstalled radar (its console is inactive at the
station); first in the cruise of run 1 loop 1 it takes the console through its use command (what E sends): the
`SonarWindow` opens, the synced model names a navigator and walking / aim are blocked; the window's Ping button
lights the station's echo at the radar's 1 000 m range, a click on that echo puts a station mark into the synced
`Marks` and a marker on the HUD, the Leave button closes the window, frees the console and the controls, and the mark
runs out (`LoopSmokeSonarConfigOverride` binds a copy of `SonarConfiguration` whose station marks last 2 s). In the
cruise of run 1 loop 1 `LoopSmokeRockBreakCheck` breaks the rock nearest to the ship through `IFieldRockBreakDebug`:
it is in the live field's `BrokenRocks`, `RockDestroyed` fired once (a second break is `AlreadyBroken`), the rock is
hidden on the host, and after landing the broken list is empty. In the same cruise `LoopSmokeRockImpactCheck` flies
into rocks through `IShipRockImpactDebug` (the debug window's rock aim entry; the hit is the real server check): a small rock breaks
once (broken list, `RockDestroyed` once), the synced hull drops and a small impact arrives; a large rock gives a large
impact, stays whole, the ship never gets within the contact radius and moves back out, and the player still rides on the
deck (in ship space); then the ship goes back and faces the destination. Every
takeoff logs its longest frame (`[LoopSmoke] loop N takeoff`). The test shortens flight timings with its own copy of
`ShipRunConfig` and adds the debug networked beacon to its own copy of `FieldStructureCatalog` and, as a giant class of its own, to its own copies of the field configuration's difficulty profiles (weight 2 against about 1 of the game's classes, so nearly every short test field has a networked
placement); assets are not changed. After the shop check the crew returns to the lobby and `LoopSmokeStagePresetCheck` plays a third run with the
`Late` stage preset: the host clicks Late in the lobby picker, the run starts on loop 9 with the preset's modules,
balance and "Preset: Late" label, the first flight's field comes from loop 9's table entry, the first station drops
nothing, and back in the lobby the preset is gone. In run 2 the minigun goes into `turret-front` at the station after loop 1 and the
host stands by it; in the cruise of loop 2 `LoopSmokeTurretCheck` spawns the debug dummy, sits the host at the turret
through the seat's use command, aims it with the station's test hook (`ShipTurretStation.DebugAimAt` /
`DebugHoldTrigger`, editor / development only) until the server has accepted 5 hits on the dummy, then
`LoopSmokeRockShootCheck` brings its own target (the ship is put beside the nearest small breakable rocks in turn
until one is in reach and clear sight of the turret, and put back afterwards) and fires until it breaks (in the broken list,
hidden on the host, `RockDestroyed` once with cause `Weapon`, the host's key and `minigun`), and stands up again.
`LoopSmokeOreCheck` (run 2 flight 1; replaced the balloon check `LoopSmokeTargetCheck` on 2026-10-28): at that station
the host buys the harpoon at the kiosk through the real aim and the window and picks it up by hand; in that cruise it
breaks the rocks nearest to the ship (the debug break, as a bullet does) until one drops ore, the piece is in the live
field and shown on the host, the ship is put 30 m beside it, the host aims the camera through the look input and clicks
`Fire` through the input service until its rope hooks the piece (the server names the host, the host draws the rope),
the piece reaches the ship once and the synced balance grows by exactly the delivered prices; then a second rock's ore
nobody picks up is gone from the field and the screen at its end of life (`LoopSmokeOreConfigOverride` binds a copy of
`FieldOreCatalog` whose kinds live 8 s). The host drops the harpoon and the test removes it in that cruise (the first
landing of run 2 must spawn what run 1's did). Screenshots in `Temp/LoopSmoke`: `ore-drop`, `ore-rope`, `ore-delivered`
(the screen) and `view-ore-*` (a 1920 x 1080 photo from a test camera beside the player's view, no HUD).
`LoopSmokeTurretTierCheck` (run 2): at the station before flight 3 (ahead of the deck item check) the host buys the
light machine gun at the kiosk through the real aim and the window (`LoopSmokeKiosk`: buy one entry, walk away, pick
the delivered item up by hand; money from the debug window when short), takes the minigun off through the socket's use
command (from behind the gun the seat volume covers the socket plate under the crosshair), drops it on the pad (then
removed), installs the light gun from the hand and boards; before flight 5 it stands by the turret, and in that cruise
(after the pistol) sits at it and fires at the debug dummy until 3 hits are accepted (screenshot
`turret-tier1-light-machine-gun.png`); after landing 5 the heavy gun goes in (`ServerTryInstall`, test setup) and fires
the same way first in cruise 6, before the hull loss check sits there (`turret-tier3-heavy-gun.png`); the minigun shot
of `LoopSmokeTurretCheck` takes `turret-tier2-minigun.png`. `LoopSmokePistolCheck` (run 2 flight 5): the host buys the
pistol and picks it up (the HUD ring is ready) by the turret (a move in cruise leaves the player behind the flying
ship); in cruise it aims the camera through the look input and clicks `Fire` through the input service
(`Tests.Shared.HandWeaponAiming`): one hit on the dummy (a miss of the swaying dummy is retried after the cooldown, at
most 3), a second click within 2 s fires nothing, the ring fills up again (`pistol-in-hand-cooldown.png`), one shot
breaks a small rock (Weapon break, the host, `pistol`); then it gives the view turn back, drops the pistol (hand and
ring empty); after the landing the dropped pistol is removed.
`LoopSmokeThrottleCheck` (run 2 flight 4): before the takeoff the host stands behind the throttle handle, aims at it
through the real camera, holds E (`LocalPlayerInteraction.Interact`) and moves the mouse forward through the
interaction's look input (`MoveHeldControl`) until the ship takes off and the handle is at Full (the server gave it to the
host's key, the view was locked while held, it let go and stayed at Full on release; the steps are the shared
`Tests.Shared.ShipThrottleHandling`, also used by the coop client); in that cruise the riding host pulls the handle back
to Stop by hand (the ship stops within 6 s and stays still for 2 s) and pushes it to Full again (it flies again), both
with the player riding on the deck; after the landing the handle is
back at Stop and free. `LoopSmokeRockNearPassCheck` (run 1 cruise 1, after the rock impacts): "Show impact volume" clicked on through the debug window, the ship is put to pass the biggest lone small rock with its side halfway between the rock's contact and data spheres; the hull must come inside the data sphere, and the rock may break only if the hull came within the contact sphere; then the toggle off and the ship back. `LoopSmokeDeckItemCheck` (run 2 flight 3): before the takeoff the host grabs two free items with a box
collider nearest to the deck's middle (in practice the wrench and the hammer of the tool rack; some station drops lie
under the deck, out of reach, so an item the aim cannot take is skipped and the list is tried up to 3 times) through
the real aim, drops the first on the deck (it comes to rest as a physics body) and
carries the second aboard; in that cruise (after the forced world shift) the first lies on the deck, the second, let
go from the hand, falls onto it, and after a hard turn (`SetDebugSteer`) both still lie on it where they were; after the
landing both are physics bodies again (not kinematic), at rest on the deck and can be picked up. "Lies on the deck" is
the gap between the item box's lowest corner and the deck surface under it in ship space (`Tests.Shared.ShipDeckItemRest`),
within 5 cm. `LoopSmokeStationMarkerCheck` (run 2 cruise 5): the debug window's "Show station: off" clicked as a
player does turns the own station marker on, the level camera turned toward the station through the look input shows
it over the station with its distance, turned away on the screen's edge, and "Show station: on" takes it off again. The test timeout is 300 s (was 240 s; a run takes about 230-250 s since the ore check of 2026-10-28). The shared steps live in helpers both loop tests use:
`LoopSmokeSession` (menu, Host, lobby, Start, all dead -> lobby, Leave; waits record the run phases),
`LoopSmokeFlight` (engines, boarding, the launch (the throttle lever at Full through the debug window's "Throttle = N",
`IShipThrottleDebug`; a check can launch the next flight its own way with `LaunchNextFlightBy`), the phases of a loop with the field checks and the checks after
landing, messages start with "Loop N:"; at the cruise start its pilot faces the ship to the destination once through
`ShipBase.FlightControlDebug`, because the autopilot keeps the takeoff heading), `LoopSmokeShipRunConfigOverride` and `LoopSmokeFieldCatalogOverride` (the
config copies) and `LoopSmokeSpawnCounts`.

**Adding a check to the loop test.** `LoopSmokeTest.cs` only plays the runs (its `_runs` list: flights per run, the
test's rocket engines or a preset's modules, how the run ends) and does not change per feature. The checks register
themselves: `LoopSmokeCheckRegistry` reads every method with `[LoopSmokeStep(phase, run, flight, order)]` in the
assembly and runs it at its slot. A new check is a new class in `Assets/Tests/PlayMode/LoopSmoke/` with one attribute
line per step, nothing else:
- Phases (`LoopSmokePhase`): `InLobby` (before the run's Start), `BeforeTakeoff` (at the station before the flight's
  launch, after the engines and the boarding), `InCruise`, `AfterLanding` (after the flight's landing checks),
  `AfterRun` (back in the lobby) take a run from 1; the flight phases also take the flight of the run from 1 (the n-th
  flight: `LoopIndex` for runs 1 and 2, loop 10 is flight 1 of the Late run 3); `InMenu` (after Leave) takes neither.
- `order`: the steps of one slot run one after the other, lower first; two steps with the same order in a slot fail
  the test at its start, so pick a free number (the existing steps use multiples of 10; the first cruise of run 1:
  sonar 10, hull 20, rock break 30, rock impacts 40, repair 50; the turret cruise of run 2: dummy 10, rock shot 20,
  stand up 40; the first cruise of run 2: crew invite 10, ore and harpoon 20).
- A step takes no arguments and returns `IEnumerator` or `void`. Its class has a public constructor taking
  `LoopSmokeContext` (session, flight helper, field check, Steam lobby spy) or an empty one; it is made at its first
  step and kept for the whole test, so its steps share state (resolve scene services at a step if they cross runs).
- At the end the test fails if a registered step never ran (a slot the runs never reach).
Why attributes and not one list file: a list is the same shared file every feature edits; a check that registers in
its own file merges without conflicts. To change the scenario itself (one more run or flight) edit `_runs`.

## Ride test
The ride test `ShipRideTest` (`Assets/Tests/PlayMode/ShipRide/`, about 75 s, `-Filter ShipRide`) flies six loops on a host on the
same fast timings, one per spot of the deck found from its geometry (`ShipRideDeckSpots`): the nose, the stern, both
side edges, the centre and the stern in a jump at the lever (the lever is pulled at the top of a real jump through
`CharacterInputBuffer`). After takeoff and in cruise the player must ride, be alive and stay on the deck; in cruise it
jumps (from the centre with a sideways hop) and must land back on the deck still riding.

## Soak test
The soak test `LoopSoakTest` (`Assets/Tests/PlayMode/LoopSoak/`, `[Category("Soak")]`, about 3 minutes) flies 15
loops in a row on the same fast timings and catches what only shows up over time. It does not repeat the station
checks of `LoopSmokeTest`. Every loop: the smoke test's flight, field and landing checks; at most 2 landing pads, no
wreckage, and runtime network objects other than items and pads exactly as after loop 1 (`LoopSoakAccumulationCheck`);
the landed ship closer to the origin than `OriginRecenterDistance` and, after a world shift, the player on the deck
(`LoopSoakWorldShiftCheck`). At least one world shift must happen (the default 800 m gives one every second loop) and
at least one networked field placement. It logs a table per loop (`LoopSoakReport`: time, managed and allocated
memory, spawned objects, items and their growth, pads, shifts, the ship's distance from the origin, field placements,
field objects in cruise and pooled field objects) and fails when memory at the last loop is more than 1.5 times that
of loop 2. Station drops are never removed, so items grow by about 10 per loop: reported, not failed.

## Running
Run from the workspace root:
`Invoke-AgentEditor.ps1 -AgentDir . -Command run-tests -Mode PlayMode -Filter LoopSmoke`
(or Test Runner → PlayMode → `LoopSmokeTest`); the soak test with `-Filter LoopSoak` (or `LoopSoakTest`), its table
is in the editor log under `[LoopSoak]`.

The test lives in Assembly-CSharp
under `#if UNITY_INCLUDE_TESTS`, so `playModeTestRunnerEnabled` is on in `ProjectSettings`. New allowed log errors go
only into the `LoopSmokeErrorLog` allow-list, with a reason.

`LoopSmokeTest` stays in `Assembly-CSharp`: every assembly is visible from there. Whatever the test touches in a
  module must be `public` (not `InternalsVisibleTo`).
