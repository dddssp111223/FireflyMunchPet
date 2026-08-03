from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageStat


EYE_REGIONS = ((105, 235, 225, 355), (255, 235, 385, 355))


def load_rgb(directory: Path, name: str) -> Image.Image:
    return Image.open(directory / name).convert("RGB")


def outside_eye_difference(first: Image.Image, second: Image.Image) -> Image.Image:
    difference = ImageChops.difference(first, second)
    keep = Image.new("L", first.size, 255)
    draw = ImageDraw.Draw(keep)
    for bounds in EYE_REGIONS:
        draw.ellipse(bounds, fill=0)
    result = Image.new("RGB", first.size, (0, 0, 0))
    result.paste(difference, mask=keep)
    return result


def assert_black(image: Image.Image, message: str) -> None:
    if image.getbbox() is not None:
        raise AssertionError(message)


def mean_absolute_difference(first: Image.Image, second: Image.Image) -> float:
    difference = ImageChops.difference(first, second)
    return sum(ImageStat.Stat(difference).mean) / 3


def verify(directory: Path) -> None:
    idle = load_rgb(directory, "idle.png")
    for prefix in ("", "harmonized_"):
        upper_right = load_rgb(directory, f"{prefix}eye_upper_right.png")
        lower_left = load_rgb(directory, f"{prefix}eye_lower_left.png")
        assert_black(
            outside_eye_difference(upper_right, lower_left),
            f"{prefix or 'original_'}eye tracking changed pixels outside the eyes.",
        )
        star = load_rgb(directory, f"{prefix}star.png")
        star_late = load_rgb(directory, f"{prefix}star_late.png")
        assert_black(
            ImageChops.difference(star, star_late),
            f"{prefix or 'original_'}star eyes moved or pulsed during file hover.",
        )

    for percent in (30, 50, 75, 100, 125, 150):
        target_size = round(512 * percent / 100)
        capture = load_rgb(directory, f"scale_{percent}.png")
        expected_size = (target_size, target_size)
        if capture.size != expected_size:
            raise AssertionError(
                f"Scale {percent}% captured {capture.size}, expected {expected_size}."
            )
        expected = idle.resize(expected_size, Image.Resampling.BILINEAR)
        difference = mean_absolute_difference(expected, capture)
        if difference > 8.0:
            raise AssertionError(
                f"Scale {percent}% is cropped or distorted; mean difference {difference:.2f}."
            )

    for idle_name, state_names in (
        ("idle.png", ("click_squash.png", "swallow_gulp.png")),
        (
            "harmonized_idle.png",
            ("harmonized_click_squash.png", "harmonized_gulp.png"),
        ),
    ):
        desk_idle = load_rgb(directory, idle_name).crop((0, 419, 512, 512))
        for name in state_names:
            desk_state = load_rgb(directory, name).crop((0, 419, 512, 512))
            assert_black(
                ImageChops.difference(desk_idle, desk_state),
                f"Static desk pixels moved during {name}.",
            )

    for required in (
        "star.png",
        "click_restored.png",
        "swallow_restored.png",
        "harmonized_idle.png",
        "harmonized_star.png",
        "harmonized_gulp.png",
        "harmonized_click_restored.png",
        "harmonized_swallow_restored.png",
    ):
        if not (directory / required).is_file():
            raise AssertionError(f"Missing visual capture: {required}")

    print(
        "visual capture checks passed: six uniform scales, two texture banks, "
        "eye-local motion, fixed star eyes, and static desks"
    )


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input-dir", type=Path, required=True)
    args = parser.parse_args()
    verify(args.input_dir)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
