from __future__ import annotations

import json
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]


class PublicReleaseContractTests(unittest.TestCase):
    def test_deprecated_cheek_interaction_is_absent_from_public_source(self) -> None:
        forbidden_names = [
            path.relative_to(ROOT).as_posix()
            for path in ROOT.rglob("*")
            if path.is_file() and "cheek" in path.name.casefold()
        ]
        self.assertEqual([], forbidden_names)

        manifest = json.loads(
            (ROOT / "assets/character/manifest.json").read_text(encoding="utf-8")
        )
        self.assertNotIn("cheekRegion", manifest)

        production_text = "\n".join(
            path.read_text(encoding="utf-8")
            for root in (ROOT / "src", ROOT / "scenes", ROOT / "scripts")
            for path in root.rglob("*")
            if path.is_file()
            and "tests" not in path.relative_to(root).parts
            and path.suffix in {".cs", ".tscn", ".py"}
        )
        self.assertNotIn("CheekPull", production_text)
        self.assertNotIn("CHEEK_", production_text)

    def test_readme_describes_the_final_public_feature_contract(self) -> None:
        readme = (ROOT / "README.md").read_text(encoding="utf-8")
        for required in (
            "和谐版",
            "提醒备忘录",
            "30%/50%/75%/100%/125%/150%",
            "%APPDATA%\\Godot\\app_userdata\\MunchPet\\reminders.json",
            "最多 5 项",
            "200",
            "两次 Q 弹",
        ):
            self.assertIn(required, readme)
        self.assertNotIn("四档缩放", readme)
        self.assertNotIn("脸颊", readme)


if __name__ == "__main__":
    unittest.main()
