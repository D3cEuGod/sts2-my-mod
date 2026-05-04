# sts2-my-mod

A C# mod for Slay the Spire 2 that adds a compact draggable in-game DPT panel.

The mod tracks current combat damage, lifetime damage, and previous combat summaries by hooking into combat damage events and maintaining runtime combat statistics.

## Key Features

- Draggable in-game DPT overlay
- Current combat damage tracking
- Lifetime damage tracking
- Previous combat summary
- Runtime hooks using Harmony patches
- DLL-only release package for easier installation
- Local debug hotkeys for testing overlay and damage events
- Clean English / Chinese overlay text switching via ModConfig

## Tech Stack

- C#
- .NET
- Godot Mono
- Harmony patching
- Slay the Spire 2 modding

## Screenshots

The gallery below mixes full-context gameplay shots with tighter crops of the HUD so the in-game footprint, readability, and information hierarchy are easy to evaluate at a glance.

<table>
  <tr>
    <td colspan="2" align="center">
      <img src="docs/images/Screenshot%202026-05-04%20at%2003.42.51.png" alt="Full in-combat overlay context" width="92%" />
      <br />
      <sub>
        <strong>EN:</strong> Full in-combat context. The top-right overlay shows current combat, run total, and last-combat summary while keeping the center play area unobstructed.<br />
        <strong>中文：</strong> 完整战斗场景中的右上角面板，同时展示当前战斗、本局累计和上一场结算，并尽量不遮挡主战场。
      </sub>
    </td>
  </tr>
  <tr>
    <td width="50%" align="center">
      <img src="docs/images/Screenshot%202026-05-04%20at%2003.45.08.png" alt="Readable low-obstruction HUD placement" width="100%" />
      <br />
      <sub>
        <strong>EN:</strong> Readable HUD footprint. The panel sits beside cards, enemies, and combat VFX rather than over the middle of the screen.<br />
        <strong>中文：</strong> 展示面板在卡牌、敌人和战斗特效旁边的低遮挡摆放方式，而不是压在屏幕中央。
      </sub>
    </td>
    <td width="50%" align="center">
      <img src="docs/images/Screenshot%202026-05-04%20at%2004.37.22.png" alt="Localized overlay state with preserved layout" width="100%" />
      <br />
      <sub>
        <strong>EN:</strong> Localized overlay state. The same compact layout stays clean after language switching, keeping spacing and visual hierarchy stable across English and Chinese text.<br />
        <strong>中文：</strong> 展示切换语言后的同一套紧凑布局，中英文切换后仍保持稳定的间距和层级。
      </sub>
    </td>
  </tr>
  <tr>
    <td width="50%" align="center">
      <img src="docs/images/Screenshot%202026-05-04%20at%2003.45.27.png" alt="Player row detail close-up" width="64%" />
      <br />
      <sub>
        <strong>EN:</strong> Row-level detail close-up. Each player row surfaces rank, name, DPT, total damage, and best hit in a compact card-style block.<br />
        <strong>中文：</strong> 行级特写，单个玩家行会在紧凑卡片块里展示排名、名称、DPT、总伤害和最高单次。
      </sub>
    </td>
    <td width="50%" align="center">
      <img src="docs/images/Screenshot%202026-05-04%20at%2003.45.33.png" alt="Section hierarchy close-up" width="64%" />
      <br />
      <sub>
        <strong>EN:</strong> Section hierarchy close-up. The panel separates live combat data from run-level and previous-combat summaries so performance trends are scannable at a glance.<br />
        <strong>中文：</strong> 分区层级特写，把当前战斗、本局累计和上一场结算拆开显示，方便一眼扫读。
      </sub>
    </td>
  </tr>
  <tr>
    <td width="50%" align="center">
      <img src="docs/images/Screenshot%202026-05-04%20at%2004.37.30.png" alt="Localized summary text in compact HUD" width="100%" />
      <br />
      <sub>
        <strong>EN:</strong> Localized summary view. Longer translated labels and summary lines still fit cleanly without widening the HUD or introducing extra controls.<br />
        <strong>中文：</strong> 本地化摘要视图。即使标签和摘要文字更长，也不需要把 HUD 做得更宽或加入额外控件。
      </sub>
    </td>
    <td width="50%" align="center">
      <img src="docs/images/Screenshot%202026-05-04%20at%2004.37.44.png" alt="Consistent overlay presentation across encounters" width="100%" />
      <br />
      <sub>
        <strong>EN:</strong> Consistent live-combat presentation. Another fight capture showing the overlay preserving readability and the same information hierarchy against a different board state.<br />
        <strong>中文：</strong> 另一张实战截图，说明在不同战斗场景下，面板仍保持同样的信息层级和可读性。
      </sub>
    </td>
  </tr>
</table>

## Chinese Notes / 中文说明

一个面向 Slay the Spire 2 的 DPT 原型模组仓库。

## Latest 安装说明

当前最新发布版本：`1.2.1`

如果你只是想安装最新版本，不需要自己构建源码，直接：

1. 在 GitHub Releases 下载最新的 `Sts2DpsPrototype-1.2.1-multiplatform-dll-only.zip`
2. 解压后把整个 `Sts2DpsPrototype/` 文件夹复制到游戏的 `mods/` 目录
3. 启动游戏，进入战斗后确认右上角出现面板

最终目录结构应类似：

```text
mods/
  Sts2DpsPrototype/
    Sts2DpsPrototype.dll
    Sts2DpsPrototype.json
    README-install.md
```

补充说明：

- 当前公开 release 采用 **DLL-only** 路线
- 不要同时启用第二份 manifest
- 当前作者本机已验证的 macOS 路径是 `SlayTheSpire2.app/Contents/MacOS/mods/`
- 更详细的安装说明见 `README-install.md`

## 当前状态

这个仓库已经不只是最小 starter prototype。
目前它有一条可工作的 **本地构建 -> 部署到真实游戏安装目录 -> 进游戏验证** 的 live-debug 路径。

当前重点有三件事：

- 用已验证的 STS2 运行时 hook 保持 DPT 统计正确
- 避免存档/进度修复逻辑再次破坏 modded 存档
- 把右上角面板继续收成紧凑、可读、接近原生 UI 的样式

## 1.2.1 发布说明

当前准备发布的 `1.2.1` 版本采用 **DLL-only 跨平台包**：

- 保留 `Sts2DpsPrototype.dll`
- 保留 `Sts2DpsPrototype.json`
- 不把 `.pck` 作为当前推荐运行路径

这样做的原因很直接：

- 当前 live runtime 已验证最稳的是 DLL-only
- 之前导出的 `.pck` 已经遇到过 Godot 运行时版本不兼容
- 安装目录里只保留一份 manifest 更安全，避免重复扫描加载

所以当前的“Windows 和 macOS 都能正常运行”的推荐发布方式，是同一份 DLL-only 包，而不是依赖 `.pck` 的完整资源包。

补充这次 `1.2.1` 直接相关的更新：

- 新增了 **面板中英文切换**，通过 ModConfig 的 `Overlay language` 下拉项切换，不往战斗 HUD 里再塞额外按钮
- 新的语言切换实现刻意保持在 overlay 文本层和设置层，不去碰稳定的伤害 hook / tracker 主路径
- 初始化仍会调用 `ModConfigBridge.DeferredRegister()`，这样安装了 ModConfig 时设置页可以正常注册
- 本机构建后的 DLL 仍会复制到实际运行时扫描的 `SlayTheSpire2.app/Contents/MacOS/mods/` 目录

## 当前文件结构

- `MainFile.cs`，模组入口，使用 `ModInitializer + Initialize()`
- `Scripts/DamageEventBridge.cs`，保留给外部/调试注入的最薄桥接层
- `Scripts/CombatRuntimeBridge.cs`，订阅 `CombatManager`，负责战斗开始/结束边界与 roster 同步
- `Scripts/DamageHookPatches.cs`，用 Harmony 直接 patch `CombatHistory.DamageReceived(...)` 采集真实战斗伤害
- `Scripts/DpsTracker.cs`，维护当前战斗、累计伤害、上一场结算三类统计
- `Scripts/DpsOverlay.cs`，右上角紧凑统计面板，带收起/展开按钮
- `Scripts/PrototypeController.cs`，负责挂载面板、运行时桥接和演示热键
- `Scripts/ModConfigBridge.cs`，可选接入 ModConfig，调节显示配置
- `Scripts/BaseProfileSyncBridge.cs`，base/modded 存档同步与摘要输出
- `Scripts/ProgressRescueBridge.cs`，进度异常时的保底修复逻辑
- `Scripts/AscensionUnlocker.cs`，升天解锁相关逻辑
- `Scripts/FullUnlockBridge.cs`，当前已禁用的全解锁路径，占位保留
- `Scripts/NeowProgressBridge.cs`，Neow/进度相关补救逻辑
- `Sts2DpsPrototype.csproj`，构建并复制 DLL 到游戏 mods 目录
- `Sts2DpsPrototype.json`，当前实际安装使用的唯一 manifest
- `project.godot`，Godot 项目文件
- `CHANGELOG.md`，repo 级更新日志

## 目前能做什么

### DPT 统计

- 通过 Harmony patch `CombatHistory.DamageReceived(...)` 读取真实伤害事件
- 按玩家聚合伤害，并把宠物/召唤物伤害归属到拥有者
- 使用运行时 `CombatState.RoundNumber` 计算真正的 DPT（Damage Per Turn）
- 在战斗开始时自动建立本场 roster
- 在战斗结束后保留上一场结算
- 同时维护：
  - 当前战斗
  - 累计伤害（本次启动）
  - 上一场结算

### UI 面板

- 在右上角显示一个紧凑统计面板
- 面板支持拖动位置
- 当前支持三段信息：
  - 当前战斗
  - 累计伤害
  - 上一场结算
- 支持右上角按钮收起 / 展开
- 如果安装了 ModConfig，可以把面板文字切到 English / 简体中文 / Auto
- 面板默认使用 input-pass-through，避免挡住游戏交互

### 本地调试

- `F7`，显示/隐藏整个面板
- `F8`，注入一笔演示伤害
- `F9`，重置当前战斗统计

### 存档 / 进度相关

- 已有 base/modded 双向同步、备份和摘要输出逻辑
- 已加入若干进度补救/恢复桥接
- 但这部分是高风险区域，目前应优先以“避免继续破坏存档”为目标

## 当前稳定方案

当前稳定方案不再走 `AbstractModel` / `ModHelper.SubscribeForCombatStateHooks(...)` 那条路径。

现在采用的是：

- `CombatManager.CombatSetUp`
- `CombatManager.CombatEnded`
- `CombatHistory.DamageReceived(...)` 的 Harmony postfix

也就是把：

- **战斗边界** 交给 `CombatManager`
- **每笔伤害采集** 交给 `CombatHistory.DamageReceived(...)`

这样在当前本地环境下更稳定，也更容易 live debug。

## 当前已确认的本地运行事实

- 游戏实际扫描的 mod 目录是：
  - `SlayTheSpire2.app/Contents/MacOS/mods`
- 当前真实运行最可靠的是 **DLL-only** 路径
- 导出的 `.pck` 目前不能直接用于真实游戏运行，因为本地导出时遇到过 Godot 版本不兼容
- 安装目录里不要同时放两份 manifest JSON，否则可能重复加载同一个 mod
- UI 调试时，如果看不见面板，先用夸张诊断面板验证 render path，再回头调 layout

## 构建

### 前置条件

- .NET 9 SDK
- Godot 4.5.1 Mono
- 本机已安装 Slay the Spire 2

### 本机路径假设

`Sts2DpsPrototype.csproj` 默认使用当前这台机器上的 macOS 路径，但也支持在构建时覆盖：

- `Sts2Dir=/Users/wehi/Library/Application Support/Steam/steamapps/common/Slay the Spire 2`
- `Sts2DataDir=$(Sts2Dir)/SlayTheSpire2.app/Contents/Resources/data_sts2_macos_arm64`

如果环境变化，可以在命令行传参覆盖，例如：

```bash
dotnet build Sts2DpsPrototype.csproj -p:Sts2Dir="/path/to/Slay the Spire 2" -p:Sts2DataDir="/path/to/game/data_dir"
```

### 构建 DLL

```bash
dotnet build Sts2DpsPrototype.csproj
```

构建后会尝试把 DLL 和必要文件复制到真实游戏 mod 目录。

## 打包发布

当前推荐打包命令：

```bash
STS2_VERSION=1.2.1 bash tools/package_release.sh
```

它会：

1. 同步 `MainFile.cs`、`Sts2DpsPrototype.json`、`mod_manifest.json` 的版本号
2. 强制发布配置为 `has_pck=false` / `has_dll=true`
3. 编译 `Sts2DpsPrototype.dll`
4. 生成 DLL-only 发布目录
5. 产出 zip：`dist/Sts2DpsPrototype-1.2.1-multiplatform-dll-only.zip`

## 文档约定

当 repo 有实质性改动时，请同步更新：

- `CHANGELOG.md`
- `notes/decisions.md`
- `notes/known-issues.md`
- 必要时更新本 README
- 每次 cut release 时重新核对 README 里的版本号、包名和安装说明

## 下一步

最关键的下一步是继续验证和收口，而不是继续堆新功能：

1. 继续验证 `CombatHistory.DamageReceived(...)` 是否覆盖所有关键伤害来源
2. 继续确认多人模式玩家显示名是否还有更官方的来源
3. 保持当前 overlay 可读、紧凑、不挡画面
4. 让 save/progression 相关逻辑更保守，优先保证不再破坏存档
5. 重新确认 `.pck` 与游戏运行时版本兼容前，不把它当作 live runtime 的默认路径
