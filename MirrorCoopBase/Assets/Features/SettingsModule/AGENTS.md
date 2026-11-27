# SettingsModule

The player's settings of this machine (local, not networked): video, controls, gameplay. Being built stage by stage
(2026-11-27); this file describes what exists now.

## Data (`Features.SettingsModule.Data`, layer 1, no module references)
- A setting is one `SettingDefinition` asset (key, label, default) of one kind:
  - `ToggleSettingDefinition`: on / off;
  - `SliderSettingDefinition`: minimum, maximum, step, value text format; the value is in the units the player sees
    (50 for 50 %), the module that applies it converts; `Normalize` clamps and snaps to the step;
  - `ChoiceSettingDefinition`: a fixed list of `SettingChoiceOption` (id + label), the value is the option id;
  - `ResolutionSettingDefinition`: a choice whose options are the display's resolutions, built at runtime; the id is
    `<width>x<height>`, empty (the default) keeps the resolution the game started with.
- `SettingsCatalog` (`SettingsCatalog_Default`, Configurations group, bound in `ConfigurationInstaller`) lists every
  setting; the assets are in `GameResources/Configurations/Definitions/`.
- `SettingsModel` (plain class, bound in `DataInstaller`) holds the current value of every setting, starting from the
  defaults: `GetToggle / GetSlider / GetChoice`, `SetToggle / SetSlider / SetChoice` (a slider value is normalized, an
  unknown choice option throws), `ResetToDefault`, `OnValueChanged(key)` only on a real change.
- `SettingKeys` holds the keys the modules that apply settings read by.
- **Saves are not here.** Another module will plug in: it writes loaded values with the setters (checking
  `AcceptsOption` for a choice) and reads the values to save for each of `SettingsModel.Definitions`. Modules that
  apply a setting react to `OnValueChanged`, so loading after startup is enough.

## Settings now
| Key | Kind | Default |
|---|---|---|
| `video.window_mode` | choice: `fullscreen_window` / `exclusive_fullscreen` / `windowed` | `fullscreen_window` |
| `video.resolution` | resolution | empty (as started) |
| `video.vsync` | toggle | on |
| `video.fps_limit` | choice: `30` / `60` / `120` / `144` / `unlimited` | `unlimited` |
| `video.shadow_quality` | choice: `off` / `low` / `medium` / `high` | `medium` |
| `video.anti_aliasing` | choice: `off` / `2x` / `4x` / `8x` | `4x` |
| `video.render_scale` | slider 50–100 %, step 5 | 100 |
| `video.field_of_view` | slider 50–100, step 1 | 60 |
| `controls.mouse_sensitivity` | slider 0.05–0.5, step 0.01 | 0.18 |
| `controls.invert_y` | toggle | off |
| `gameplay.screen_shake` | slider 0–100 %, step 5 | 50 (today's strength) |
| `gameplay.nameplates` | toggle | on |

Nothing applies these values yet: the game behaves as before.

## New setting
A definition asset in `Definitions/`, an entry in `SettingsCatalog_Default`, a key in `SettingKeys` if a module reads
it.

## Tests (`Features.SettingsModule.Editor.Tests`)
- `SettingsModelTests`: defaults, change events only on a real change, slider clamp and step, choice options, reset of
  one setting, wrong kind / unknown key / duplicate key.
- `SettingsCatalogGuardTests`: the catalog asset at its address has no empty entries, unique keys, labels, a
  definition for every `SettingKeys` key, slider defaults in range and on a step, choice defaults among the options.
