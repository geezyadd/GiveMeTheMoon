# ConstellationModule — constellations highlighted on the sky

A constellation drawn over the painted stars of the skybox: a glow on each star and thin lines of its figure. A test
for now: only Orion, always on, only in `LobbyScene`. Read the [root AGENTS.md](../../../../AGENTS.md) first (game,
rules, module map). Change this module's description here, not in the root file.

## Assembly
- `Features.ConstellationModule` (layer 1; no references): the constellation data, the sky frame and look
  configurations, the mesh builder, the view. No Zenject: the view runs in edit mode too, where nothing is injected.
  No tests yet.

## Key places
| What | Where |
|---|---|
| Constellation as data | `ConstellationDefinition` (`GameResources/Constellations/Orion.asset`): display name, stars (name, RA and Dec in degrees, J2000) and the lines of the usual figure as pairs of star indices. A new constellation is a new asset, no code. Orion: Betelgeuse, Bellatrix, Meissa, Mintaka, Alnilam, Alnitak, Saiph, Rigel; lines Meissa-Betelgeuse, Meissa-Bellatrix, Betelgeuse-Alnitak, Bellatrix-Mintaka, the belt, Alnitak-Saiph, Mintaka-Rigel. Positions from AstroTracker's `ObjectCatalog/stars.txt`. |
| Where the painted sky lies | `CelestialSkyFrameConfiguration` (`GameResources/Configurations/CelestialSkyFrameConfiguration_LobbySky.asset`, for `StarSkybox04.mat`, `_Rotation` 0): a catalogue position (RA a, Dec d) is written in Unity's axis order (x = cos d cos a, y = sin d, z = cos d sin a: the swap that turns the right-handed celestial frame into Unity's left-handed one), then turned by `Euler(0, -90, 0)`. So RA 0 / Dec 0 is world +Z, RA 90 is -X, the north pole is +Y: a camera with yaw = -RA and pitch = -Dec looks at (RA, Dec); the celestial equator is the lobby's horizon. Found on 2026-11-27 by plate solving 1024 px, 60 deg renders of the skybox with AstroTracker's solver (8 frames, 38-51 matched stars each, fit RMS 26" = 0.11 px); Betelgeuse, Rigel, Bellatrix and Sirius land within 0.1 px of the painted stars. Another skybox, or a skybox `_Rotation`, needs its own frame. |
| Look | `ConstellationHighlightConfiguration_Default`: star glow radius (degrees), star colour (HDR, above 1 so the scene's Bloom, threshold 1, makes it shine), line colour (HDR below 1: calm, no bloom), the gap a line keeps from its stars. |
| Drawing | `ConstellationHighlightView` (`[ExecuteAlways]`, on the `ConstellationHighlight` object in `LobbyScene`, serialized refs to the three assets, its `MeshFilter` and `MeshRenderer`) builds the mesh with `ConstellationMeshBuilder` on enable and sets the star / line colours per submesh (property blocks). Every vertex is a world direction: a star is a quad of four directions around it, a line two directions. `ConstellationAtInfinity.shader` (`GameResources/Shaders`, material `GameResources/Materials/ConstellationHighlight.mat`) projects a direction with the view-projection matrix and w = 0, so the camera's position is ignored like the skybox's, and puts the depth just inside the far plane: the figure never slides against the stars when the player walks or flies, any world geometry hides it, it is unlit, unfogged, added onto the sky (transparent queue, `Blend One One`). The mesh has huge bounds so it is never frustum-culled. Local only: nothing is networked. |

## Known limits
- Only `LobbyScene` has the highlight; `GameScene` uses the same skybox and frame but has no highlight object yet.
- Lines are one pixel wide (`MeshTopology.Lines`), not anti-aliased.
- The glows are drawn over the painted stars, not from them: a star's own brightness and colour are not used.
- Checked in editor renders (batchmode camera renders of `LobbyScene`), not in Play mode.
