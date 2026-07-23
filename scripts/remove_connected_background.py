from __future__ import annotations

import argparse
from collections import deque
from pathlib import Path

from PIL import Image


def _distance_from_white(pixel: tuple[int, int, int, int]) -> float:
    red, green, blue, _ = pixel
    return ((255 - red) ** 2 + (255 - green) ** 2 + (255 - blue) ** 2) ** 0.5


def remove_connected_background(
    source: Image.Image,
    *,
    threshold: float = 72,
    protect_from_y: int | None = None,
) -> Image.Image:
    image = source.convert("RGBA")
    width, height = image.size
    protected_y = height if protect_from_y is None else max(0, min(height, protect_from_y))
    pixels = image.load()
    connected = bytearray(width * height)
    queue: deque[tuple[int, int]] = deque()

    def candidate(x: int, y: int) -> bool:
        return (
            y < protected_y
            and pixels[x, y][3] > 0
            and _distance_from_white(pixels[x, y]) <= threshold
        )

    def enqueue(x: int, y: int) -> None:
        index = y * width + x
        if connected[index] or not candidate(x, y):
            return
        connected[index] = 1
        queue.append((x, y))

    for x in range(width):
        enqueue(x, 0)
    for y in range(protected_y):
        enqueue(0, y)
        enqueue(width - 1, y)

    while queue:
        x, y = queue.popleft()
        if x > 0:
            enqueue(x - 1, y)
        if x + 1 < width:
            enqueue(x + 1, y)
        if y > 0:
            enqueue(x, y - 1)
        if y + 1 < protected_y:
            enqueue(x, y + 1)

    output = image.copy()
    out_pixels = output.load()
    transparent_cutoff = min(18.0, threshold)
    ramp = max(1.0, threshold - transparent_cutoff)

    for y in range(protected_y):
        for x in range(width):
            if not connected[y * width + x]:
                continue
            red, green, blue, alpha = out_pixels[x, y]
            distance = _distance_from_white((red, green, blue, alpha))
            matte = 0 if distance <= transparent_cutoff else round(255 * (distance - transparent_cutoff) / ramp)
            out_pixels[x, y] = (red, green, blue, min(alpha, max(0, min(255, matte))))

    return output


def main() -> int:
    parser = argparse.ArgumentParser(description="Remove only border-connected near-white background.")
    parser.add_argument("--input", required=True)
    parser.add_argument("--out", required=True)
    parser.add_argument("--threshold", type=float, default=72)
    parser.add_argument("--protect-from-y", type=int)
    args = parser.parse_args()

    source_path = Path(args.input)
    output_path = Path(args.out)
    with Image.open(source_path) as source:
        result = remove_connected_background(
            source,
            threshold=args.threshold,
            protect_from_y=args.protect_from_y,
        )
        output_path.parent.mkdir(parents=True, exist_ok=True)
        result.save(output_path, "PNG")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
