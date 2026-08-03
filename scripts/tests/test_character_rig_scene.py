import unittest
from pathlib import Path


class CharacterRigSceneTests(unittest.TestCase):
    def test_tray_exposes_harmonized_mode_and_six_scales(self):
        pet_root = (
            Path(__file__).resolve().parents[2] / "src" / "App" / "PetRoot.cs"
        ).read_text(encoding="utf-8")

        self.assertIn('AddCheckItem("和谐版"', pet_root)
        for percent in (30, 50, 75, 100, 125, 150):
            self.assertIn(f'AddRadioCheckItem("缩放 {percent}%"', pet_root)
            self.assertIn(f"MenuScale{percent}", pet_root)

    def test_runtime_switches_complete_texture_banks_without_second_scene(self):
        root = Path(__file__).resolve().parents[2]
        character_rig = (root / "src" / "Character" / "CharacterRig.cs").read_text(
            encoding="utf-8"
        )
        eye_rig = (root / "src" / "Character" / "EyeRig.cs").read_text(
            encoding="utf-8"
        )
        pet_root = (root / "src" / "App" / "PetRoot.cs").read_text(
            encoding="utf-8"
        )

        self.assertIn("SetHarmonizedMode(bool enabled)", character_rig)
        self.assertIn("layers_harmonized", character_rig)
        self.assertIn("BodyRole", character_rig)
        self.assertIn("SetHarmonizedMode(bool enabled)", eye_rig)
        self.assertIn("layers_harmonized", eye_rig)
        self.assertIn("_rig.SetHarmonizedMode(_settings.HarmonizedMode)", pet_root)

    def test_visual_diagnostics_cover_six_scales_and_both_visual_modes(self):
        root = Path(__file__).resolve().parents[2]
        capture = (root / "src" / "Diagnostics" / "VisualCapture.cs").read_text(
            encoding="utf-8"
        )
        verifier = (root / "scripts" / "verify_visual_capture.py").read_text(
            encoding="utf-8"
        )

        self.assertIn("new[] { 30, 50, 75, 100, 125, 150 }", capture)
        self.assertIn("(30, 50, 75, 100, 125, 150)", verifier)
        for name in ("harmonized_idle", "harmonized_star", "harmonized_gulp"):
            self.assertIn(f'Save("{name}")', capture)
            self.assertIn(f'"{name}.png"', verifier)

    def _scene_text(self) -> str:
        scene_path = (
            Path(__file__).resolve().parents[2]
            / "scenes"
            / "character"
            / "character_rig.tscn"
        )
        return scene_path.read_text(encoding="utf-8")

    def test_character_motion_root_uses_desk_contact_as_scale_origin(self):
        scene = self._scene_text()

        self.assertIn('[node name="StaticDesk" type="Sprite2D" parent="."]', scene)
        self.assertIn(
            '[node name="CharacterMotionRoot" type="Node2D" parent="."]',
            scene,
        )
        self.assertIn("position = Vector2(256, 419)", scene)
        self.assertIn(
            '[node name="Body" type="Sprite2D" parent="CharacterMotionRoot"]',
            scene,
        )
        self.assertIn("position = Vector2(-256, -419)", scene)
        self.assertIn("position = Vector2(-91, -127)", scene)
        self.assertIn("position = Vector2(63, -125)", scene)

    def test_scene_uses_independent_eye_nodes_without_cheek_mesh(self):
        scene = self._scene_text()

        self.assertIn(
            '[node name="EyeRig" type="Node2D" parent="CharacterMotionRoot"]',
            scene,
        )
        self.assertIn(
            '[node name="LeftEyeClip" type="Sprite2D" parent="CharacterMotionRoot/EyeRig"]',
            scene,
        )
        self.assertIn(
            '[node name="RightEyeClip" type="Sprite2D" parent="CharacterMotionRoot/EyeRig"]',
            scene,
        )
        self.assertNotIn("CheekMesh", scene)
        self.assertNotIn("cheek_patch", scene)
        self.assertNotIn("CheekMeshController", scene)
        self.assertNotIn("pet_warp.gdshader", scene)

    def test_windows_runtime_does_not_assign_a_mouse_passthrough_polygon(self):
        root = Path(__file__).resolve().parents[2]
        pet_root = (root / "src" / "App" / "PetRoot.cs").read_text(encoding="utf-8")

        self.assertNotIn("MousePassthroughPolygon", pet_root)
        self.assertNotIn("SetMousePassthroughContour", pet_root)

    def test_runtime_has_no_cheek_drag_input_or_state_path(self):
        root = Path(__file__).resolve().parents[2]
        runtime_sources = (
            root / "src" / "App" / "PetRoot.cs",
            root / "src" / "Core" / "GestureClassifier.cs",
            root / "src" / "Core" / "PetState.cs",
            root / "src" / "Core" / "PetStateMachine.cs",
            root / "src" / "Character" / "CharacterRig.cs",
        )

        for source_path in runtime_sources:
            source = source_path.read_text(encoding="utf-8")
            self.assertNotIn("CheekDrag", source, source_path.name)
            self.assertNotIn("CheekDragging", source, source_path.name)
            self.assertNotIn("SetCheekPull", source, source_path.name)
            self.assertNotIn("ReleaseCheek", source, source_path.name)

    def test_project_enables_all_native_window_transparency_prerequisites(self):
        root = Path(__file__).resolve().parents[2]
        project = (root / "project.godot").read_text(encoding="utf-8")

        self.assertIn("window/per_pixel_transparency/allowed=true", project)
        self.assertIn("window/size/transparent=true", project)
        self.assertIn("viewport/transparent_background=true", project)

    def test_project_and_runtime_use_a_fixed_scaled_virtual_canvas(self):
        root = Path(__file__).resolve().parents[2]
        project = (root / "project.godot").read_text(encoding="utf-8")
        pet_root = (root / "src" / "App" / "PetRoot.cs").read_text(
            encoding="utf-8"
        )

        self.assertIn('window/stretch/mode="canvas_items"', project)
        self.assertIn('window/stretch/aspect="keep"', project)
        self.assertIn("_window.ContentScaleSize = new Vector2I(512, 512);", pet_root)
        self.assertIn(
            "_window.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;",
            pet_root,
        )
        self.assertIn(
            "_window.ContentScaleAspect = Window.ContentScaleAspectEnum.Keep;",
            pet_root,
        )
        self.assertLess(
            pet_root.index("_window.ContentScaleSize"),
            pet_root.index("ApplySettings(placeAtDefaultWhenUnset: true)"),
        )

    def test_idle_motion_does_not_translate_character_away_from_desk(self):
        root = Path(__file__).resolve().parents[2]
        rig = (root / "src" / "Character" / "CharacterRig.cs").read_text(
            encoding="utf-8"
        )

        self.assertNotIn("var idleY", rig)
        self.assertNotIn("_characterRestPosition + new Vector2(_rejectOffsetX, idleY)", rig)


if __name__ == "__main__":
    unittest.main()
