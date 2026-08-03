import unittest
from pathlib import Path


class PackageScriptTests(unittest.TestCase):
    def test_package_docs_describe_harmonized_mode_and_six_scales(self):
        root = Path(__file__).resolve().parents[2]
        package = (root / "scripts" / "package.ps1").read_text(encoding="utf-8")
        readme = (root / "README.md").read_text(encoding="utf-8")

        for text in ("和谐版", "30%", "50%", "150%"):
            with self.subTest(text=text):
                self.assertIn(text, package)
                self.assertIn(text, readme)

    def test_standard_export_copies_dotnet_data_directory(self):
        script_path = Path(__file__).resolve().parents[1] / "package.ps1"
        script = script_path.read_text(encoding="utf-8")

        self.assertIn('data_DesktopPet_windows_x86_64', script)
        self.assertIn('Copy-Item -LiteralPath $standardData', script)

    def test_portable_fallback_can_be_selected_when_standard_export_is_stale(self):
        script_path = Path(__file__).resolve().parents[1] / "package.ps1"
        script = script_path.read_text(encoding="utf-8")

        self.assertIn("[switch]$ForcePortable", script)
        self.assertIn("-not $ForcePortable", script)

    def test_export_excludes_generated_and_development_directories(self):
        preset_path = Path(__file__).resolve().parents[2] / "export_presets.cfg"
        preset = preset_path.read_text(encoding="utf-8")

        for excluded in (
            "analysis/*",
            "artifacts/*",
            "exports/*",
            "tests/*",
            "src/Core/bin/*",
            "src/Core/obj/*",
            "assets/character/harmonized_review/*",
            "assets/character/generated_sources/*",
            "assets/character/frames/*",
        ):
            self.assertIn(excluded, preset)


if __name__ == "__main__":
    unittest.main()
