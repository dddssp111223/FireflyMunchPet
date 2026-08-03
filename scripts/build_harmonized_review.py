from __future__ import annotations

import hashlib
import json
from datetime import date
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
FRAMES = ROOT / "assets" / "character" / "frames"
REVIEW = ROOT / "assets" / "character" / "harmonized_review"
GENERATED = REVIEW / "generated_sources"
GENERATION_CROPS = REVIEW / "generation_crops"

PAIRS = {
    "idle.png": "idle_harmonized.png",
    "blink.png": "blink_harmonized.png",
    "hover.png": "hover_harmonized.png",
    "swallow_anticipation.png": "swallow_anticipation_harmonized.png",
    "swallow_max.png": "swallow_max_harmonized.png",
    "swallow_gulp.png": "swallow_gulp_harmonized.png",
}

FRAME_LABELS = {
    "idle.png": "Idle",
    "blink.png": "Blink",
    "hover.png": "Hover / Star",
    "swallow_anticipation.png": "Swallow anticipation",
    "swallow_max.png": "Maximum bite",
    "swallow_gulp.png": ">< Gulp",
}

# Every path follows a bright saliva string in the accepted 512px keyframe.
# Narrow masks let ImageGen supply only the dry artwork hidden by the liquid;
# the accepted character pixels remain untouched everywhere else.
MOUTH_PATHS: dict[str, list[tuple[list[tuple[int, int]], int]]] = {
    "idle.png": [
        ([(190, 337), (190, 357), (191, 378), (190, 402)], 8),
        ([(248, 352), (249, 374), (248, 402)], 8),
    ],
    "blink.png": [
        ([(190, 337), (190, 357), (191, 378), (190, 402)], 8),
        ([(248, 352), (249, 374), (248, 402)], 8),
    ],
    "hover.png": [
        ([(190, 337), (190, 357), (191, 378), (190, 402)], 8),
        ([(248, 352), (249, 374), (248, 402)], 8),
    ],
    "swallow_anticipation.png": [
        ([(199, 322), (199, 346), (200, 373), (200, 402)], 8),
        ([(264, 355), (265, 376), (264, 404)], 8),
    ],
    "swallow_max.png": [
        ([(191, 329), (188, 352), (185, 377), (183, 402)], 9),
        ([(258, 337), (259, 361), (261, 386), (262, 405)], 9),
    ],
    "swallow_gulp.png": [
        ([(251, 365), (253, 377), (253, 391), (253, 408)], 9),
    ],
}

# Sample dry pixels from the inside of the same mouth/tongue region. This keeps
# the accepted frame's palette and shading instead of importing ImageGen drift.
MOUTH_CLONE_OFFSETS: dict[str, list[int]] = {
    "idle.png": [20, -20],
    "blink.png": [20, -20],
    "hover.png": [20, -20],
    "swallow_anticipation.png": [22, -24],
    "swallow_max.png": [24, -24],
    "swallow_gulp.png": [-24],
}

BOTTOM_DROPLETS: dict[str, list[tuple[int, int, int, int]]] = {
    "idle.png": [(180, 399, 199, 419), (237, 399, 257, 419)],
    "blink.png": [(180, 399, 199, 419), (237, 399, 257, 419)],
    "hover.png": [(180, 399, 199, 419), (237, 399, 257, 419)],
    "swallow_anticipation.png": [(188, 397, 209, 420), (252, 398, 274, 420)],
    "swallow_max.png": [(173, 392, 205, 421), (250, 392, 280, 421)],
    "swallow_gulp.png": [],
}

DESK_POLYGON = [
    (142, 412),
    (158, 406),
    (190, 406),
    (208, 412),
    (226, 405),
    (247, 409),
    (269, 407),
    (293, 412),
    (325, 418),
    (319, 431),
    (292, 439),
    (266, 444),
    (244, 444),
    (220, 451),
    (188, 451),
    (165, 443),
    (144, 435),
]

PROMPT = """Use case: precise-object-edit
Asset type: aligned Windows desktop-pet animation keyframe
Primary request: remove every visible saliva element only, including the desk puddle, wet streaks, mouth-corner droplets, and saliva strings. Reconstruct clean dry desk and mouth artwork.
Constraints: preserve the exact frame role, composition, identity, pose, expression, mouth, tongue, teeth, eyes, hair, ornaments, line art, palette, shading, desk edge, and desk geometry. No redesign, restyling, text, watermark, or new elements.
"""

GENERATION_CROP_BOX = (128, 256, 384, 512)


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def load_rgba(path: Path) -> Image.Image:
    return Image.open(path).convert("RGBA")


def normalize_candidate(path: Path) -> Image.Image:
    image = load_rgba(path)
    if image.size != (512, 512):
        image = image.resize((512, 512), Image.Resampling.LANCZOS)
    return image


def make_generation_crops() -> None:
    GENERATION_CROPS.mkdir(parents=True, exist_ok=True)
    for source_name, output_name in PAIRS.items():
        load_rgba(FRAMES / source_name).crop(GENERATION_CROP_BOX).save(
            GENERATION_CROPS / output_name
        )


def mouth_mask(frame_name: str) -> Image.Image:
    mask = Image.new("L", (512, 512), 0)
    draw = ImageDraw.Draw(mask)
    for points, width in MOUTH_PATHS[frame_name]:
        draw.line(points, fill=255, width=width, joint="curve")
        radius = max(4, width // 2 + 2)
        for x, y in (points[0], points[-1]):
            draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=255)
    return mask


def harmonic_inpaint(
    source: Image.Image,
    candidate: Image.Image,
    hard_mask: Image.Image,
) -> Image.Image:
    """Solve a small Laplace fill bounded by the accepted mouth artwork."""
    source_array = np.asarray(source, dtype=np.uint8)
    candidate_array = np.asarray(candidate, dtype=np.uint8)
    unknown = np.asarray(hard_mask, dtype=np.uint8) > 0
    working = source_array[..., :3].astype(np.float32)
    # The generated dry frame is only an initialization. Repeated relaxation
    # makes the accepted pixels around the narrow mask the final boundary truth.
    working[unknown] = candidate_array[..., :3][unknown]
    for _ in range(80):
        average = (
            np.roll(working, 1, axis=0)
            + np.roll(working, -1, axis=0)
            + np.roll(working, 1, axis=1)
            + np.roll(working, -1, axis=1)
        ) * 0.25
        working[unknown] = average[unknown]
    repaired_array = source_array.copy()
    repaired_array[..., :3] = np.clip(np.rint(working), 0, 255).astype(np.uint8)
    repaired = Image.fromarray(repaired_array, "RGBA")
    soft_mask = hard_mask.filter(ImageFilter.GaussianBlur(0.65))
    return Image.composite(repaired, source, soft_mask)


def repair_bottom_droplets(image: Image.Image, frame_name: str) -> Image.Image:
    """Extend dry mouth/tongue color down through droplets at the desk seam."""
    if not BOTTOM_DROPLETS[frame_name]:
        return image
    image_array = np.asarray(image, dtype=np.uint8)
    donor = np.empty_like(image_array)
    distance = 18
    donor[distance:] = image_array[:-distance]
    donor[:distance] = image_array[:1]
    donor[..., 3] = image_array[..., 3]
    mask = Image.new("L", image.size, 0)
    draw = ImageDraw.Draw(mask)
    for bounds in BOTTOM_DROPLETS[frame_name]:
        draw.ellipse(bounds, fill=255)
    return Image.composite(
        Image.fromarray(donor, "RGBA"),
        image,
        mask.filter(ImageFilter.GaussianBlur(0.8)),
    )


def directional_clone_inpaint(source: Image.Image, frame_name: str) -> Image.Image:
    """Clone nearby dry artwork across each narrow saliva path."""
    source_array = np.asarray(source, dtype=np.uint8)
    result = source.copy()
    for (points, width), offset_x in zip(
        MOUTH_PATHS[frame_name],
        MOUTH_CLONE_OFFSETS[frame_name],
        strict=True,
    ):
        mask = Image.new("L", source.size, 0)
        draw = ImageDraw.Draw(mask)
        draw.line(points, fill=255, width=width, joint="curve")
        radius = max(7, width // 2 + 3)
        for x, y in (points[0], points[-1]):
            draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=255)

        donor = np.empty_like(source_array)
        if offset_x > 0:
            donor[:, :-offset_x] = source_array[:, offset_x:]
            donor[:, -offset_x:] = source_array[:, -1:]
        else:
            distance = -offset_x
            donor[:, distance:] = source_array[:, :-distance]
            donor[:, :distance] = source_array[:, :1]
        donor[..., 3] = source_array[..., 3]
        donor_image = Image.fromarray(donor, "RGBA")
        result = Image.composite(
            donor_image,
            result,
            mask.filter(ImageFilter.GaussianBlur(0.8)),
        )
    return result


def desk_mask() -> Image.Image:
    mask = Image.new("L", (512, 512), 0)
    draw = ImageDraw.Draw(mask)
    draw.polygon(DESK_POLYGON, fill=255)
    # Below y=421 this central band is desk-only in every approved frame. Cover
    # it completely so differently rendered puddle fringes cannot survive in a
    # swallow expression or flicker between animation states.
    draw.rectangle((135, 421, 334, 457), fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(1.4))
    ImageDraw.Draw(mask).rectangle((135, 421, 334, 457), fill=255)
    return mask


def color_match_candidate(
    source: Image.Image,
    candidate: Image.Image,
    mask: Image.Image,
    *,
    light_only: bool,
) -> np.ndarray:
    source_rgb = np.asarray(source, dtype=np.uint8)[..., :3]
    candidate_rgb = np.asarray(candidate, dtype=np.uint8)[..., :3]
    mask_array = np.asarray(mask, dtype=np.uint8)
    expanded = np.asarray(mask.filter(ImageFilter.MaxFilter(19)), dtype=np.uint8)
    ring = (expanded > 0) & (mask_array < 8)
    if light_only:
        ring &= source_rgb.min(axis=2) > 175
    if np.count_nonzero(ring) < 20:
        offset = np.zeros(3, dtype=np.float32)
    else:
        delta = source_rgb.astype(np.int16) - candidate_rgb.astype(np.int16)
        offset = np.median(delta[ring], axis=0).astype(np.float32)
        offset = np.clip(offset, -32, 32)
    return np.clip(candidate_rgb.astype(np.float32) + offset, 0, 255)


def blend_local(
    source: Image.Image,
    candidate: Image.Image,
    mask: Image.Image,
    *,
    light_only: bool = False,
) -> Image.Image:
    source_array = np.asarray(source, dtype=np.uint8).copy()
    replacement = color_match_candidate(source, candidate, mask, light_only=light_only)
    weight = np.asarray(mask, dtype=np.float32)[..., None] / 255.0
    source_array[..., :3] = np.rint(
        source_array[..., :3].astype(np.float32) * (1.0 - weight)
        + replacement * weight
    ).astype(np.uint8)
    return Image.fromarray(source_array, "RGBA")


def build_frames() -> list[dict[str, str]]:
    REVIEW.mkdir(parents=True, exist_ok=True)
    records: list[dict[str, str]] = []
    for source_name, output_name in PAIRS.items():
        source_path = FRAMES / source_name
        generated_path = GENERATED / output_name
        source = load_rgba(source_path)
        candidate = normalize_candidate(generated_path)
        array = np.asarray(candidate, dtype=np.uint8).copy()
        # ImageGen owns the harmonized keyframe artwork. The approved source owns
        # the exact transparency silhouette so the desktop window never gains a
        # black box or loses ornaments at the edge.
        array[..., 3] = np.asarray(source, dtype=np.uint8)[..., 3]
        final = Image.fromarray(array, "RGBA")
        output_path = REVIEW / output_name
        final.save(output_path)
        records.append(
            {
                "source": source_name,
                "source_sha256": sha256(source_path),
                "generated_source": f"generated_sources/{output_name}",
                "generated_sha256": sha256(generated_path),
                "output": output_name,
                "output_sha256": sha256(output_path),
            }
        )
    return records


def composite_background(frame: Image.Image, color: tuple[int, int, int]) -> Image.Image:
    background = Image.new("RGBA", frame.size, (*color, 255))
    return Image.alpha_composite(background, frame).convert("RGB")


def make_sheets() -> None:
    cell = 512
    label_height = 34
    review_sheet = Image.new("RGB", (cell * 3, (cell + label_height) * 2), "white")
    generated_sheet = Image.new("RGB", (cell * 3, (cell + label_height) * 2), "white")
    review_draw = ImageDraw.Draw(review_sheet)
    generated_draw = ImageDraw.Draw(generated_sheet)
    audit_sheet = Image.new("RGB", (cell * 2, cell * 6), "white")
    for index, (source_name, output_name) in enumerate(PAIRS.items()):
        frame = load_rgba(REVIEW / output_name)
        x = (index % 3) * cell
        y = (index // 3) * (cell + label_height)
        review_sheet.paste(composite_background(frame, (8, 188, 188)), (x, y))
        review_draw.text((x + 8, y + cell + 9), FRAME_LABELS[source_name], fill="black")
        generated_array = np.asarray(normalize_candidate(GENERATED / output_name), dtype=np.uint8).copy()
        generated_array[..., 3] = np.asarray(
            load_rgba(FRAMES / source_name), dtype=np.uint8
        )[..., 3]
        generated_frame = Image.fromarray(generated_array, "RGBA")
        generated_sheet.paste(composite_background(generated_frame, (8, 188, 188)), (x, y))
        generated_draw.text((x + 8, y + cell + 9), FRAME_LABELS[source_name], fill="black")
        audit_sheet.paste(composite_background(frame, (8, 188, 188)), (0, index * cell))
        audit_sheet.paste(composite_background(frame, (35, 35, 42)), (cell, index * cell))
    review_sheet.save(REVIEW / "review_sheet.png")
    generated_sheet.save(REVIEW / "review_sheet_generated.png")
    audit_sheet.save(REVIEW / "review_sheet_audit.png")

    mouth_box = (150, 300, 285, 421)
    zoom_scale = 3
    mouth_width = (mouth_box[2] - mouth_box[0]) * zoom_scale
    mouth_height = (mouth_box[3] - mouth_box[1]) * zoom_scale
    mouth_sheet = Image.new("RGB", (mouth_width * 3, (mouth_height + label_height) * 2), "white")
    mouth_draw = ImageDraw.Draw(mouth_sheet)
    for index, (source_name, output_name) in enumerate(PAIRS.items()):
        frame = load_rgba(REVIEW / output_name)
        crop = frame.crop(mouth_box).resize(
            (mouth_width, mouth_height),
            Image.Resampling.NEAREST,
        )
        x = (index % 3) * mouth_width
        y = (index // 3) * (mouth_height + label_height)
        mouth_sheet.paste(composite_background(crop, (8, 188, 188)), (x, y))
        mouth_draw.text((x + 8, y + mouth_height + 9), FRAME_LABELS[source_name], fill="black")
    mouth_sheet.save(REVIEW / "review_sheet_mouth_zoom.png")


def write_manifest(records: list[dict[str, str]]) -> None:
    manifest = {
        "tool_mode": "built-in image_gen",
        "generation_date": date.today().isoformat(),
        "prompt": PROMPT,
        "localized_transfer": {
            "method": "full ImageGen keyframe artwork with the accepted source alpha silhouette restored exactly",
            "source_alpha_preserved": True,
        },
        "frames": records,
    }
    (REVIEW / "manifest.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
        newline="\n",
    )


def main() -> int:
    make_generation_crops()
    records = build_frames()
    make_sheets()
    write_manifest(records)
    for record in records:
        print(record["output"], record["output_sha256"])
    print(REVIEW / "review_sheet.png")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
