# Firefly MunchPet — 流萤桌面文件吞噬宠

把文件、文件夹或多选项目拖到流萤身上，她会做出期待表情并“啊呜”一口吞掉；
文件实际通过 Windows Shell 移入回收站，可按系统规则恢复。

适用于 Windows 10/11 x64，提供免安装压缩包。

## 下载

前往 [Releases](../../releases) 下载最新
`FireflyMunchPet-v7-win-x64.zip`，解压后双击 `MunchPet.exe`。
无需另外安装 Godot 或 .NET。

首次测试建议使用刚创建的可丢弃文件。成功删除不显示自定义文字；文件过大、
无法进入回收站或其他失败情况由 Windows 显示系统提示。

## 主要功能

- **文件投喂**：全角色区域接受文件、文件夹和多选拖放；悬停时显示固定星星眼，
  松手后播放文件飞入口中、张嘴咬下、`><` 闭眼吞咽和回弹动画。
- **独立眼球跟随**：左右虹膜是独立图层，仅在各自眼眶内有限移动，不会拖动
  睫毛、嘴部或露出整片眼白。
- **待机动画**：随机眨眼、轻微呼吸、头发摆动和克制的口水晃动。
- **单击 Q 弹**：点击任意可见区域会以画面底部为锚点向下压缩并回弹，带音效。
- **自由移动**：按住角色上部头发区域拖动，可放置到桌面任意位置。
- **透明窗口**：外围背景透明，不使用白底；空白区域不会遮挡桌面。
- **和谐版**：托盘菜单可切换去除嘴部口水与桌面水迹的素材组，设置会持久化。
- **六档缩放**：30%/50%/75%/100%/125%/150%，不会裁切角色或错误缩小画布。
- **置顶与静音**：默认不置顶；均可在通知区域菜单切换并保存。

## 提醒备忘录

提醒功能默认关闭，可从通知区域菜单选择“开启备忘录提醒”或“编辑任务列表…”。

- 最多 5 项提醒，每项最多 200 个字符。
- 支持定时提醒或倒数提醒。
- 支持一次性提醒或按相同周期循环。
- 每项均可启用、停用、编辑或删除。
- 默认包含一个可编辑/删除的 40 分钟循环运动提醒。
- 提醒触发时显示薄荷绿透明气泡，并以 1 秒间隔执行两次 Q 弹和音效。
- 提醒编辑器是独立窗口；气泡会根据屏幕工作区自动选择角色上、下、左、右位置，
  不会被桌宠画布裁切。

提醒数据保存在：

```text
%APPDATA%\Godot\app_userdata\MunchPet\reminders.json
```

关闭软件或关闭提醒总开关期间不会补发错过的提醒。

## 通知区域菜单

- 置顶显示
- 和谐版
- 开启备忘录提醒
- 编辑任务列表…
- 缩放 30% / 50% / 75% / 100% / 125% / 150%
- 静音
- 重置到右下角
- 退出

位置、缩放、置顶、静音、和谐版、提醒总开关及提醒事项都会保存。

## 从源码运行

### 依赖

- [Godot 4.x](https://godotengine.org/) Mono / .NET 版
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10/11 x64
- Python 3 和 Pillow（仅素材/视觉自动化测试需要）

```powershell
git clone https://github.com/dddssp111223/FireflyMunchPet.git
cd FireflyMunchPet
dotnet restore
dotnet run --project tests\DesktopPet.Tests
```

随后使用 Godot Mono 打开根目录的 `project.godot`。仓库中的 `scripts/godot.ps1`
和 `scripts/package.ps1` 用于本项目的固定工具链验证与免安装打包；如果使用自己的
Godot 安装，可直接在编辑器中运行或导出 `Windows Desktop` 预设。

完整验证入口：

```powershell
powershell -ExecutionPolicy Bypass -File scripts\verify.ps1
```

回收站集成测试只处理测试程序自行创建并校验路径的临时文件，必须显式启用：

```powershell
powershell -ExecutionPolicy Bypass -File scripts\dotnet.ps1 run `
  --project tests\DesktopPet.Integration -- --run-recycle-tests
```

## 项目结构

```text
assets/                  角色源图、普通/和谐素材组、图标
scenes/                  Godot 主场景、角色场景和诊断场景
src/App/                 桌宠入口、音频、提醒窗口与协调逻辑
src/Character/           角色绑定、表情和独立眼球
src/Core/                状态机、设置、提醒模型与调度
src/Windows/             Win32、透明窗口、托盘和回收站集成
scripts/                 素材、验证、构建和打包脚本
tests/                   .NET 单元测试与 Windows 集成测试
docs/releases/           版本说明
```

## 技术栈

| 层面 | 技术 |
| --- | --- |
| 引擎 | Godot 4 Mono |
| 语言 | C# / .NET 8 |
| 渲染 | OpenGL `gl_compatibility` |
| 平台 | Windows 10/11 x64 |
| 系统集成 | Win32 P/Invoke、OLE、Shell COM |

## 角色素材与许可

项目中的角色美术素材为 SD WebUI 与 Codex 生成/处理的 AI 素材，不包含在 MIT
代码许可范围内。公开分发、二次创作或商用时，请自行确认相关角色与素材的使用权。

代码部分基于 [MIT License](LICENSE) 开源。

---

Made with Godot + .NET.
