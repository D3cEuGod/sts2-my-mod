# sts2-my-mod

一个面向 Slay the Spire 2 的 DPT 原型模组仓库。

## Latest 安装说明

当前最新发布版本：`1.1.1`

如果你只是想安装最新版本，不需要自己构建源码，直接：

1. 在 GitHub Releases 下载最新的 `Sts2DpsPrototype-1.1.1-multiplatform-dll-only.zip`
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

## 1.1.1 发布说明

当前准备发布的 `1.1.1` 版本采用 **DLL-only 跨平台包**：

- 保留 `Sts2DpsPrototype.dll`
- 保留 `Sts2DpsPrototype.json`
- 不把 `.pck` 作为当前推荐运行路径

这样做的原因很直接：

- 当前 live runtime 已验证最稳的是 DLL-only
- 之前导出的 `.pck` 已经遇到过 Godot 运行时版本不兼容
- 安装目录里只保留一份 manifest 更安全，避免重复扫描加载

所以当前的“Windows 和 macOS 都能正常运行”的推荐发布方式，是同一份 DLL-only 包，而不是依赖 `.pck` 的完整资源包。

补充两点和 `1.1.1` 直接相关的发布修复：

- 初始化时现在会调用 `ModConfigBridge.DeferredRegister()`，这样安装了 ModConfig 时设置页才能按示例模板正常注册
- 本机构建后的 DLL 现在会复制到实际运行时扫描的 `SlayTheSpire2.app/Contents/MacOS/mods/` 目录，而不是旧的外层 `mods/` 路径

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
STS2_VERSION=1.1.1 bash tools/package_release.sh
```

它会：

1. 同步 `MainFile.cs`、`Sts2DpsPrototype.json`、`mod_manifest.json` 的版本号
2. 强制发布配置为 `has_pck=false` / `has_dll=true`
3. 编译 `Sts2DpsPrototype.dll`
4. 生成 DLL-only 发布目录
5. 产出 zip：`dist/Sts2DpsPrototype-1.1.1-multiplatform-dll-only.zip`

## 文档约定

当 repo 有实质性改动时，请同步更新：

- `CHANGELOG.md`
- `notes/decisions.md`
- `notes/known-issues.md`
- 必要时更新本 README

## 下一步

最关键的下一步是继续验证和收口，而不是继续堆新功能：

1. 继续验证 `CombatHistory.DamageReceived(...)` 是否覆盖所有关键伤害来源
2. 继续确认多人模式玩家显示名是否还有更官方的来源
3. 保持当前 overlay 可读、紧凑、不挡画面
4. 让 save/progression 相关逻辑更保守，优先保证不再破坏存档
5. 重新确认 `.pck` 与游戏运行时版本兼容前，不把它当作 live runtime 的默认路径
