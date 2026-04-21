# StS2 DPS Prototype 安装说明

这是当前推荐的 **DLL-only 跨平台发布包**，目标是让 Windows 和 macOS 都按同一组核心文件安装。

发布包内容：

- `Sts2DpsPrototype.dll`
- `Sts2DpsPrototype.json`
- `README-install.md`

## 安装方法

把整个 `Sts2DpsPrototype/` 文件夹复制到游戏的 `mods/` 目录下。

最终结构应类似：

```text
mods/
  Sts2DpsPrototype/
    Sts2DpsPrototype.dll
    Sts2DpsPrototype.json
    README-install.md
```

## 各平台常见路径

### macOS

Steam 默认通常在：

```text
~/Library/Application Support/Steam/steamapps/common/Slay the Spire 2/SlayTheSpire2.app/Contents/MacOS/mods/
```

### Windows

Steam 默认通常在：

```text
C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods\
```

## 说明

- 当前 live runtime 已知最稳的路径是 **DLL-only**。
- 这个包刻意 **不包含 `.pck`**，因为之前已确认过导出的 `.pck` 存在 Godot 运行时版本兼容风险。
- 这个包也只保留一份 manifest（`Sts2DpsPrototype.json`），避免安装目录里出现重复 manifest 导致 mod 被扫描两次。
- 如果之后重新验证了 `.pck` 与游戏运行时兼容，再考虑恢复完整资源包发布。
