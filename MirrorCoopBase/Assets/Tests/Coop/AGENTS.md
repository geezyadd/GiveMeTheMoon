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
  flight, rejoins at the next station on the deck, dies (the dev kill command, like the debug window's "Kill me"), waits to be bought back, on the
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
  least 5 m/s in that cruise.
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
