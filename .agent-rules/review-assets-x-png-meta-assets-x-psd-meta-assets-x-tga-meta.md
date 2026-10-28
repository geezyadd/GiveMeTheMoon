<!-- globs: Assets/**/*.png.meta,Assets/**/*.psd.meta,Assets/**/*.tga.meta; rules: 1; dev-review-rules@local, generated - do not edit -->
# Team review rules for `Assets/**/*.png.meta,Assets/**/*.psd.meta,Assets/**/*.tga.meta`
Code you write in matching files is reviewed against these rules; follow them. Findings cite the rule ID.

### ASSET-004: Textures use the team import preset with the smallest suitable max size
- **Priority:** Performance
- **Rule:** New or changed textures use the team import preset; `maxTextureSize` matches on-screen use (UI icons 128–256).
- **Why:** Oversized textures inflate build size and memory.
- **Bad → Good:** an icon at `maxTextureSize: 2048` → preset applied, 256.
- **Fix:** Apply the preset and pick the size.
