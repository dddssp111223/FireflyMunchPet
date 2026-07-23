# Anime Desktop Pet Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a portable Windows 10/11 anime desktop pet that animates from the supplied single image and safely sends dropped files, folders, or multi-selections to the Windows Recycle Bin.

**Architecture:** A Godot .NET 4.7.1 front end owns the transparent window, character rig, animation state machine, mouse interaction, tray menu, settings, and audio. Focused C# Windows adapters own OLE drag-hover/drop, Windows Shell icons, and `IFileOperation`; pure logic lives in a .NET class library tested without launching Godot.

**Tech Stack:** Godot .NET 4.7.1, C# 12/.NET 8, Win32/OLE/Windows Shell COM, Godot shaders and 2D animation, workspace-local test runner, Windows x86_64 export.

---

## Scope and execution mode

The approved design is at `docs/superpowers/specs/2026-07-23-anime-desktop-pet-design.md`.

The user requested uninterrupted inline execution. No subagent is authorized. The `superpowers:executing-plans` skill is not available in this session, so execution must follow this checklist locally, using the available test-driven-development and verification-before-completion skills.

The plan produces one vertical product rather than separate subsystem releases because the animation, drop lifecycle, and Shell result must share one state machine to satisfy the safety invariant: a successful swallow animation may only follow a successful Shell deletion.

## File map

```text
project.godot                              Godot project and window/render settings
DesktopPet.csproj                          Godot .NET application project
DesktopPet.sln                             Solution containing app, core, and tests

src/Core/DesktopPet.Core.csproj            Pure logic library with no Godot dependency
src/Core/PetState.cs                       State and deletion outcome enums
src/Core/PetStateMachine.cs                Allowed transitions and busy-state guards
src/Core/GestureClassifier.cs              Click/cheek-drag/window-drag classification
src/Core/EyeConstraint.cs                  Elliptical iris movement clamping
src/Core/PetSettings.cs                    Persisted settings record and defaults
src/Core/SettingsJson.cs                    JSON validation and fallback
src/Core/DropBatch.cs                      Validated immutable dropped-path batch

src/App/PetRoot.cs                         Composition root and lifecycle
src/App/PetWindowController.cs             Transparent window, scaling, position, focus
src/App/PetInteractionController.cs        Mouse and external-drop routing
src/App/PetAnimationController.cs          Semantic animation orchestration
src/App/PetAudioController.cs              Short sound playback and mute
src/App/PetSettingsController.cs           Godot user:// persistence and display clamping

src/Character/CharacterRig.cs              Layer references and runtime rig parameters
src/Character/EyeRig.cs                    Independent left/right eye tracking
src/Character/CheekMeshController.cs       Screen-right cheek deformation and rebound
src/Character/IdleMotionController.cs      Blink, breathing, hair, and drool randomness

src/Windows/NativeMethods.cs               Win32/OLE declarations and constants
src/Windows/OleDropTarget.cs               DragEnter/Over/Leave/Drop bridge
src/Windows/ShellFileOperation.cs           IFileOperation recycle/delete adapter
src/Windows/ShellIconService.cs            Shell icon extraction before deletion
src/Windows/TrayController.cs              StatusIndicator menu and actions
src/Windows/WindowStyleService.cs          HWND styles, no activation, task switch hiding

scenes/pet.tscn                            Main window and rig scene
scenes/character/character_rig.tscn        Layered character scene
shaders/cheek_warp.gdshader                Localized cheek mesh deformation
shaders/soft_sway.gdshader                 Hair/drool secondary motion

assets/source/character_original.png       Untouched supplied image
assets/character/master_transparent.png    Reconstructed transparent master
assets/character/layers/*.png              Rig layers and expression variants
assets/audio/*.wav                         Click, suction, gulp, and reject sounds
assets/icons/app.ico                       Application/tray icon
assets/character/manifest.json             Layer anchors, masks, bounds, and scale

tests/DesktopPet.Tests/DesktopPet.Tests.csproj
tests/DesktopPet.Tests/Program.cs           Dependency-free test runner
tests/DesktopPet.Tests/AssertEx.cs
tests/DesktopPet.Tests/*Tests.cs            Core behavior tests
tests/DesktopPet.Integration/               Explicit disposable-file Windows tests

scripts/verify.ps1                          Core tests, Godot validation, export checks
scripts/package.ps1                         Deterministic ZIP packaging
export_presets.cfg                          Windows x86_64 export
docs/workflows/desktop-pet-production-log.md
```

## Task 1: Prepare an isolated, reproducible toolchain

**Files:**

- Modify: `.gitignore`
- Create: `.tools/README.md`
- Create: `global.json`

- [ ] **Step 1: Ignore local tool downloads and generated exports**

Add:

```gitignore
.tools/*
!.tools/README.md
.dotnet/
exports/
artifacts/
```

- [ ] **Step 2: Document the pinned local tools**

Create `.tools/README.md`:

```markdown
# Local toolchain

This directory is intentionally not committed except for this file.

- Godot .NET: 4.7.1 stable, Windows x86_64
- .NET SDK: latest servicing release in the 8.0 channel, Windows x64

The existing Steam Godot installation is not modified.
```

- [ ] **Step 3: Pin the SDK feature band**

Create `global.json`:

```json
{
  "sdk": {
    "version": "8.0.100",
    "rollForward": "latestFeature",
    "allowPrerelease": false
  }
}
```

- [ ] **Step 4: Download the workspace-local .NET 8 SDK**

Run from PowerShell after network approval:

```powershell
New-Item -ItemType Directory -Force -Path '.tools' | Out-Null
Invoke-WebRequest -UseBasicParsing `
  -Uri 'https://dot.net/v1/dotnet-install.ps1' `
  -OutFile '.tools/dotnet-install.ps1'
powershell -ExecutionPolicy Bypass -File '.tools/dotnet-install.ps1' `
  -Channel 8.0 -Architecture x64 -InstallDir '.tools/dotnet'
```

Expected: `.tools/dotnet/dotnet.exe --info` reports an 8.0 SDK.

- [ ] **Step 5: Download matching Godot .NET 4.7.1**

Run after network approval:

```powershell
Invoke-WebRequest -UseBasicParsing `
  -Uri 'https://github.com/godotengine/godot-builds/releases/download/4.7.1-stable/Godot_v4.7.1-stable_mono_win64.zip' `
  -OutFile '.tools/Godot_v4.7.1-stable_mono_win64.zip'
Expand-Archive -LiteralPath '.tools/Godot_v4.7.1-stable_mono_win64.zip' `
  -DestinationPath '.tools/godot-dotnet' -Force
```

Expected: the extracted directory contains the Godot .NET editor executable and `GodotSharp`.

- [ ] **Step 6: Verify without touching the Steam installation**

Run:

```powershell
& '.tools/dotnet/dotnet.exe' --info
Get-ChildItem -LiteralPath '.tools/godot-dotnet' -Recurse -Filter '*mono*win64.exe'
```

Expected: both commands succeed; `F:\SteamLibrary\steamapps\common\Godot Engine` remains unchanged.

- [ ] **Step 7: Commit reproducibility metadata**

```powershell
git add .gitignore .tools/README.md global.json
git commit -m "build: pin desktop pet toolchain"
```

## Task 2: Scaffold the Godot app and dependency-free test runner

**Files:**

- Create: `project.godot`
- Create: `DesktopPet.csproj`
- Create: `DesktopPet.sln`
- Create: `src/Core/DesktopPet.Core.csproj`
- Create: `tests/DesktopPet.Tests/DesktopPet.Tests.csproj`
- Create: `tests/DesktopPet.Tests/AssertEx.cs`
- Create: `tests/DesktopPet.Tests/Program.cs`
- Create: `scenes/pet.tscn`
- Create: `src/App/PetRoot.cs`

- [ ] **Step 1: Create the pure core project**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>12</LangVersion>
  </PropertyGroup>
</Project>
```

- [ ] **Step 2: Create the Godot project**

`DesktopPet.csproj`:

```xml
<Project Sdk="Godot.NET.Sdk/4.7.1">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <EnableDynamicLoading>true</EnableDynamicLoading>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="src/Core/DesktopPet.Core.csproj" />
  </ItemGroup>
</Project>
```

`project.godot`:

```ini
[application]
config/name="MunchPet"
run/main_scene="res://scenes/pet.tscn"

[display]
window/size/viewport_width=512
window/size/viewport_height=512
window/size/window_width_override=512
window/size/window_height_override=512
window/size/borderless=true
window/per_pixel_transparency/allowed=true

[rendering]
renderer/rendering_method="gl_compatibility"
renderer/rendering_method.mobile="gl_compatibility"
environment/defaults/default_clear_color=Color(0, 0, 0, 0)
```

- [ ] **Step 3: Create a zero-package console test harness**

`tests/DesktopPet.Tests/DesktopPet.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../../src/Core/DesktopPet.Core.csproj" />
  </ItemGroup>
</Project>
```

`AssertEx.cs`:

```csharp
namespace DesktopPet.Tests;

internal static class AssertEx
{
    public static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"{name}: expected {expected}, got {actual}");
    }

    public static void True(bool value, string name)
    {
        if (!value) throw new InvalidOperationException($"{name}: expected true");
    }
}
```

`Program.cs`:

```csharp
using DesktopPet.Tests;

var suites = Array.Empty<Action>();

var failed = 0;
foreach (var suite in suites)
{
    try { suite(); }
    catch (Exception ex) { failed++; Console.Error.WriteLine(ex); }
}

Console.WriteLine($"{suites.Length - failed}/{suites.Length} suites passed");
return failed == 0 ? 0 : 1;
```

- [ ] **Step 4: Create a transparent-window smoke scene**

`PetRoot.cs`:

```csharp
using Godot;

namespace DesktopPet.App;

public partial class PetRoot : Node2D
{
    public override void _Ready()
    {
        var window = GetWindow();
        window.TransparentBg = true;
        window.Borderless = true;
        window.AlwaysOnTop = false;
    }
}
```

`scenes/pet.tscn`:

```ini
[gd_scene load_steps=2 format=3]

[ext_resource path="res://src/App/PetRoot.cs" type="Script" id="1"]

[node name="PetRoot" type="Node2D"]
script = ExtResource("1")
```

- [ ] **Step 5: Build and open headlessly**

Run:

```powershell
& '.tools/dotnet/dotnet.exe' build DesktopPet.csproj
$godotExe = (Get-ChildItem -LiteralPath '.tools/godot-dotnet' -Recurse `
  -Filter 'Godot_v4.7.1-stable_mono_win64.exe' | Select-Object -First 1).FullName
& $godotExe --headless --path . --editor --quit
```

Expected: build succeeds and Godot reports no parse/resource errors.

- [ ] **Step 6: Commit the scaffold**

```powershell
git add project.godot DesktopPet.csproj DesktopPet.sln src tests scenes
git commit -m "build: scaffold Godot desktop pet"
```

## Task 3: Implement the safety-critical state machine first

**Files:**

- Create: `src/Core/PetState.cs`
- Create: `src/Core/PetStateMachine.cs`
- Create: `tests/DesktopPet.Tests/PetStateMachineTests.cs`

- [ ] **Step 1: Write failing state-transition tests**

```csharp
using DesktopPet.Core;

namespace DesktopPet.Tests;

internal static class PetStateMachineTests
{
    public static void Run()
    {
        var machine = new PetStateMachine();
        AssertEx.True(machine.EnterFileHover(), "idle -> file hover");
        AssertEx.True(machine.BeginShellPending(), "hover -> pending");
        AssertEx.Equal(PetState.Rejecting, machine.ResolveDelete(DeleteOutcome.Failed), "failed rejects");
        machine.FinishTransient();
        AssertEx.Equal(PetState.Idle, machine.State, "reject -> idle");

        machine.EnterFileHover();
        machine.BeginShellPending();
        AssertEx.Equal(PetState.Swallowing, machine.ResolveDelete(DeleteOutcome.Succeeded), "success swallows");
        AssertEx.True(!machine.EnterFileHover(), "busy rejects another feed");

        machine.FinishTransient();
        machine.EnterFileHover();
        machine.BeginShellPending();
        AssertEx.Equal(PetState.Idle, machine.ResolveDelete(DeleteOutcome.Cancelled), "cancel returns idle");
    }
}
```

Update `tests/DesktopPet.Tests/Program.cs` to:

```csharp
using DesktopPet.Tests;

var suites = new Action[] { PetStateMachineTests.Run };
var failed = 0;
foreach (var suite in suites)
{
    try { suite(); }
    catch (Exception ex) { failed++; Console.Error.WriteLine(ex); }
}
Console.WriteLine($"{suites.Length - failed}/{suites.Length} suites passed");
return failed == 0 ? 0 : 1;
```

- [ ] **Step 2: Run the test and verify compilation fails**

```powershell
& '.tools/dotnet/dotnet.exe' run --project tests/DesktopPet.Tests
```

Expected: FAIL because `PetStateMachine` is undefined.

- [ ] **Step 3: Implement the minimal state model**

```csharp
namespace DesktopPet.Core;

public enum PetState
{
    Idle, FileHover, ShellPending, Swallowing,
    ClickBounce, CheekDragging, WindowDragging, Rejecting
}

public enum DeleteOutcome { Succeeded, Cancelled, Failed }

public sealed class PetStateMachine
{
    public PetState State { get; private set; } = PetState.Idle;
    public bool IsBusy => State is PetState.ShellPending or PetState.Swallowing;

    public bool EnterFileHover() => Transition(PetState.Idle, PetState.FileHover);
    public bool LeaveFileHover() => Transition(PetState.FileHover, PetState.Idle);
    public bool BeginShellPending() => Transition(PetState.FileHover, PetState.ShellPending);
    public bool BeginClickBounce() => Transition(PetState.Idle, PetState.ClickBounce);
    public bool BeginCheekDrag() => Transition(PetState.Idle, PetState.CheekDragging);
    public bool BeginWindowDrag() => Transition(PetState.Idle, PetState.WindowDragging);

    public PetState ResolveDelete(DeleteOutcome outcome)
    {
        if (State != PetState.ShellPending) return State;
        State = outcome switch
        {
            DeleteOutcome.Succeeded => PetState.Swallowing,
            DeleteOutcome.Failed => PetState.Rejecting,
            _ => PetState.Idle
        };
        return State;
    }

    public void FinishTransient()
    {
        if (State is PetState.Swallowing or PetState.ClickBounce or
            PetState.CheekDragging or PetState.WindowDragging or PetState.Rejecting)
            State = PetState.Idle;
    }

    private bool Transition(PetState expected, PetState next)
    {
        if (State != expected) return false;
        State = next;
        return true;
    }
}
```

- [ ] **Step 4: Run tests**

Expected: `1/1 suites passed`.

- [ ] **Step 5: Commit**

```powershell
git add src/Core tests/DesktopPet.Tests
git commit -m "feat: add pet interaction state machine"
```

## Task 4: Add gesture classification, eye limits, drop validation, and settings

**Files:**

- Create: `src/Core/GestureClassifier.cs`
- Create: `src/Core/EyeConstraint.cs`
- Create: `src/Core/DropBatch.cs`
- Create: `src/Core/PetSettings.cs`
- Create: `src/Core/SettingsJson.cs`
- Create: `tests/DesktopPet.Tests/GestureClassifierTests.cs`
- Create: `tests/DesktopPet.Tests/EyeConstraintTests.cs`
- Create: `tests/DesktopPet.Tests/DropBatchTests.cs`
- Create: `tests/DesktopPet.Tests/SettingsJsonTests.cs`

- [ ] **Step 1: Write gesture and eye tests**

```csharp
using System.Numerics;
using DesktopPet.Core;

namespace DesktopPet.Tests;

internal static class GestureClassifierTests
{
    public static void Run()
    {
        AssertEx.Equal(GestureKind.Click,
            GestureClassifier.Classify(HitRegion.Cheek, Vector2.Zero, new Vector2(2, 1), 8), "short cheek click");
        AssertEx.Equal(GestureKind.CheekDrag,
            GestureClassifier.Classify(HitRegion.Cheek, Vector2.Zero, new Vector2(12, 0), 8), "cheek drag");
        AssertEx.Equal(GestureKind.WindowDrag,
            GestureClassifier.Classify(HitRegion.MoveHandle, Vector2.Zero, new Vector2(0, 12), 8), "window drag");
    }
}

internal static class EyeConstraintTests
{
    public static void Run()
    {
        var clamped = EyeConstraint.Clamp(new Vector2(20, 20), new Vector2(7, 5));
        var ellipse = clamped.X * clamped.X / 49f + clamped.Y * clamped.Y / 25f;
        AssertEx.True(ellipse <= 1.0001f, "iris remains inside ellipse");
        AssertEx.Equal(Vector2.Zero, EyeConstraint.Clamp(Vector2.Zero, new Vector2(7, 5)), "center");
    }
}
```

- [ ] **Step 2: Run and confirm failure**

Expected: missing classifier and constraint types.

- [ ] **Step 3: Implement the pure helpers**

```csharp
using System.Numerics;

namespace DesktopPet.Core;

public enum HitRegion { Visible, Cheek, MoveHandle }
public enum GestureKind { Click, CheekDrag, WindowDrag }

public static class GestureClassifier
{
    public static GestureKind Classify(HitRegion region, Vector2 down, Vector2 current, float threshold)
    {
        if (Vector2.Distance(down, current) < threshold) return GestureKind.Click;
        return region == HitRegion.Cheek ? GestureKind.CheekDrag :
               region == HitRegion.MoveHandle ? GestureKind.WindowDrag :
               GestureKind.Click;
    }
}

public static class EyeConstraint
{
    public static Vector2 Clamp(Vector2 desired, Vector2 radii)
    {
        if (radii.X <= 0 || radii.Y <= 0) return Vector2.Zero;
        var q = desired.X * desired.X / (radii.X * radii.X) +
                desired.Y * desired.Y / (radii.Y * radii.Y);
        return q <= 1f ? desired : desired / MathF.Sqrt(q);
    }
}
```

- [ ] **Step 4: Add validated batch and JSON settings tests**

```csharp
internal static class DropBatchTests
{
    public static void Run()
    {
        var batch = DropBatch.Create(new[] { @"C:\a.txt", @"C:\a.txt", @"C:\b" }, 10, 20);
        AssertEx.Equal(2, batch.Paths.Count, "deduplicates paths");
        AssertEx.Equal(10, batch.DropX, "keeps drop x");
    }
}

internal static class SettingsJsonTests
{
    public static void Run()
    {
        var defaults = PetSettings.Default;
        var restored = SettingsJson.Deserialize(SettingsJson.Serialize(defaults));
        AssertEx.Equal(defaults, restored, "round trip");
        AssertEx.Equal(defaults, SettingsJson.Deserialize("{broken"), "corrupt fallback");
        AssertEx.Equal(100, SettingsJson.Deserialize("""{"scalePercent":999}""").ScalePercent, "invalid scale");
    }
}
```

Update the suite list in `tests/DesktopPet.Tests/Program.cs` to:

```csharp
var suites = new Action[]
{
    PetStateMachineTests.Run,
    GestureClassifierTests.Run,
    EyeConstraintTests.Run,
    SettingsJsonTests.Run,
    DropBatchTests.Run
};
```

- [ ] **Step 5: Implement immutable batch and validated settings**

```csharp
namespace DesktopPet.Core;

public sealed record DropBatch(IReadOnlyList<string> Paths, int DropX, int DropY)
{
    public static DropBatch Create(IEnumerable<string> paths, int x, int y)
    {
        var normalized = paths.Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (normalized.Length == 0) throw new ArgumentException("No file-system paths.", nameof(paths));
        return new DropBatch(normalized, x, y);
    }
}

public sealed record PetSettings(
    int ScalePercent, bool AlwaysOnTop, bool Muted,
    int X, int Y, string MonitorId)
{
    public static PetSettings Default => new(100, false, false, -1, -1, "");
}
```

Use this complete `SettingsJson` implementation:

```csharp
using System.Text.Json;

namespace DesktopPet.Core;

public static class SettingsJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string Serialize(PetSettings value) =>
        JsonSerializer.Serialize(value, Options);

    public static PetSettings Deserialize(string json)
    {
        try
        {
            var value = JsonSerializer.Deserialize<PetSettings>(json, Options);
            if (value is null) return PetSettings.Default;
            var allowed = value.ScalePercent is 75 or 100 or 125 or 150;
            return value with
            {
                ScalePercent = allowed ? value.ScalePercent : 100,
                MonitorId = value.MonitorId ?? ""
            };
        }
        catch (JsonException)
        {
            return PetSettings.Default;
        }
    }
}
```

- [ ] **Step 6: Run all core suites**

```powershell
& '.tools/dotnet/dotnet.exe' run --project tests/DesktopPet.Tests
```

Expected: `5/5 suites passed`.

- [ ] **Step 7: Commit**

```powershell
git add src/Core tests/DesktopPet.Tests
git commit -m "feat: add gestures eye limits and settings core"
```

## Task 5: Produce and verify the non-destructive character asset pack

**Files:**

- Create: `assets/source/character_original.png`
- Create: `assets/character/master_transparent.png`
- Create: `assets/character/layers/*.png`
- Create: `assets/character/manifest.json`
- Create: `docs/workflows/desktop-pet-production-log.md`

- [ ] **Step 1: Copy the source without modification**

Copy `01978-3283962926.png` to `assets/source/character_original.png`. Record SHA-256 of both files and require equality.

- [ ] **Step 2: Use the image-generation editing workflow**

Use the source image as the sole reference and request:

```text
Preserve the exact character design, line art, colors, face proportions, pale-blue desk edge,
and drool puddle. Remove only the pure white surrounding background and output a transparent
high-resolution PNG. Conservatively reconstruct edges hidden by the former white background.
Do not redesign clothing, eyes, hair ornaments, mouth, or desk.
```

Generate at a working size of at least 1536×1536 and visually compare it with the source.

- [ ] **Step 3: Generate expression variants from the approved master**

Create consistent transparent variants for:

- neutral open eyes
- normal closed eyes
- `><` closed eyes
- enlarged star eyes
- original open mouth
- anticipatory open mouth
- maximum gulp mouth
- closed swallow mouth

Each edit prompt must say to change only the named expression and preserve all other pixels and proportions.

- [ ] **Step 4: Create rig layers and restore hidden regions**

Export separate transparent PNGs matching the manifest slots:

```json
{
  "canvas": [1536, 1536],
  "baseScale": 3.0,
  "eyeMaxOffsetSourcePx": [7, 5],
  "mouthAnchorSourcePx": [240, 372],
  "cheekRegionSourcePx": [330, 260, 165, 155],
  "moveRegionSourcePx": [85, 35, 340, 150],
  "layers": {
    "desk": {"file": "layers/desk.png", "z": 0},
    "backHair": {"file": "layers/back_hair.png", "z": 10},
    "face": {"file": "layers/face.png", "z": 20},
    "leftEyeWhite": {"file": "layers/left_eye_white.png", "z": 30},
    "leftIris": {"file": "layers/left_iris.png", "z": 31},
    "rightEyeWhite": {"file": "layers/right_eye_white.png", "z": 30},
    "rightIris": {"file": "layers/right_iris.png", "z": 31},
    "eyesClosed": {"file": "layers/eyes_closed.png", "z": 35},
    "eyesStar": {"file": "layers/eyes_star.png", "z": 35},
    "eyesGreaterLess": {"file": "layers/eyes_greater_less.png", "z": 35},
    "mouthOriginal": {"file": "layers/mouth_original.png", "z": 40},
    "mouthAnticipation": {"file": "layers/mouth_anticipation.png", "z": 40},
    "mouthMaximum": {"file": "layers/mouth_maximum.png", "z": 40},
    "mouthClosed": {"file": "layers/mouth_closed.png", "z": 40},
    "frontHair": {"file": "layers/front_hair.png", "z": 50},
    "droolStream": {"file": "layers/drool_stream.png", "z": 60},
    "droolPuddle": {"file": "layers/drool_puddle.png", "z": 5}
  }
}
```

Populate `layers` with each produced filename, anchor, z-index, and visible bounds.

- [ ] **Step 5: Inspect every raster result**

Check:

- transparent pixels around the character and retained desk
- no missing line art at image edges
- no invented accessories
- identical face proportions across expressions
- no white fringe
- drool and desk puddle retained

Reject and regenerate any inconsistent variant before rigging.

- [ ] **Step 6: Record the exact prompts and accepted outputs**

Append source hash, prompts, output filenames, rejection reasons, and final selection to `docs/workflows/desktop-pet-production-log.md`.

- [ ] **Step 7: Commit approved assets**

```powershell
git add assets docs/workflows/desktop-pet-production-log.md
git commit -m "art: add layered desktop pet character"
```

## Task 6: Build the character rig and idle animation

**Files:**

- Create: `scenes/character/character_rig.tscn`
- Create: `src/Character/CharacterRig.cs`
- Create: `src/Character/EyeRig.cs`
- Create: `src/Character/IdleMotionController.cs`
- Create: `shaders/soft_sway.gdshader`
- Modify: `scenes/pet.tscn`

- [ ] **Step 1: Build the scene tree**

Use named nodes:

```text
CharacterRig
  Desk
  BackHair
  Face
  LeftEye/White/Iris/OpenLid/ClosedLid/Star/GreaterLess
  RightEye/White/Iris/OpenLid/ClosedLid/Star/GreaterLess
  Mouth/Original/Anticipation/Maximum/Closed
  FrontHair
  DroolStream
  DroolPuddle
  FileIconLayer
```

- [ ] **Step 2: Implement constrained eye targets**

`EyeRig.SetTarget(Vector2 localTarget)` must convert target to each eye's local space, call the pure `EyeConstraint.Clamp`, and smooth with:

```csharp
_velocity += (target - _offset) * stiffness * delta;
_velocity *= MathF.Exp(-damping * delta);
_offset += _velocity * delta;
```

Pause and center the irises when blink, star, or `><` layers are active.

- [ ] **Step 3: Implement randomized idle timers**

Use independent random ranges:

- blink interval: 2.5–6.5 seconds
- double-blink chance: 12%
- breathing period: 3.8–5.5 seconds
- hair sway periods: 4.2–7.0 seconds with distinct phases
- drool sway period: 5.0–8.0 seconds
- puddle ripple interval: 4.0–9.0 seconds

- [ ] **Step 4: Add semantic rig methods**

```csharp
public void SetFileHover(bool active);
public void SetEyeTrackingEnabled(bool enabled);
public void PlayBlink();
public void PlayClickBounce();
public void SetCheekPull(Vector2 displacement);
public void ReleaseCheek();
public void PlaySwallow(Texture2D icon, Vector2 dropPoint);
public void PlayReject();
```

Every one-shot animation must emit one completion signal used by `PetStateController`.

- [ ] **Step 5: Validate visually at all four scales**

Run the scene and check 75%, 100%, 125%, and 150%. Capture screenshots for the workflow log.

- [ ] **Step 6: Commit**

```powershell
git add scenes src/Character shaders docs/workflows
git commit -m "feat: rig character and idle motion"
```

## Task 7: Implement click bounce, cheek pull, and free window movement

**Files:**

- Create: `src/App/PetInteractionController.cs`
- Create: `src/Character/CheekMeshController.cs`
- Create: `shaders/cheek_warp.gdshader`
- Create: `src/App/PetWindowController.cs`

- [ ] **Step 1: Route pointer input through the pure classifier**

On press, store the local point and hit region. On motion, classify only after the 8-source-pixel threshold scaled by the active zoom.

- [ ] **Step 2: Implement all-visible click bounce**

Animate:

```text
scale (1.00, 1.00)
-> 70 ms (1.08, 0.86)
-> 95 ms (0.96, 1.08)
-> 145 ms (1.00, 1.00)
```

Use cubic easing and play `click_pop.wav` once.

- [ ] **Step 3: Implement screen-right cheek deformation**

Normalize pointer displacement by current scale, clamp magnitude, pass to shader uniforms:

```glsl
uniform vec2 pull = vec2(0.0);
uniform vec2 center = vec2(0.72, 0.62);
uniform float radius = 0.28;

void vertex() {
    float d = distance(UV, center);
    float w = smoothstep(radius, 0.0, d);
    VERTEX += pull * w * w;
}
```

Release with a critically damped spring and one small overshoot.

- [ ] **Step 4: Move the window from the upper-hair region**

Use screen coordinates and preserve the pointer offset. Clamp the resulting window rectangle so at least the full visible pet remains on a current monitor work area.

- [ ] **Step 5: Verify gesture conflicts**

Manual cases:

- click cheek without motion → Q bounce
- drag cheek → no click sound
- click upper hair without motion → Q bounce
- drag upper hair → window moves
- click desk edge → Q bounce

- [ ] **Step 6: Commit**

```powershell
git add src/App src/Character shaders
git commit -m "feat: add pet click pull and movement"
```

## Task 8: Add Windows tray, settings persistence, and window styles

**Files:**

- Create: `src/Windows/TrayController.cs`
- Create: `src/Windows/WindowStyleService.cs`
- Create: `src/App/PetSettingsController.cs`
- Modify: `src/App/PetRoot.cs`
- Create: `assets/icons/app.ico`

- [ ] **Step 1: Apply non-activating desktop-pet styles**

Use the Godot window handle and update extended styles without overwriting unrelated bits:

```csharp
var style = GetWindowLongPtr(hwnd, GwlExStyle);
style |= WsExToolWindow | WsExNoActivate;
style &= ~WsExAppWindow;
SetWindowLongPtr(hwnd, GwlExStyle, style);
```

Do not set `WS_EX_TRANSPARENT` globally because visible pixels must accept input.

- [ ] **Step 2: Configure the Godot StatusIndicator menu**

Create checked menu items for:

- always on top
- 75%, 100%, 125%, 150%
- mute
- reset to lower right
- exit

Only one scale item may be checked.

- [ ] **Step 3: Persist settings**

Store JSON under `user://settings.json` after a 250 ms debounce. On load, call the pure validator and clamp the saved rectangle to current monitor work areas.

- [ ] **Step 4: Confirm defaults**

Fresh profile must produce:

```json
{
  "scalePercent": 100,
  "alwaysOnTop": false,
  "muted": false,
  "x": -1,
  "y": -1,
  "monitorId": ""
}
```

- [ ] **Step 5: Verify lifecycle**

Confirm no taskbar button, no Alt+Tab entry, no keyboard focus steal, tray exit removes the icon, and relaunch restores all four settings.

- [ ] **Step 6: Commit**

```powershell
git add src/App src/Windows assets/icons
git commit -m "feat: add tray settings and desktop window behavior"
```

## Task 9: Implement native OLE file hover and drop

**Files:**

- Create: `src/Windows/NativeMethods.cs`
- Create: `src/Windows/OleDropTarget.cs`
- Modify: `src/App/PetInteractionController.cs`

- [ ] **Step 1: Define the COM drop-target contract**

Use `System.Runtime.InteropServices.ComTypes.IDataObject` and define:

```csharp
[ComVisible(true)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("00000122-0000-0000-C000-000000000046")]
internal interface IDropTargetNative
{
    [PreserveSig] int DragEnter(IDataObject data, uint keys, PointL point, ref uint effect);
    [PreserveSig] int DragOver(uint keys, PointL point, ref uint effect);
    [PreserveSig] int DragLeave();
    [PreserveSig] int Drop(IDataObject data, uint keys, PointL point, ref uint effect);
}
```

Declare `OleInitialize`, `RegisterDragDrop`, `RevokeDragDrop`, and `ReleaseStgMedium`.

- [ ] **Step 2: Extract only `CF_HDROP` file lists**

Query `DataFormats.FileDrop`, copy the returned `string[]`, normalize through `DropBatch.Create`, and release COM storage in `finally`.

- [ ] **Step 3: Map lifecycle to semantic events**

- `DragEnter` with valid paths → `EnterFileHover`
- `DragOver` → update cached screen position
- `DragLeave` → `LeaveFileHover`
- `Drop` → submit immutable `DropBatch`
- busy state → return `DROPEFFECT_NONE`
- accepted state → return `DROPEFFECT_MOVE`

- [ ] **Step 4: Register and revoke exactly once**

Initialize on `_Ready` after the native window exists. Revoke and release the COM callable wrapper on `_ExitTree`.

- [ ] **Step 5: Verify Explorer behavior**

Drag a disposable file over all visible regions, transparent corners, then out of the window. Confirm star eyes enter and leave correctly without deleting until drop.

- [ ] **Step 6: Commit**

```powershell
git add src/Windows src/App
git commit -m "feat: add native file hover and drop"
```

## Task 10: Implement Windows Shell icons and safe Recycle Bin deletion

**Files:**

- Create: `src/Windows/ShellFileOperation.cs`
- Create: `src/Windows/ShellIconService.cs`
- Create: `tests/DesktopPet.Integration/DesktopPet.Integration.csproj`
- Create: `tests/DesktopPet.Integration/Program.cs`

- [ ] **Step 1: Define the deletion result boundary**

```csharp
public sealed record ShellDeleteResult(DeleteOutcome Outcome, int HResult, string? Message);

public interface IShellDeleteService
{
    Task<ShellDeleteResult> DeleteAsync(DropBatch batch, CancellationToken cancellationToken);
}
```

- [ ] **Step 2: Implement `IFileOperation` with explicit flags**

Create one `IShellItem` per path, add all items to one operation, then use flags equivalent to:

```csharp
const uint FOF_SILENT = 0x0004;
const uint FOF_NOCONFIRMATION = 0x0010;
const uint FOF_WANTNUKEWARNING = 0x4000;
const uint FOFX_RECYCLEONDELETE = 0x00080000;
const uint FOFX_ADDUNDORECORD = 0x20000000;
```

Call `PerformOperations`, then `GetAnyOperationsAborted`. Map:

- successful and not aborted → `Succeeded`
- aborted → `Cancelled`
- failing HRESULT → `Failed`

COM objects must be released in reverse order in `finally`.

- [ ] **Step 3: Cache Shell icons before deleting**

Use `SHGetFileInfo` with `SHGFI_ICON | SHGFI_LARGEICON`, convert the returned `HICON` to PNG bytes, and always call `DestroyIcon`.

For multi-selection, render up to three cached icons as a small stack. Do not render text or a count.

- [ ] **Step 4: Create an opt-in disposable integration runner**

The runner must:

1. create a unique directory under the process temp directory
2. resolve and assert that every test target is a descendant of that exact directory
3. create files itself
4. require `--run-recycle-tests`
5. call the Shell adapter
6. never test permanent deletion automatically

Without the flag it prints `SKIPPED: explicit disposable recycle test flag required` and exits 0.

- [ ] **Step 5: Verify normal, cancel, and failure behavior manually**

Use only generated test data. Confirm successful items can be restored from Recycle Bin. Use a locked test file for failure. Use a disposable removable-drive item only if available for the permanent-delete warning; cancelling must preserve it.

- [ ] **Step 6: Commit**

```powershell
git add src/Windows tests/DesktopPet.Integration
git commit -m "feat: recycle dropped items through Windows Shell"
```

## Task 11: Choreograph feeding, rejection, and audio

**Files:**

- Modify: `src/App/PetRoot.cs`
- Modify: `src/App/PetAnimationController.cs`
- Create: `src/App/PetAudioController.cs`
- Create: `assets/audio/click_pop.wav`
- Create: `assets/audio/suction.wav`
- Create: `assets/audio/gulp.wav`
- Create: `assets/audio/reject.wav`

- [ ] **Step 1: Connect the drop transaction**

Use this order:

```csharp
rig.SetFileHover(true);
var icon = await iconService.GetStackAsync(batch);
state.BeginShellPending();
var result = await deleteService.DeleteAsync(batch, cancellationToken);
switch (state.ResolveDelete(result.Outcome))
{
    case PetState.Swallowing: await animation.PlaySwallowAsync(icon, batch); break;
    case PetState.Rejecting: await animation.PlayRejectAsync(result); break;
}
state.FinishTransient();
```

- [ ] **Step 2: Implement the 0.6–0.8 second swallow timeline**

```text
0–120 ms    cached icon flies from drop point and scales to 45%
80–230 ms   mouth switches to maximum open; head leans forward
220–310 ms  icon reaches mouth and disappears; play suction
300–430 ms  mouth closes; eyes switch to ><; play gulp
360–560 ms  face/head compress downward
540–760 ms  body rebounds; hair and drool lag
760–800 ms  return to idle and resume eye tracking
```

- [ ] **Step 3: Implement reject behavior**

Do not hide the original file icon. Pull back 4–6 source pixels, shake once, play `reject.wav`, then show the Windows/system error already returned by the Shell layer.

- [ ] **Step 4: Create or synthesize short original sounds**

Keep each file under one second, normalize peaks below clipping, and document source/creation in the production log. The mute setting must gate all four audio streams.

- [ ] **Step 5: Verify no text in successful flow**

Search scenes and code for toasts, labels, notifications, and successful message boxes. The only dialogs allowed are permanent-delete warnings and actual failure errors.

- [ ] **Step 6: Commit**

```powershell
git add src/App assets/audio docs/workflows
git commit -m "feat: choreograph file feeding and sounds"
```

## Task 12: Add end-to-end verification and deterministic packaging

**Files:**

- Create: `scripts/verify.ps1`
- Create: `scripts/package.ps1`
- Create: `export_presets.cfg`
- Create: `README.md`

- [ ] **Step 1: Write the verification script**

It must run:

```powershell
& '.tools/dotnet/dotnet.exe' run --project tests/DesktopPet.Tests
$godotExe = (Get-ChildItem -LiteralPath '.tools/godot-dotnet' -Recurse `
  -Filter 'Godot_v4.7.1-stable_mono_win64.exe' | Select-Object -First 1).FullName
& $godotExe --headless --path . --editor --quit
& $godotExe --headless --path . --quit-after 3
git diff --check
```

The script exits on the first non-zero code and never invokes destructive integration tests.

- [ ] **Step 2: Configure Windows export**

Create a Windows Desktop preset for x86_64, release mode, embedded PCK if supported, application icon `assets/icons/app.ico`, and output `exports/MunchPet/MunchPet.exe`.

- [ ] **Step 3: Write deterministic packaging**

`scripts/package.ps1` must:

1. remove only the verified `artifacts/MunchPet-win-x64` staging directory
2. copy the fresh export into that staging directory
3. include `README.txt` with launch, tray, and Recycle Bin behavior
4. create `artifacts/MunchPet-win-x64.zip`
5. print SHA-256 and archive size

- [ ] **Step 4: Run the full non-destructive verification**

Expected:

- `5/5 suites passed`
- Godot editor and runtime checks exit 0
- `git diff --check` produces no output

- [ ] **Step 5: Run the disposable Recycle Bin integration test**

Run only after verifying the generated temp root:

```powershell
& '.tools/dotnet/dotnet.exe' run `
  --project tests/DesktopPet.Integration -- --run-recycle-tests
```

Expected: generated files leave their temp directory and appear in Recycle Bin.

- [ ] **Step 6: Export and package**

Run:

```powershell
$godotExe = (Get-ChildItem -LiteralPath '.tools/godot-dotnet' -Recurse `
  -Filter 'Godot_v4.7.1-stable_mono_win64.exe' | Select-Object -First 1).FullName
& $godotExe --headless --path . --export-release 'Windows Desktop'
powershell -ExecutionPolicy Bypass -File scripts/package.ps1
```

- [ ] **Step 7: Manual Windows acceptance pass**

Check every case in design section 13 and record results in `docs/workflows/desktop-pet-production-log.md`.

- [ ] **Step 8: Commit the release candidate**

```powershell
git add scripts export_presets.cfg README.md docs/workflows
git commit -m "build: package desktop pet release candidate"
```

Do not commit exported binaries unless the user explicitly requests binary versioning.

## Task 13: User modification and final acceptance loop

**Files:**

- Modify: files implicated by user feedback
- Modify: `docs/workflows/desktop-pet-production-log.md`

- [ ] **Step 1: Deliver the candidate ZIP and a concise test checklist**

Include archive path, SHA-256, supported Windows versions, and the exact safe test procedure.

- [ ] **Step 2: Record each user modification request**

For every request, add:

```markdown
### Change request YYYY-MM-DD-N

- Requested:
- Affected behavior/assets:
- Acceptance check:
- Result:
```

- [ ] **Step 3: Apply each change test-first where logic is involved**

Add or update the exact failing test, verify failure, implement, verify pass, and rerun `scripts/verify.ps1`.

- [ ] **Step 4: Re-export after every accepted batch**

Never label a build final until the user explicitly says final acceptance has passed.

## Task 14: Create the reusable production Skill after final acceptance

**Prerequisite:** The user has explicitly approved the final desktop-pet build.

**Files:**

- Create or update: a personal Codex Skill using the available skill-creator workflow
- Finalize: `docs/workflows/desktop-pet-production-log.md`

- [ ] **Step 1: Invoke the skill-creator skill**

Use the final production log and repository as the only source of verified workflow truth.

- [ ] **Step 2: Encode the reusable inputs**

The Skill must request or detect:

- source character image
- intended private/public use and asset rights
- Windows target
- desired expression overrides
- optional sounds and app name

- [ ] **Step 3: Encode the end-to-end stages**

Include toolchain bootstrap, image inspection, transparent master, expression/layer production, rig configuration, Windows integration, disposable deletion tests, export, and acceptance.

- [ ] **Step 4: Add hard safety rules**

The Skill must forbid deletion tests against existing user files and require an internally created, path-validated temporary root.

- [ ] **Step 5: Validate on a second character**

Run the Skill against a different same-type source image or a dry-run fixture. Fix any assumptions tied to the first character before considering the Skill complete.

- [ ] **Step 6: Deliver the Skill and workflow record**

Provide the installed Skill location, usage example, validation evidence, and the finalized production log.
