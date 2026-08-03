from __future__ import annotations

import argparse
from pathlib import Path
from typing import Iterable, Sequence

from PIL import Image, ImageDraw, ImageFilter


Ellipse = tuple[int, int, int, int]
Polygon = Sequence[tuple[int, int]]


def composite_variant(
    master: Image.Image,
    variant: Image.Image,
    *,
    ellipses: Iterable[Ellipse] = (),
    polygons: Iterable[Polygon] = (),
    feather_radius: float = 3,
) -> Image.Image:
    master_rgba = master.convert("RGBA")
    variant_rgba = variant.convert("RGBA").resize(
        master_rgba.size,
        Image.Resampling.LANCZOS,
    )

    mask = Image.new("L", master_rgba.size, 0)
    draw = ImageDraw.Draw(mask)
    for bounds in ellipses:
        draw.ellipse(bounds, fill=255)
    for points in polygons:
        draw.polygon(points, fill=255)
    if feather_radius > 0:
        mask = mask.filter(ImageFilter.GaussianBlur(feather_radius))

    result = Image.composite(variant_rgba, master_rgba, mask)
    result.putalpha(master_rgba.getchannel("A"))
    return result


FRAME_SPECS = {
    "blink": {
        "ellipses": [(100, 238, 226, 351), (257, 238, 390, 351)],
    },
    "hover": {
        "ellipses": [
            (94, 226, 231, 357),
            (252, 226, 397, 358),
            (160, 304, 323, 430),
        ],
    },
    "swallow_anticipation": {
        "ellipses": [
            (98, 232, 230, 354),
            (253, 232, 394, 356),
            (146, 292, 335, 438),
        ],
    },
    "swallow_max": {
        "ellipses": [
            (96, 229, 232, 358),
            (251, 229, 398, 360),
            (139, 268, 349, 446),
        ],
    },
    "swallow_gulp": {
        "ellipses": [
            (91, 230, 235, 361),
            (247, 230, 402, 361),
            (131, 302, 349, 436),
            (82, 286, 209, 407),
            (284, 286, 421, 407),
        ],
    },
}


def build_frames(master_path: Path, source_dir: Path, output_dir: Path) -> None:
    master = Image.open(master_path).convert("RGBA")
    output_dir.mkdir(parents=True, exist_ok=True)
    master.save(output_dir / "idle.png")

    for name, spec in FRAME_SPECS.items():
        variant_path = source_dir / f"{name}.png"
        if not variant_path.is_file():
            raise FileNotFoundError(f"Missing generated source: {variant_path}")
        variant = Image.open(variant_path)
        frame = composite_variant(
            master,
            variant,
            ellipses=spec.get("ellipses", ()),
            polygons=spec.get("polygons", ()),
            feather_radius=spec.get("feather_radius", 3),
        )
        frame.save(output_dir / f"{name}.png")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--master", type=Path, required=True)
    parser.add_argument("--source-dir", type=Path, required=True)
    parser.add_argument("--output-dir", type=Path, required=True)
    args = parser.parse_args()
    build_frames(args.master, args.source_dir, args.output_dir)


if __name__ == "__main__":
    main()
