from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter


CANVAS_SIZE = (512, 512)
DESK_TOP_LEFT_Y = 400
DESK_TOP_RIGHT_Y = 419

LEFT_EYE_CENTER = (165, 292)
RIGHT_EYE_CENTER = (319, 294)
EYE_CROP_SIZE = 112

def ellipse_mask(
    size: tuple[int, int],
    bounds: tuple[int, int, int, int],
    feather: float,
) -> Image.Image:
    mask = Image.new("L", size, 0)
    ImageDraw.Draw(mask).ellipse(bounds, fill=255)
    return mask.filter(ImageFilter.GaussianBlur(feather)) if feather else mask


def polygon_mask(
    size: tuple[int, int],
    points: tuple[tuple[int, int], ...],
    feather: float,
) -> Image.Image:
    mask = Image.new("L", size, 0)
    ImageDraw.Draw(mask).polygon(points, fill=255)
    return mask.filter(ImageFilter.GaussianBlur(feather)) if feather else mask


def union_masks(*masks: Image.Image) -> Image.Image:
    result = Image.new("L", masks[0].size, 0)
    for mask in masks:
        result = ImageChops.lighter(result, mask)
    return result


def composite_local(
    base: Image.Image,
    edited: Image.Image,
    mask: Image.Image,
) -> Image.Image:
    base_rgba = base.convert("RGBA")
    edited_rgba = edited.convert("RGBA").resize(
        base_rgba.size,
        Image.Resampling.LANCZOS,
    )
    result = Image.composite(edited_rgba, base_rgba, mask)
    result.putalpha(base_rgba.getchannel("A"))
    return result


def desk_boundary_y(x: int, width: int = CANVAS_SIZE[0]) -> int:
    if width <= 1:
        return DESK_TOP_LEFT_Y
    progress = x / (width - 1)
    return round(
        DESK_TOP_LEFT_Y
        + (DESK_TOP_RIGHT_Y - DESK_TOP_LEFT_Y) * progress
    )


def remove_desk(image: Image.Image) -> Image.Image:
    rgba = image.convert("RGBA")
    alpha = rgba.getchannel("A")
    pixels = alpha.load()
    for x in range(rgba.width):
        boundary = desk_boundary_y(x, rgba.width)
        for y in range(boundary, rgba.height):
            pixels[x, y] = 0
    rgba.putalpha(alpha)
    return rgba


def extract_desk(image: Image.Image) -> Image.Image:
    rgba = image.convert("RGBA")
    alpha = rgba.getchannel("A")
    pixels = alpha.load()
    for x in range(rgba.width):
        boundary = desk_boundary_y(x, rgba.width)
        for y in range(boundary):
            pixels[x, y] = 0
    rgba.putalpha(alpha)
    return rgba


def eye_crop_box(center: tuple[int, int]) -> tuple[int, int, int, int]:
    half = EYE_CROP_SIZE // 2
    return (
        center[0] - half,
        center[1] - half,
        center[0] + half,
        center[1] + half,
    )


def extract_eye_layer(
    source: Image.Image,
    center: tuple[int, int],
    iris_radii: tuple[int, int],
) -> Image.Image:
    crop_box = eye_crop_box(center)
    crop = source.convert("RGBA").crop(crop_box)
    local_center = (EYE_CROP_SIZE // 2, EYE_CROP_SIZE // 2)
    mask = ellipse_mask(
        crop.size,
        (
            local_center[0] - iris_radii[0],
            local_center[1] - iris_radii[1],
            local_center[0] + iris_radii[0],
            local_center[1] + iris_radii[1],
        ),
        1.3,
    )
    alpha = Image.composite(crop.getchannel("A"), Image.new("L", crop.size, 0), mask)
    crop.putalpha(alpha)
    return crop


def build_eye_clip_mask(
    eye_base: Image.Image,
    center: tuple[int, int],
    radii: tuple[int, int],
) -> Image.Image:
    local_center = (EYE_CROP_SIZE // 2, EYE_CROP_SIZE // 2)
    rough = ellipse_mask(
        (EYE_CROP_SIZE, EYE_CROP_SIZE),
        (
            local_center[0] - radii[0],
            local_center[1] - radii[1],
            local_center[0] + radii[0],
            local_center[1] + radii[1],
        ),
        1.0,
    )
    crop = eye_base.convert("RGBA").crop(eye_crop_box(center))
    mask = Image.new("L", crop.size, 0)
    rough_pixels = rough.load()
    crop_pixels = crop.load()
    mask_pixels = mask.load()
    for y in range(crop.height):
        for x in range(crop.width):
            red, green, blue, source_alpha = crop_pixels[x, y]
            minimum = min(red, green, blue)
            chroma = max(red, green, blue) - minimum
            light_score = max(0, min(255, round((minimum - 145) * 3.2)))
            neutral_score = max(0, min(255, round((92 - chroma) * 4.0)))
            mask_pixels[x, y] = min(
                source_alpha,
                rough_pixels[x, y],
                light_score,
                neutral_score,
            )
    mask = mask.filter(ImageFilter.GaussianBlur(0.7))
    white = Image.new("RGBA", mask.size, (255, 255, 255, 255))
    white.putalpha(mask)
    return white


def extract_right_hair_foreground(source: Image.Image) -> Image.Image:
    rgba = source.convert("RGBA")
    rough = polygon_mask(
        rgba.size,
        (
            (347, 198),
            (454, 185),
            (505, 250),
            (512, 425),
            (428, 431),
            (365, 414),
            (338, 377),
            (350, 338),
            (340, 286),
        ),
        1.0,
    )
    result = Image.new("RGBA", rgba.size, (0, 0, 0, 0))
    source_pixels = rgba.load()
    rough_pixels = rough.load()
    result_pixels = result.load()
    for y in range(rgba.height):
        for x in range(rgba.width):
            red, green, blue, source_alpha = source_pixels[x, y]
            maximum = max(red, green, blue)
            minimum = min(red, green, blue)
            chroma = maximum - minimum
            pale_hair = minimum > 132 and chroma < 68
            dark_line = maximum < 118 and chroma < 58
            if pale_hair or dark_line:
                alpha = min(source_alpha, rough_pixels[x, y])
                result_pixels[x, y] = (red, green, blue, alpha)
    return result


def build_layers(
    frame_dir: Path,
    generated_dir: Path,
    output_dir: Path,
) -> None:
    output_dir.mkdir(parents=True, exist_ok=True)

    idle = Image.open(frame_dir / "idle.png").convert("RGBA")
    hover = Image.open(frame_dir / "hover.png").convert("RGBA")
    eye_generated = Image.open(generated_dir / "eye_base_v2.png").convert("RGBA")

    left_eye_edit_mask = ellipse_mask(CANVAS_SIZE, (118, 244, 218, 348), 3.0)
    right_eye_edit_mask = ellipse_mask(CANVAS_SIZE, (267, 244, 374, 350), 3.0)
    eye_edit_mask = union_masks(left_eye_edit_mask, right_eye_edit_mask)

    base = composite_local(idle, eye_generated, eye_edit_mask)
    body_idle = remove_desk(base)
    body_idle.save(output_dir / "body_idle.png")
    body_idle.save(output_dir / "body_hover.png")

    for name in ("blink", "swallow_anticipation", "swallow_max", "swallow_gulp"):
        remove_desk(
            Image.open(frame_dir / f"{name}.png").convert("RGBA")
        ).save(output_dir / f"body_{name}.png")

    extract_desk(idle).save(output_dir / "desk.png")

    extract_eye_layer(idle, LEFT_EYE_CENTER, (31, 34)).save(
        output_dir / "left_iris_idle.png"
    )
    extract_eye_layer(idle, RIGHT_EYE_CENTER, (32, 36)).save(
        output_dir / "right_iris_idle.png"
    )
    extract_eye_layer(hover, LEFT_EYE_CENTER, (32, 35)).save(
        output_dir / "left_iris_star.png"
    )
    extract_eye_layer(hover, RIGHT_EYE_CENTER, (33, 37)).save(
        output_dir / "right_iris_star.png"
    )

    build_eye_clip_mask(base, LEFT_EYE_CENTER, (47, 48)).save(
        output_dir / "left_eye_mask.png"
    )
    build_eye_clip_mask(base, RIGHT_EYE_CENTER, (49, 49)).save(
        output_dir / "right_eye_mask.png"
    )


def build_rest_preview(output_dir: Path, preview_path: Path) -> None:
    preview = Image.new("RGBA", CANVAS_SIZE, (0, 184, 184, 255))
    preview.alpha_composite(Image.open(output_dir / "body_idle.png").convert("RGBA"))

    for center, name in (
        (LEFT_EYE_CENTER, "left_iris_idle.png"),
        (RIGHT_EYE_CENTER, "right_iris_idle.png"),
    ):
        iris = Image.open(output_dir / name).convert("RGBA")
        preview.alpha_composite(
            iris,
            (
                center[0] - EYE_CROP_SIZE // 2,
                center[1] - EYE_CROP_SIZE // 2,
            ),
        )

    preview.alpha_composite(Image.open(output_dir / "desk.png").convert("RGBA"))
    preview_path.parent.mkdir(parents=True, exist_ok=True)
    preview.convert("RGB").save(preview_path)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--frame-dir", type=Path, required=True)
    parser.add_argument("--generated-dir", type=Path, required=True)
    parser.add_argument("--output-dir", type=Path, required=True)
    parser.add_argument("--preview", type=Path)
    args = parser.parse_args()

    build_layers(args.frame_dir, args.generated_dir, args.output_dir)
    if args.preview is not None:
        build_rest_preview(args.output_dir, args.preview)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
