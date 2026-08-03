import argparse
from pathlib import Path

from PIL import Image, ImageChops


EXPECTED = (
    "reminder_list.png",
    "reminder_edit.png",
    "reminder_200_chars.png",
    "reminder_bubble.png",
    "reminder_bubble_above.png",
    "reminder_bubble_right.png",
    "reminder_bubble_left.png",
    "reminder_bubble_below.png",
    "reminder_bubble_200_chars.png",
)
MINT_HEX = "63cbb4"
MINT_DEEP_HEX = "369b84"
MINT = tuple(bytes.fromhex(MINT_HEX))
MINT_DEEP = tuple(bytes.fromhex(MINT_DEEP_HEX))
MINT_SOFT_HEX = "effcf8"
MINT_SOFT = tuple(bytes.fromhex(MINT_SOFT_HEX))
DEFAULT_CONTROL_GREY = (128, 130, 129)
BUBBLE_CROPS = {
    "reminder_bubble.png": (136, 170, 584, 430),
    "reminder_bubble_above.png": (136, 170, 584, 430),
    "reminder_bubble_right.png": (136, 170, 584, 430),
    "reminder_bubble_left.png": (136, 170, 584, 430),
    "reminder_bubble_below.png": (136, 170, 584, 430),
    "reminder_bubble_200_chars.png": (136, 100, 584, 488),
}


def near(pixel, target, tolerance=28):
    return all(abs(int(pixel[index]) - target[index]) <= tolerance for index in range(3))


def color_count(image, target, tolerance=28):
    pixels = image.get_flattened_data() if hasattr(image, "get_flattened_data") else image.getdata()
    return sum(1 for pixel in pixels if pixel[3] > 0 and near(pixel, target, tolerance))


def verify(input_dir: Path):
    images = {}
    for name in EXPECTED:
        path = input_dir / name
        if not path.is_file():
            raise AssertionError(f"missing reminder capture: {name}")
        image = Image.open(path).convert("RGBA")
        if image.size != (720, 620):
            raise AssertionError(f"unexpected reminder capture size for {name}: {image.size}")
        bounds = image.getchannel("A").getbbox()
        if bounds is None:
            raise AssertionError(f"fully transparent reminder capture: {name}")
        if bounds[0] < 8 or bounds[1] < 8 or bounds[2] > 712 or bounds[3] > 612:
            raise AssertionError(f"reminder content touches capture edge: {name} {bounds}")
        if color_count(image, MINT) < 12:
            raise AssertionError(f"mint accent missing from {name}")
        if name != "reminder_edit.png" and color_count(image, MINT_DEEP) < 12:
            raise AssertionError(f"deep mint action missing from {name}")
        if name in ("reminder_edit.png", "reminder_200_chars.png"):
            edit_surface = image.crop((60, 145, 660, 380))
            if color_count(edit_surface, DEFAULT_CONTROL_GREY, tolerance=8) > 800:
                raise AssertionError(f"default grey controls leaked into {name}")
        if name in BUBBLE_CROPS:
            bubble_crop = image.crop(BUBBLE_CROPS[name])
            local_bounds = bubble_crop.getchannel("A").getbbox()
            if local_bounds is None:
                raise AssertionError(f"bubble is fully transparent in {name}")
            width, height = bubble_crop.size
            if (
                local_bounds[0] < 5
                or local_bounds[1] < 5
                or local_bounds[2] > width - 5
                or local_bounds[3] > height - 5
            ):
                raise AssertionError(f"bubble chrome touches native window edge in {name}: {local_bounds}")
            if color_count(bubble_crop, MINT_SOFT, tolerance=12) < 1000:
                raise AssertionError(f"mint cloud surface missing from {name}")
        images[name] = image

    if ImageChops.difference(
        images["reminder_edit.png"].convert("RGB"),
        images["reminder_200_chars.png"].convert("RGB"),
    ).getbbox() is None:
        raise AssertionError("200-character editor state did not change")

    bubble = images["reminder_bubble.png"]
    lower_action = bubble.crop((430, 330, 565, 420))
    if color_count(lower_action, MINT_DEEP) < 12:
        raise AssertionError("acknowledge button is not visible in bubble action region")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--input-dir", type=Path, required=True)
    args = parser.parse_args()
    verify(args.input_dir)
    print("reminder visual checks passed: mint editor, 200-character state, and acknowledgement bubble")


if __name__ == "__main__":
    main()
