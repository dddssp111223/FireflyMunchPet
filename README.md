# Firefly MunchPet — 二次元桌面文件吞噬宠

把文件/文件夹拖到桌宠身上，它会"啊呜"一口帮你吃掉——实际上是移到 Windows 回收站。支持待机动画、鼠标跟随、Q弹互动、托盘控制等。

## 功能

- **拖拽删除** — 拖文件/文件夹到桌宠身上，角色会做出渴望→张口→吞咽→反弹的动画，文件移入回收站
- **待机动画** — 眨眼、呼吸、头发和口水轻微晃动
- **眼睛跟随鼠标** — 眼球在生理合理范围内追踪鼠标位置
- **单击 Q 弹** — 点击角色任意可见区域会压扁回弹（带音效）
- **拖头发移动** — 按住上方头发区域可拖拽移动窗口
- **系统托盘菜单** — 置顶切换、四档缩放（75/100/125/150%）、静音、复位、退出
- **透明窗口** — 空白像素不遮挡桌面操作

## 下载

前往 [Releases](../../releases) 页面下载最新 `FireflyMunchPet.zip`，解压后双击 `FireflyMunchPet.exe` 即可运行。

- 平台：Windows 10/11 x64
- 无需安装 Godot 或 .NET 运行时

**注意**：首次使用建议用可丢弃的新建文件测试，被删除的文件可从回收站恢复。

## 从源码构建

### 依赖

- [Godot 4.x](https://godotengine.org/) (Mono / .NET 版)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10/11 x64

### 构建步骤

```powershell
# 1. 克隆仓库
git clone https://github.com/dddssp111223/FireflyMunchPet.git
cd FireflyMunchPet

# 2. 恢复 .NET 依赖
dotnet restore

# 3. 用 Godot 打开项目
#    启动 Godot Mono，导入项目根目录（project.godot 所在目录）
#    在 Godot 编辑器中进行构建/导出

# 或使用提供的脚本（需先配置 Godot 路径）
.\scripts\godot.ps1 --build
.\scripts\package.ps1
```

### 项目结构

```
├── src/
│   ├── App/            # 入口：主场景控制器、音频
│   ├── Character/      # 角色绑定：骨骼、眼睛、脸颊、表情
│   ├── Core/           # 状态机、手势分类、约束数学、设置
│   └── Windows/        # Win32 互操作：透明窗口、Shell 文件操作、托盘图标
├── scenes/             # Godot 场景文件
├── assets/             # 角色素材、音频、图标
├── scripts/            # 构建/打包/素材处理脚本
├── tests/              # 单元测试与集成测试
├── shaders/            # 着色器
└── docs/               # 设计规格与实现计划
```

### 运行测试

```powershell
dotnet test
```

## 技术栈

| 层面 | 技术 |
|------|------|
| 引擎 | Godot 4 (Mono / C#) |
| 语言 | C# (.NET 8) |
| 渲染 | OpenGL (gl_compatibility) |
| 平台 | Windows 10/11 x64 |
| Windows 集成 | Win32 P/Invoke, OLE, Shell COM |

## 关于角色素材

本项目中的角色美术素材为SDwebUI及Codex生成的AI素材。 代码部分可自由使用（见下方许可）。

## 许可

本项目代码基于 [MIT License](LICENSE) 开源。角色美术素材不包含在开源许可范围内。

---

*Made with Godot + .NET*
