from __future__ import annotations

import hashlib
import unittest
from pathlib import Path

import numpy as np
from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
FRAMES = ROOT / "assets" / "character" / "frames"
REVIEW = ROOT / "assets" / "character" / "harmonized_review"
GENERATED = REVIEW / "generated_sources"

PAIRS = {
    "idle.png": "idle_harmonized.png",
    "blink.png": "blink_harmonized.png",
    "hover.png": "hover_harmonized.png",
    "swallow_anticipation.png": "swallow_anticipation_harmonized.png",
    "swallow_max.png": "swallow_max_harmonized.png",
    "swallow_gulp.png": "swallow_gulp_harmonized.png",
}

SOURCE_SHA256 = {
    "idle.png": "82F53D7F52F7C417C084B8546B73073DAF59D20ED08F3361B615CF391055CDD9",
    "blink.png": "A8FAF4C7F7215F23F6953D944F4605CB8676AE328F6D056F7D3F1C7A7B3A7E05",
    "hover.png": "A021C096683890E1DB6F0D2F7826324F8A6563EF5AF8534E1F26BA1011367AED",
    "swallow_anticipation.png": "71FDE4B5FE55878023DD93571ADECBD132B18510F6360BBECB5FB6630E1F6FF9",
    "swallow_max.png": "AC0F69ED7C6AEC5CE4E9F2B5724471603ACFED816AE2FA6B8BD9C1033CB06B38",
    "swallow_gulp.png": "68158B13DD8566B8EFB37F0E877F44305FE7BF551ED3D63D2EDC84EBB6EC30B3",
}

def rgba(path: Path) -> np.ndarray:
    with Image.open(path) as image:
        return np.asarray(image.convert("RGBA"))


class HarmonizedKeyframeTests(unittest.TestCase):
    def test_accepted_sources_remain_byte_identical(self) -> None:
        for name, expected in SOURCE_SHA256.items():
            with self.subTest(frame=name):
                path = FRAMES / name
                digest = hashlib.sha256(path.read_bytes()).hexdigest().upper()
                self.assertEqual(expected, digest)

    def test_six_harmonized_frames_exist_and_align(self) -> None:
        for source_name, output_name in PAIRS.items():
            with self.subTest(frame=source_name):
                output_path = REVIEW / output_name
                self.assertTrue(output_path.is_file(), f"missing {output_path}")
                source = rgba(FRAMES / source_name)
                output = rgba(output_path)
                self.assertEqual((512, 512, 4), output.shape)
                self.assertTrue(np.array_equal(source[..., 3], output[..., 3]))

    def test_outputs_use_generated_artwork_with_source_alpha(self) -> None:
        for source_name, output_name in PAIRS.items():
            with self.subTest(frame=source_name):
                output_path = REVIEW / output_name
                source = rgba(FRAMES / source_name)
                output = rgba(output_path)
                with Image.open(GENERATED / output_name) as image:
                    generated = np.asarray(
                        image.convert("RGBA").resize((512, 512), Image.Resampling.LANCZOS)
                    )
                self.assertTrue(np.array_equal(generated[..., :3], output[..., :3]))
                self.assertTrue(np.array_equal(source[..., 3], output[..., 3]))

    def test_review_artifacts_exist(self) -> None:
        for name in (
            "manifest.json",
            "review_sheet.png",
            "review_sheet_audit.png",
            "review_sheet_mouth_zoom.png",
        ):
            with self.subTest(artifact=name):
                self.assertTrue((REVIEW / name).is_file())


if __name__ == "__main__":
    unittest.main()
