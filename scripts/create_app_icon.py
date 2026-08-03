from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "assets" / "character" / "master_transparent.png"
TARGET = ROOT / "assets" / "icons" / "app.ico"


def main() -> None:
    source = Image.open(SOURCE).convert("RGBA")
    portrait = source.crop((48, 0, 464, 416))
    canvas = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    canvas.alpha_composite(portrait.resize((448, 448), Image.Resampling.LANCZOS), (32, 32))
    TARGET.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(TARGET, format="ICO", sizes=[(16, 16), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])


if __name__ == "__main__":
    main()
