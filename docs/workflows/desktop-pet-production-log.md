# Desktop Pet Production Log

## Source

- File: `assets/source/character_original.png`
- SHA-256: `9F0B1E9F47DBDF464411A204BD69E02BAF86579C5505974D9C827F45B78FE60F`
- Original size: 512×512
- Source copy is byte-identical to the user-provided file.

## Background extraction

Two identity-preserving image-generation edits were attempted. Both were rejected because they changed the crop, face proportions, eyes, hair ornaments, line art, and desk geometry. Neither generated result was added to the repository.

The accepted master uses deterministic connected-background extraction:

- Only near-white pixels connected to the upper or side canvas borders are eligible for removal.
- Rows from source Y=400 downward are protected so the pale-blue desk remains intact.
- Enclosed white regions such as hair, eye whites, highlights, and teeth are preserved.
- Synthetic tests verify that enclosed white regions and the protected desk band stay opaque.
- A colored-background visual check confirmed clean silhouette retention.

Accepted output: `assets/character/master_transparent.png`

## Expression strategy

The original art is preserved as the master. Initial interactive expressions use Godot overlays, masks, transforms, and shaders rather than generated redraws. Any later raster expression replacement must pass identity and alignment comparison before it can replace the procedural version.
