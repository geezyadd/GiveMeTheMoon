# TooltipModule — tooltips

Interaction tooltips. Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description here, not in the root file.

## Assembly
`Features.TooltipModule` (layer 6; references: Shop, Ship, ShipDamage for the broken module tooltip, Grab, Mvp).

## Key places
| What | Where |
|---|---|
| Tooltips | `Assets/Features/TooltipModule/`: `InteractionTooltip` on the object, `TooltipContentService`, `Tooltip_*.asset` in `GameResources/Configurations/`; live texts through `ITooltipStateProvider`: `ShipModuleRepairTooltipProvider` (a broken module), `ShipThrottleTooltipProvider` (the throttle's mode and percent, and at a station how to take off or why not; texts in `ShipThrottleConfiguration`, title and hint in `Tooltip_Throttle`, the former `Tooltip_LaunchLever`) |
