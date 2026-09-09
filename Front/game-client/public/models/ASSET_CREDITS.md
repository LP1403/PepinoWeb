# Pepino hands

`pepino-hand-rigged.glb` uses the same source and license described below.
It preserves the 18-bone skeleton at the same cupped pose, with a 1024px texture.
Reproduce with `node scripts/export-rigged-hand.mjs` from the web client while
Vite runs at port 5174. The script uses the repository FBX and texture, and Edge.
Skeletons are cloned independently for each rendered hand.

`pepino-hand.glb` is derived from the First Person Hands asset by Robert Ramsay
already imported into this project's Unity assets. Source:
`UnityProject/PepinoUnity3D/Assets/FirstPersonHands/MaleHands/MaleHand.FBX`
and `firstPersonHand_textures/HandArmDiff.png`.

This is a game-specific, baked cupped pose (625/30 seconds of Take 001), with
animation and skeleton data removed and the albedo reduced to 1024px. It is
approximately 1.6 MB. This asset is NOT original Pepino work or CC0. Its original
Asset Store license continues to apply. Do not distribute it as an asset pack.

The table, felt, wood texture, lamps, mate, glasses and snacks are original
procedural geometry/art in `src/game3d/tableEnvironment.ts`.
