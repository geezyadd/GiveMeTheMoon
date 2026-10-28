# Two-player test

Read the [root AGENTS.md](../../../../AGENTS.md) first. The single-process loop tests are in [Tests/PlayMode/LoopSmoke](../PlayMode/LoopSmoke/AGENTS.md).

`CoopTwoProcessTest` (`Assets/Tests/Coop/`) plays the loop with a **real second client process**: the PlayMode test in
the editor is the host, a Windows development build of the game is the client. One command from the workspace root:
`Invoke-AgentEditor.ps1 -AgentDir . -Command run-tests -Mode PlayMode -Filter CoopTwoProcess`
(or Test Runner → PlayMode → `CoopTwoProcessTest`). It takes about 70 s, plus about 30-80 s when the client is rebuilt.

- **The client build.** Before Play mode the test's `PrebuildSetup` (`CoopClientBuild`, editor) builds Addressables
  content and a Win64 development player of the Build Settings scenes into `MirrorCoopBase/Builds/CoopClient/`
  (git-ignored). It skips the build when the content of `Assets`, `Packages` and `ProjectSettings` is the same as
  after the last build (a file hash list in `Builds/CoopClient/build.stamp`; the build's own outputs, the Test
  Runner's scene and render textures are left out). By hand: **Tools → Coop Test → Build Client**. The build re-saves
  a few settings assets with LF line endings (no content change); do not commit them.
- **The client bot** (`Assets/Tests/Coop/Client/`, only under `UNITY_EDITOR || DEVELOPMENT_BUILD`, not in a release
  build) starts when the player is launched with `-coopTestClient -connect <ip> -port <port> -scenario
  leave-and-rejoin -coopDir <folder> -logFile <file>`. It joins through the menu's address field and Join (Telepathy,
  a free port the test picks, not Steam), boards at the stern (`ShipDeckBoarding.PutAboveStern`), rides the loops the host flies, takes the helm after the first landing (put
  beside the seat, the seat's use command, then D held through the scripted move stick, `seated-at-helm`), steers the
  next cruise for 0.5 s and leaves in flight from the helm, is turned away in
  flight, rejoins at the next station on the deck, dies (the dev kill command, like the debug window's "Kill me"), waits to be bought back, on the host's
  `fire-pistol` command fires the pistol the host put in its hand at the dummy before the ship (3 clicks a cooldown
  apart, aimed through the look input, `Tests.Shared.HandWeaponAiming`; reports `pistol-fired`; the host checks the
  client's trigger arrived, it replayed the shot with a tracer and the server accepted a hit), on the
  host's `push-throttle` command launches the last loop by pushing the throttle handle (`Tests.Shared.ShipThrottleHandling`:
  behind the handle, the real aim, E held, the mouse forward until takeoff at Full, release; reports `throttle-pushed`),
  rides that loop and quits.
- **No Steam identity.** Both processes run on one Steam account, so `CoopSessionActions.PrepareDirect` sets
  `ConnectionAuthenticator.UsesSteamIdentity = false` (editor / development builds only): they authenticate as
  `CoopHost` / `CoopClient` with no Steam id (a `client:` player key), as without Steam, instead of the persona name.
  The test passes with the Steam client running; Steam itself still starts. Normal runs keep the Steam name.
- **Exchange.** The client appends a JSON line per step to `<coopDir>/client-reports.jsonl` (phase, `LoopIndex`,
  installed sockets, ship stats, crew balance, flight field number / seed / placement count, the nameplate it sees
  over the host, its own position on the deck, life state, spectating, the crew's sonar marks, reject reason, its
  logged errors); the host
  answers with commands in `<coopDir>/host-commands.txt` (`rejoin`, `finish`). The host captures the same fields
  (`CoopObserver`) and compares them at every step (`CoopObservationComparison`), and checks the client's server
  position on the deck, the `InFlight` refusal text in the client's menu, the buy-back at the kiosk through the shop
  window and that neither side logged an error. The host also installs the radar and, in the first cruise, takes the
  sonar console, pings and marks the station (`LoopSmokeSonarCheck.MarkStationInCruiseCoroutine`); the client waits
  for the mark in its own synced `SonarConsole` model and reports `sonar-mark-seen` with it. At the helm (`CoopHelm`)
  the host checks the seat held by the client's key on the server, the client's rider seated, its own helm station
  with a remote pilot and the client's body at the sit point; in the next cruise the client still steers on its peer
  and the ship turned right; after the client left in flight the helm and its station are free (the host faces the
  ship to the destination again), and the rejoined client is not seated and the helm is free. For the last loop the host
  sees the throttle held by the client's key on the server, the takeoff, the throttle at Full where the client left it
  (the client reports its own synced value too, `ThrottlePosition`), no holder after the release, and the ship flying at
  least 5 m/s in that cruise (heading away from the station so the short test cruise does not land meanwhile). Then on
  the host's `pull-throttle` the client pulls the held handle back to Stop and waits until it sees the ship at rest itself
  (`throttle-pulled`); the host sees the throttle at Stop, the ship stopped within 6 s and still for 2 s; on
  `push-throttle-in-cruise` the client pushes it to Full (`throttle-pushed-in-cruise`), the ship flies again and the host
  faces it to the station.
  At the first station the host drops a free item (the one nearest to the deck's middle it can aim at) on the deck by hand
  (`LoopSmokeDeckItemCheck.DropOnDeckByHandCoroutine`); both sides report every loose box item over the deck with its
  gap above the deck surface in ship space (`DeckItemIds` / `DeckItemGaps`, `Tests.Shared.ShipDeckItemRest`), and in
  every cruise the host checks the same items on both peers, the client's gaps within 5 cm of its own, and its dropped
  item lying on the deck (within 5 cm) and seen by the client. At a station the client sees loose items through their
  network bodies a moment behind the server, so they are not compared there.
- The test always kills the client process (also on failure or timeout). A passed run deletes its folder
  `Builds/CoopRuns/<time>/`; a failed one keeps the client's log and the exchange files there, and the failure message
  ends with the tail of the client's log.
- Shared code, one copy each: `Tests.Shared` (`SessionQueries`: menu / game-at-station / back-in-menu checks, the
  network manager, `Resolve<T>` from the project or a scene context, `IsOnDeck` (ship space, over any deck walk box
  within 0.5 m and not 0.5 m below its surface; the world bounds of the first deck box missed the console corner at
  some landing headings), `ContainsXZ`; `ShipDeckBoarding`) is used by the
  loop tests, the coop host test and the client build (no NUnit there); the host side reuses `LoopSmokeSession`,
  `LoopSmokeFlight` and `LoopSmokeShipRunConfigOverride`.
- [`Docs/COOP-CHECKLIST.md`](../../../../Docs/COOP-CHECKLIST.md) is the manual two-player pass; its "Auto" column says what this test already covers.

## Economy and weapons tests (`CoopEconomyTest`, `CoopWeaponsTest`)
Two more host tests in `Tests/Coop/Host/`, each with its own client process (scenarios `economy` / `weapons`, one
command-driven client bot: `CoopWeaponsEconomyClientScenarios` takes the host's commands in order through
`CoopTestExchange.ReadCommandsFrom`, does each through the real input path and reports what its own peer sees;
`CoopWeaponsEconomyObserver` adds loose module items, condition, hull, popped balloons, broken rocks, hand and hold
progress to the observation, compared by `CoopWeaponsEconomyComparison`). Shared host steps: `CoopEconomyHostHarness`.
Run one test per run (`-Filter CoopEconomyTest`, then `-Filter CoopWeaponsTest`): two coop tests in one run share the
ProjectContext and the second cannot bind its config override. Each takes about 75 s plus the client build.
- **Economy** (normal run, 300 at the start): at the kiosk (aim + E, the window's buttons clicked through the event
  system, the list wheeled to the row) the client is refused a rocket engine (button off, a purchase sent anyway is
  refused, no item), buys a propeller, tops the wallet up with the debug window's "+1000", buys a rocket engine, the
  light machine gun, the wrench and the pistol, is refused the heavy gun; installs the gun (turret socket by its use
  command, TurretModule's known limit), the engine and the propeller by aim (flight speed changes on both peers); the
  host breaks the engine and opens a breach (debug), the client repairs with its bought wrench and the rack's hammer by
  holding E (the host sees the client's `HoldProgress` run on the server); both ride loop 1; the next station drops no
  module. GameScene places 10 loose modules at the start station (not a station drop): counted the same on both peers.
- **Weapons**: the pistol (put in the client's hand by the host) fires at the dummy, a click within the cooldown fires
  nothing, the host sees the pistol at the client's side, its trigger and a tracer; each turret tier fires at the dummy
  from the seat through the look input and the fire button for 2 s after its first hit: the host sees trigger and
  tracers, the accepted hits keep to the tier's rate (about 12 / 40 / 8 in 2 s), the minigun lands the most, hit reports
  never exceed the network ticks (`CoopWeaponCommandCounter`; trigger / aim updates stay at about 20/s for every tier,
  so the fire is replayed, not sent per bullet). In the cruise (ship stopped by the debug throttle) the client breaks a
  small rock and pops a balloon with the heavy gun, sees a balloon the ship flies into, and breaks a rock and pops a
  balloon with the pistol: each once, by the client's key and weapon, paid once, gone on both peers. A pistol target the
  client cannot see or misses three times is reported back (`target-blocked`) and the host picks the next one.
- Client screenshots (`client-*.png`) are copied to `Temp/CoopClient/` after every run.
