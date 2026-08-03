import unittest
from pathlib import Path


class ReminderUiContractTests(unittest.TestCase):
    @property
    def root(self) -> Path:
        return Path(__file__).resolve().parents[2]

    def read(self, relative: str) -> str:
        return (self.root / relative).read_text(encoding="utf-8")

    def test_editor_matches_approved_mint_design(self):
        theme = self.read("src/App/Reminders/ReminderTheme.cs")
        editor_view = self.read("src/App/Reminders/ReminderEditorView.cs")
        editor_window = self.read("src/App/Reminders/ReminderEditorWindow.cs")

        self.assertIn('new Color("63cbb4")', theme)
        self.assertIn('new Color("369b84")', theme)
        self.assertIn('"提醒备忘录"', editor_window)
        for label in (
            "新建提醒",
            "定时提醒",
            "倒数提醒",
            "删除此事项",
            "保存提醒",
        ):
            self.assertIn(label, editor_view)
        self.assertIn("ReminderDocument.MaxItems", editor_view)
        self.assertIn("ReminderDefinition.MaxTextElements", editor_view)
        self.assertIn("CountTextElements", editor_view)
        self.assertIn("ShowOrFocus", editor_window)
        self.assertIn("GrabFocus", editor_window)

    def test_reminder_windows_are_native_and_not_embedded(self):
        project = self.read("project.godot")
        pet_root = self.read("src/App/PetRoot.cs")

        self.assertIn("window/subwindows/embed_subwindows=false", project)
        guard = pet_root.index("GetTree().Root.GuiEmbedSubwindows = false")
        creation = pet_root.index("CreateReminderCoordinator();")
        self.assertLess(guard, creation)

    def test_bubble_is_configured_transparent_before_native_window_creation(self):
        bubble_window = self.read("src/App/Reminders/ReminderBubbleWindow.cs")
        coordinator = self.read("src/App/Reminders/ReminderCoordinator.cs")
        probe = self.read("src/Diagnostics/ReminderNativeWindowProbe.cs")

        self.assertIn("CreateNative", bubble_window)
        self.assertIn("Transparent = true", bubble_window)
        self.assertIn("TransparentBg = true", bubble_window)
        self.assertIn("ReminderBubbleWindow.CreateNative", coordinator)
        self.assertIn("ReminderBubbleWindow.CreateNative", probe)
        self.assertLess(
            coordinator.index("ReminderBubbleWindow.CreateNative"),
            coordinator.index("AddChild(_bubble)"),
        )
        self.assertLess(
            probe.index("ReminderBubbleWindow.CreateNative"),
            probe.index("AddChild(_bubble)"),
        )

    def test_native_window_probe_checks_distinct_ids_and_visible_bubble(self):
        probe = self.read("src/Diagnostics/ReminderNativeWindowProbe.cs")
        scene = self.read("scenes/diagnostics/reminder_native_window_probe.tscn")

        self.assertIn("editor.GetWindowId()", probe)
        self.assertIn("bubble.GetWindowId()", probe)
        self.assertIn("_workArea.Encloses(bubbleRect)", probe)
        self.assertIn("GetTree().Quit(1)", probe)
        self.assertIn("user://native-window-probe.txt", probe)
        self.assertIn("File.WriteAllText", probe)
        self.assertIn("ReminderNativeWindowProbe.cs", scene)

    def test_bubble_and_character_use_approved_reminder_feedback(self):
        bubble_view = self.read("src/App/Reminders/ReminderBubbleView.cs")
        bubble_window = self.read("src/App/Reminders/ReminderBubbleWindow.cs")
        rig = self.read("src/Character/CharacterRig.cs")

        self.assertIn('Text = "提醒"', bubble_view)
        self.assertIn('Text = "知道了"', bubble_view)
        self.assertNotIn("稍后提醒", bubble_view)
        self.assertIn("ReminderBubblePlacement.Calculate", bubble_window)
        self.assertIn("PlayReminderBounceSequence", rig)
        self.assertIn("CreateTimer(1.0)", rig)
        self.assertEqual(2, rig.count("EmitSignal(SignalName.ReminderBouncePulse)"))

    def test_bubble_uses_approved_mint_cloud_chrome(self):
        bubble_view = self.read("src/App/Reminders/ReminderBubbleView.cs")
        bubble_window = self.read("src/App/Reminders/ReminderBubbleWindow.cs")

        for contract in (
            "ReminderTheme.MintSoft",
            "ShadowSize",
            "DrawColoredPolygon",
            "SetPlacement",
            "✦♡",
        ):
            self.assertIn(contract, bubble_view)
        self.assertIn("new Vector2I(448", bubble_window)
        self.assertIn("_view.SetPlacement(placement)", bubble_window)

    def test_tray_and_coordinator_wire_the_complete_feature(self):
        pet_root = self.read("src/App/PetRoot.cs")
        coordinator = self.read("src/App/Reminders/ReminderCoordinator.cs")

        self.assertIn('AddCheckItem("开启备忘录提醒"', pet_root)
        self.assertIn('AddItem("编辑任务列表…"', pet_root)
        self.assertIn('user://reminders.json', pet_root)
        self.assertIn("RemindersEnabled", pet_root)
        self.assertIn("ReminderBouncePulse", pet_root)
        self.assertIn("TimeZoneInfo.Local", coordinator)
        self.assertIn("ResetAfterResume", coordinator)
        self.assertIn("ReminderQueue", coordinator)
        self.assertIn("DisableAfterTrigger", coordinator)
        self.assertIn("Acknowledge", coordinator)
        self.assertIn("Stopwatch", coordinator)


if __name__ == "__main__":
    unittest.main()
