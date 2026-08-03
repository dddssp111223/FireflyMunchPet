import unittest
from pathlib import Path


class ReminderVisualContractTests(unittest.TestCase):
    @property
    def root(self) -> Path:
        return Path(__file__).resolve().parents[2]

    def test_diagnostics_capture_every_approved_state(self):
        capture = (self.root / "src/Diagnostics/ReminderVisualCapture.cs").read_text(
            encoding="utf-8"
        )
        verifier = (self.root / "scripts/verify_reminder_visuals.py").read_text(
            encoding="utf-8"
        )
        for name in (
            "reminder_list",
            "reminder_edit",
            "reminder_200_chars",
            "reminder_bubble",
            "reminder_bubble_above",
            "reminder_bubble_right",
            "reminder_bubble_left",
            "reminder_bubble_below",
            "reminder_bubble_200_chars",
        ):
            self.assertIn(f'Save("{name}")', capture)
            self.assertIn(f'"{name}.png"', verifier)
        self.assertIn('Enumerable.Repeat("萤", 200)', capture)
        self.assertIn("63cbb4", verifier.lower())
        self.assertIn("369b84", verifier.lower())
        self.assertIn("effcf8", verifier.lower())
        self.assertIn("bubble_crops", verifier.lower())

    def test_docs_describe_persistence_and_limits(self):
        readme = (self.root / "README.md").read_text(encoding="utf-8")
        package = (self.root / "scripts/package.ps1").read_text(encoding="utf-8")
        for text in (
            "开启备忘录提醒",
            "5",
            "200",
            "reminders.json",
        ):
            self.assertIn(text, readme)
            self.assertIn(text, package)


if __name__ == "__main__":
    unittest.main()
