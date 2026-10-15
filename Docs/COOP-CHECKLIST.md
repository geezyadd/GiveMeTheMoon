# Co-op checklist (two players)

A manual pass for a host and a second player. Run it after every large merge that touches the ship, the session, the
shop or the players. The owner fills it in; each item is checked from **both** sides.

**Setup.** Host: the editor (menu → Host). Client: the ParrelSync clone `MirrorCoopBase_clone_0` (menu → address →
Join), or a development build. Steam is not needed for direct IP.

**Result** per item: `OK`, `BUG: <what, repro, who sees it>` or `not checked: <why>`. After every item look at the
console of both sides; any `Error` / `Exception` goes into the result with its stack.

**Auto** says what the automated tests already cover, so the manual pass can focus on the rest:
- `2P` - covered by the two-process test `CoopTwoProcessTest` (editor host + development-build client, see
  `AGENTS.md` → "Two-player test"); host and client views are compared;
- `2P part` - partly covered, the column says what is left;
- `LS` - covered only on the host by `LoopSmokeTest` (an in-process bot, not a real second client);
- `-` - not automated.

## Loop

| # | Scenario | Host sees | Client sees | Result | Auto |
|---|---|---|---|---|---|
| 1 | Both in the lobby → Start → both in the game, see each other at the station, the same balance. | | | | 2P (balance, nameplates, both spawned) |
| 2 | The client picks up a module and installs it in a slot; the host sees it in the slot. And the other way round. | | | | - |
| 3 | The client takes a module off (Engine or Radar): it is in the client's hands, the host sees it there. | | | | - (LS: host only) |
| 4 | The client buys at the kiosk (aim + **E**, buy in the window): the balance drops for both, the item lies on the kiosk's delivery pad for both. | | | | - (LS: host only) |
| 5 | Both on the deck, the host pulls the lever: takeoff, flight, landing; both stay on the deck, no jerks, no falling through. Repeat with the client pulling the lever. | | | | 2P part: host pulls; client pulling and "no jerks" by eye are left |
| 6 | The client sits at the helm and steers: the host sees the ship turn and the client in the seat. | | | | - |
| 7 | Three full loops in a row: modules in place for both, both on the same pad. | | | | 2P (3 loops, sockets, both on the deck) |
| 8 | A world shift in flight (`DebugForceRecenter` or a long flight): no jerk of the ship, the rocks or the player on the client. | | | | - (LS: host only, field follows the shift) |

## Death and buy-back

| # | Scenario | Host sees | Client sees | Result | Auto |
|---|---|---|---|---|---|
| 9 | The client falls off the pad and dies: the host sees the client gone, the client spectates; the module from the client's hands drops and lies in the same place for both. | | | | 2P part: death (debug kill) and spectating; falling and the dropped item are left |
| 10 | Both die: both are back in the lobby after `AllDeadReturnDelay`. | | | | - (LS: host only) |
| 11 | Buy-back: the client is dead, the host opens the kiosk, the "Crew" section lists the client, the host buys them back: the client is alive on a deck spawn point, stops spectating, the balance drops for both. | | | | 2P |
| 12 | Buy-back the other way: the host is dead, the client buys them back at the kiosk. | | | | - (LS: an in-process bot buys the host back) |
| 13 | Both press "buy back" for the same dead player at once: the price is taken once. | | | | - (LS: a double click on one host) |

## Leaving and joining

| # | Scenario | Host sees | Client sees | Result | Auto |
|---|---|---|---|---|---|
| 14 | The client leaves in Build: the client is gone for the host, nothing breaks, the host can go on with the loop. | | | | - |
| 15 | The client leaves holding a module: where is the module for the host? (expected: it drops at the station). | | | | - |
| 16 | The client leaves in flight: the host flies on and lands normally. | | | | 2P |
| 17 | The client leaves sitting at the helm in flight: the helm is free, the host can sit down. | | | | - |
| 18 | The client tries to join in flight: a clear refusal in the menu (`InFlight` text), no errors on the host. After the landing the client joins and appears on the deck. | | | | 2P |
| 19 | The client leaves dead, joins again at a station: still dead, listed in the shop's "Crew" section, can be bought back. | | | | - (LS: an in-process bot) |
| 20 | A new client (never in this run) joins at a station in the middle of the run: appears on the deck, sees the current modules, balance and phase. | | | | 2P part: the same client rejoining; a brand-new player is left |
| 21 | The host leaves: the client returns to the menu cleanly, no hang, no errors. | | | | - |

## Players and the station

| # | Scenario | Host sees | Client sees | Result | Auto |
|---|---|---|---|---|---|
| 22 | Nicknames over the heads: each player sees the other one's name, not their own. | | | | 2P |
| 23 | Two players buy at the kiosk at the same time: both purchases are paid once each, both items are delivered. | | | | - |
| 24 | The "Hire crew" terminal (needs Steam): active at a station with free slots; "Crew is full" with a full crew; inactive in flight. | | | | - (LS: without Steam only) |
| 25 | The flight field: the same rocks in the same places for host and client, also after a world shift in flight. | | | | 2P part: the same field number, seed and placement count; the look and the shift are left |
| 26 | The ship stats line on the client shows the same values as on the host. | | | | 2P part: the stats model is compared, the UI line is left |

## Log

| # | Scenario | Host sees | Client sees | Result | Auto |
|---|---|---|---|---|---|
| 27 | After every item: any `Error` / `Exception` in the console of the host and of the client, with its stack. | | | | 2P (for its own scenario: no errors on either side) |
