from __future__ import annotations

import unittest
from pathlib import Path

import numpy as np
from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
REVIEW = ROOT / "assets" / "character" / "harmonized_review"
LAYERS = ROOT / "assets" / "character" / "layers_harmonized"

BODY_NAMES = (
    "body_idle.png",
    "body_hover.png",
    "body_blink.png",
    "body_swallow_anticipation.png",
    "body_swallow_max.png",
    "body_swallow_gulp.png",
)
EYE_NAMES = (
    "left_eye_mask.png",
    "right_eye_mask.png",
    "left_iris_idle.png",
    "right_iris_idle.png",
    "left_iris_star.png",
    "right_iris_star.png",
)


def rgba(path: Path) -> np.ndarray:
    with Image.open(path) as image:
        return np.asarray(image.convert("RGBA"))


class HarmonizedRuntimeLayerTests(unittest.TestCase):
    def test_complete_harmonized_texture_bank_exists(self) -> None:
        for name in (*BODY_NAMES, "desk.png", *EYE_NAMES):
            with self.subTest(asset=name):
                self.assertTrue((LAYERS / name).is_file(), f"missing {name}")

    def test_body_desk_and_eye_dimensions_are_runtime_compatible(self) -> None:
        for name in (*BODY_NAMES, "desk.png"):
            with self.subTest(asset=name):
                image = rgba(LAYERS / name)
                self.assertEqual((512, 512, 4), image.shape)
                self.assertGreater(int(np.count_nonzero(image[..., 3])), 1000)
        for name in EYE_NAMES:
            with self.subTest(asset=name):
                image = rgba(LAYERS / name)
                self.assertEqual((112, 112, 4), image.shape)
                self.assertGreater(int(np.count_nonzero(image[..., 3])), 50)

    def test_idle_body_and_desk_recompose_harmonized_alpha_exactly(self) -> None:
        source_alpha = rgba(REVIEW / "idle_harmonized.png")[..., 3]
        body_alpha = rgba(LAYERS / "body_idle.png")[..., 3]
        desk_alpha = rgba(LAYERS / "desk.png")[..., 3]

        self.assertEqual(0, int(np.count_nonzero((body_alpha > 0) & (desk_alpha > 0))))
        self.assertTrue(np.array_equal(source_alpha, np.maximum(body_alpha, desk_alpha)))

    def test_normal_and_star_irises_are_separate_authored_assets(self) -> None:
        for side in ("left", "right"):
            normal = rgba(LAYERS / f"{side}_iris_idle.png")
            star = rgba(LAYERS / f"{side}_iris_star.png")
            self.assertFalse(np.array_equal(normal, star))


if __name__ == "__main__":
    unittest.main()
