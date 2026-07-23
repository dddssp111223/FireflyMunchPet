import unittest

from PIL import Image

from scripts.remove_connected_background import remove_connected_background


class RemoveConnectedBackgroundTests(unittest.TestCase):
    def test_removes_only_border_connected_white(self):
        image = Image.new("RGBA", (7, 7), (255, 255, 255, 255))
        pixels = image.load()
        for x in range(2, 5):
            pixels[x, 2] = (0, 0, 0, 255)
            pixels[x, 4] = (0, 0, 0, 255)
        for y in range(2, 5):
            pixels[2, y] = (0, 0, 0, 255)
            pixels[4, y] = (0, 0, 0, 255)

        result = remove_connected_background(image, threshold=30, protect_from_y=7)

        self.assertEqual(0, result.getpixel((0, 0))[3])
        self.assertEqual(255, result.getpixel((3, 3))[3])

    def test_preserves_protected_bottom_region(self):
        image = Image.new("RGBA", (5, 5), (255, 255, 255, 255))

        result = remove_connected_background(image, threshold=30, protect_from_y=3)

        self.assertEqual(0, result.getpixel((0, 0))[3])
        self.assertEqual(255, result.getpixel((0, 4))[3])


if __name__ == "__main__":
    unittest.main()
