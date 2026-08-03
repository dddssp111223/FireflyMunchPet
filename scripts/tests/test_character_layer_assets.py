import unittest
from pathlib import Path

from PIL import Image


class CharacterLayerAssetTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.root = Path(__file__).resolve().parents[2]
        cls.layers = cls.root / "assets" / "character" / "layers"

    def test_required_rig_layers_exist(self):
        required = {
            "body_idle.png",
            "body_hover.png",
            "desk.png",
            "left_iris_idle.png",
            "right_iris_idle.png",
            "left_iris_star.png",
            "right_iris_star.png",
            "left_eye_mask.png",
            "right_eye_mask.png",
        }

        self.assertEqual(
            required,
            {path.name for path in self.layers.glob("*.png")} & required,
        )

    def test_eye_layers_have_real_transparency(self):
        for name in (
            "left_iris_idle.png",
            "right_iris_idle.png",
            "left_iris_star.png",
            "right_iris_star.png",
            "left_eye_mask.png",
            "right_eye_mask.png",
        ):
            with Image.open(self.layers / name).convert("RGBA") as image:
                alpha = image.getchannel("A")
                self.assertEqual(0, alpha.getextrema()[0], name)
                self.assertGreater(alpha.getextrema()[1], 240, name)

    def test_body_ends_at_lowest_contact_and_desk_reaches_bottom(self):
        with Image.open(self.layers / "body_idle.png").convert("RGBA") as body:
            body_alpha = body.getchannel("A")
            lower_alpha = body_alpha.crop((0, 419, body.width, body.height))
            self.assertEqual(0, lower_alpha.getextrema()[1])

        with Image.open(self.layers / "desk.png").convert("RGBA") as desk:
            self.assertGreater(
                desk.getchannel("A").crop((0, 419, desk.width, desk.height)).getextrema()[1],
                240,
            )

    def test_runtime_layer_directory_contains_no_cheek_drag_assets(self):
        forbidden = {
            "cheek_patch.png",
            "cheek_patch_stretch.png",
            "right_hair_foreground.png",
        }
        self.assertTrue(forbidden.isdisjoint(path.name for path in self.layers.glob("*.png")))

    def test_screen_left_hair_ornament_alpha_matches_idle_source(self):
        frames = self.root / "assets" / "character" / "frames"
        ornament_box = (0, 30, 115, 300)
        with Image.open(frames / "idle.png").convert("RGBA") as idle:
            expected = idle.getchannel("A").crop(ornament_box)
        with Image.open(self.layers / "body_idle.png").convert("RGBA") as body:
            actual = body.getchannel("A").crop(ornament_box)

        self.assertEqual(list(expected.get_flattened_data()), list(actual.get_flattened_data()))

    @staticmethod
    def _desk_boundary_y(x: int) -> int:
        return round(400 + 19 * x / 511)

    def test_desk_and_all_body_frames_use_complementary_sloped_masks(self):
        with Image.open(self.layers / "desk.png").convert("RGBA") as desk:
            desk_alpha = desk.getchannel("A")
            for x in range(desk.width):
                boundary = self._desk_boundary_y(x)
                self.assertEqual(
                    0,
                    desk_alpha.crop((x, 0, x + 1, boundary)).getextrema()[1],
                    f"desk contains static pixels above boundary at x={x}",
                )

        for body_path in self.layers.glob("body_*.png"):
            with Image.open(body_path).convert("RGBA") as body:
                body_alpha = body.getchannel("A")
                for x in range(body.width):
                    boundary = self._desk_boundary_y(x)
                    self.assertEqual(
                        0,
                        body_alpha.crop((x, boundary, x + 1, body.height)).getextrema()[1],
                        f"{body_path.name} contains desk pixels at x={x}",
                    )

    def test_idle_body_and_desk_recompose_contact_band_exactly(self):
        frames = self.root / "assets" / "character" / "frames"
        with Image.open(frames / "idle.png").convert("RGBA") as idle:
            expected = idle.crop((0, 380, idle.width, idle.height))
        with Image.open(self.layers / "body_idle.png").convert("RGBA") as body:
            with Image.open(self.layers / "desk.png").convert("RGBA") as desk:
                recomposed = Image.alpha_composite(body, desk).crop(
                    (0, 380, body.width, body.height)
                )

        self.assertEqual(expected.tobytes(), recomposed.tobytes())


if __name__ == "__main__":
    unittest.main()
