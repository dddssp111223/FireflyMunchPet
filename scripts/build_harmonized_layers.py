from __future__ import annotations

from pathlib import Path

from PIL import Image

from build_character_layers import (
    CANVAS_SIZE,
    EYE_CROP_SIZE,
    LEFT_EYE_CENTER,
    RIGHT_EYE_CENTER,
    build_eye_clip_mask,
    composite_local,
    ellipse_mask,
    extract_desk,
    extract_eye_layer,
    remove_desk,
    union_masks,
)


ROOT = Path(__file__).resolve().parents[1]
REVIEW = ROOT / "assets" / "character" / "harmonized_review"
GENERATED = REVIEW / "generated_sources"
OUTPUT = ROOT / "assets" / "character" / "layers_harmonized"
PREVIEW = ROOT / "analysis" / "harmonized_runtime_recomposition.png"


def build_layers() -> None:
    OUTPUT.mkdir(parents=True, exist_ok=True)

    idle = Image.open(REVIEW / "idle_harmonized.png").convert("RGBA")
    hover = Image.open(REVIEW / "hover_harmonized.png").convert("RGBA")
    eye_generated = Image.open(
        GENERATED / "eye_base_harmonized.png"
    ).convert("RGBA")

    eye_mask = union_masks(
        ellipse_mask(CANVAS_SIZE, (118, 244, 218, 348), 3.0),
        ellipse_mask(CANVAS_SIZE, (267, 244, 374, 350), 3.0),
    )
    neutral = composite_local(idle, eye_generated, eye_mask)

    body_idle = remove_desk(neutral)
    body_idle.save(OUTPUT / "body_idle.png")
    body_idle.save(OUTPUT / "body_hover.png")

    frame_names = {
        "blink_harmonized.png": "body_blink.png",
        "swallow_anticipation_harmonized.png": "body_swallow_anticipation.png",
        "swallow_max_harmonized.png": "body_swallow_max.png",
        "swallow_gulp_harmonized.png": "body_swallow_gulp.png",
    }
    for source_name, output_name in frame_names.items():
        remove_desk(
            Image.open(REVIEW / source_name).convert("RGBA")
        ).save(OUTPUT / output_name)

    extract_desk(idle).save(OUTPUT / "desk.png")

    for source, suffix, left_radii, right_radii in (
        (idle, "idle", (31, 34), (32, 36)),
        (hover, "star", (32, 35), (33, 37)),
    ):
        extract_eye_layer(source, LEFT_EYE_CENTER, left_radii).save(
            OUTPUT / f"left_iris_{suffix}.png"
        )
        extract_eye_layer(source, RIGHT_EYE_CENTER, right_radii).save(
            OUTPUT / f"right_iris_{suffix}.png"
        )

    build_eye_clip_mask(neutral, LEFT_EYE_CENTER, (47, 48)).save(
        OUTPUT / "left_eye_mask.png"
    )
    build_eye_clip_mask(neutral, RIGHT_EYE_CENTER, (49, 49)).save(
        OUTPUT / "right_eye_mask.png"
    )


def build_preview() -> None:
    preview = Image.new("RGBA", CANVAS_SIZE, (0, 184, 184, 255))
    preview.alpha_composite(Image.open(OUTPUT / "body_idle.png").convert("RGBA"))
    for center, name in (
        (LEFT_EYE_CENTER, "left_iris_idle.png"),
        (RIGHT_EYE_CENTER, "right_iris_idle.png"),
    ):
        eye = Image.open(OUTPUT / name).convert("RGBA")
        preview.alpha_composite(
            eye,
            (
                center[0] - EYE_CROP_SIZE // 2,
                center[1] - EYE_CROP_SIZE // 2,
            ),
        )
    preview.alpha_composite(Image.open(OUTPUT / "desk.png").convert("RGBA"))
    PREVIEW.parent.mkdir(parents=True, exist_ok=True)
    preview.convert("RGB").save(PREVIEW)


def main() -> int:
    build_layers()
    build_preview()
    print(OUTPUT)
    print(PREVIEW)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
