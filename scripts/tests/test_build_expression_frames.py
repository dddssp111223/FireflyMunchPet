import unittest

from PIL import Image

from scripts.build_expression_frames import composite_variant


class CompositeVariantTests(unittest.TestCase):
    def test_preserves_master_pixels_outside_mask_and_master_alpha_everywhere(self):
        master = Image.new("RGBA", (8, 8), (20, 40, 60, 255))
        master.putpixel((7, 7), (20, 40, 60, 0))
        variant = Image.new("RGB", (16, 16), (220, 80, 100))

        result = composite_variant(
            master,
            variant,
            ellipses=[(2, 2, 6, 6)],
            feather_radius=0,
        )

        self.assertEqual((8, 8), result.size)
        self.assertEqual(master.getpixel((0, 0)), result.getpixel((0, 0)))
        self.assertEqual((220, 80, 100, 255), result.getpixel((4, 4)))
        self.assertEqual(0, result.getpixel((7, 7))[3])
        self.assertEqual(
            list(master.getchannel("A").get_flattened_data()),
            list(result.getchannel("A").get_flattened_data()),
        )


if __name__ == "__main__":
    unittest.main()
