# DefectSkinModFix | 故障机器人娘化兼容补丁

[![Version](https://img.shields.io/badge/version-v0.1.0-blue)](#版本与兼容性)
[![Game](https://img.shields.io/badge/Slay%20the%20Spire%202-v0.113.x-informational)](#版本与兼容性)
[![Status](https://img.shields.io/badge/status-tested%20with%20known%20limitation-orange)](#已知限制)

适用于《杀戮尖塔 2》（Slay the Spire 2）的**非官方独立兼容补丁**，用于修复游戏更新至 v0.113.x 后，原模组 [DefectSkinMod（故障机器人娘化）](https://steamcommunity.com/sharedfiles/filedetails/?id=3748976166) 因 API 变动而无法正常初始化的问题。

本项目仅提供兼容性代码，**不包含、不修改或重新分发原模组的 DLL、PCK、美术或动画资源**。使用时仍需安装并启用原模组。

> [!IMPORTANT]
> **v0.1.0 已通过本地游戏测试，但仍有一个必须手动处理的限制：** 原模组加载时会先报错。使用支持忽略初始化错误并继续加载的模组管理器（例如 Mod Launch Manager）时，请对 **原模组「鸡煲娘化 / DefectSkinMod」** 选择 **「忽略并继续」**，让后续的 `DefectSkinModFix` 加载。**不要关闭原模组。** 本补丁目前无法阻止原模组先报错，也无法在原模组报错时自动替你选择继续。

## 版本与兼容性

- **补丁版本：** `v0.1.0`
- **适配目标：** 游戏 `v0.113.x`（已在 `v0.113.1` 环境中测试通过）
- **依赖模组：** [DefectSkinMod（故障机器人娘化）](https://steamcommunity.com/sharedfiles/filedetails/?id=3748976166)
- **加载顺序：** `DefectSkinMod` → `DefectSkinModFix`
- **适用范围：** 修复原模组在上述游戏版本中的初始化兼容问题；其他游戏版本尚未验证

## 安装与使用

### 通过创意工坊安装

如果补丁已发布到 Steam 创意工坊，订阅**原模组**和**本补丁**，并同时启用。原模组仍是所有皮肤资源的来源。

### 通过 GitHub Release 或本地文件安装

如果 Release 页面提供了预编译文件，将 `DefectSkinModFix.dll` 和 `DefectSkinModFix.json` 放入游戏目录下的：

```text
Slay the Spire 2/
└── mods/
    └── DefectSkinModFix/
        ├── DefectSkinModFix.dll
        └── DefectSkinModFix.json
```

也可使用游戏的本地模组导入功能，导入构建产生的 `Dist/DefectSkinModFix` 文件夹。**不要将修复文件覆盖到原模组目录。**

### 启动步骤

1. 启用 `DefectSkinMod` 和 `DefectSkinModFix`，确保原模组先加载。
2. 启动游戏；原模组出现初始化错误时，选择 **「忽略并继续」**。
3. 等待 `DefectSkinModFix` 加载完成，再进入游戏。
4. 在角色选择界面选择故障机器人，检查皮肤；建议再检查战斗、商店与休息场景。

> [!WARNING]
> 如果你的模组加载器在原模组抛出异常后直接终止加载、没有「忽略并继续」功能，当前补丁不一定能够生效。这是 v0.1.0 尚未解决的限制。

## 已知限制

- **原模组的报错仍然存在。** 本补丁在原模组之后加载，无法阻止原模组初始化时抛出异常；当前必须允许后续模组继续加载。
- 原模组的 `version` 写为 `1.0`，不符合加载器的语义化版本要求。本补丁仅按模组 ID 声明依赖，**没有设置 `min_version`**，避免依赖检查在补丁启动前失败。
- 当前仅针对已发现的 v0.113.x 兼容问题做修复；如果原作者更新了实现、游戏继续更改 API，补丁可能需要再次适配。
- 如果原模组未来已经自行修复，建议先禁用本补丁进行测试，以免产生重复补丁或其他冲突。

## 修复原理

游戏更新后，`NCharacterSelectScreen.SelectCharacter` 的参数签名发生变化，原模组的 Harmony `Postfix` 仍使用已移除的 `characterModel` 参数，导致 `Harmony.PatchAll()` 抛出异常，原模组后续的资源预加载也未执行。

本补丁在原模组加载之后：

1. 定位原模组已加载的程序集；
2. 尝试恢复因初始化中断而未注册的其他 Harmony 补丁；
3. 为新版 `SelectCharacter(NCharacterSelectButton)` 注册兼容的 Postfix，通过按钮的 `Character` 属性取得角色模型，再调用原作者原有的处理逻辑；
4. 调用原模组未执行的资源预加载方法。

这是一种**加载失败后的恢复方案**，并非消除原模组启动错误的前置补丁。

## 从源码构建

### 环境要求

- Windows
- GNU Make（使用 `make` 命令时）
- 已安装的《杀戮尖塔 2》（需包含 `sts2.dll`、`0Harmony.dll`、`GodotSharp.dll`）
- **.NET 9 SDK**（通过 PATH 中的 `dotnet` 或 Scoop 提供）

在仓库根目录创建 `local.mk`，写入游戏安装目录（此文件已加入忽略规则）：

```makefile
GAME_DIR := D:/path/to/Slay the Spire 2
```

之后执行：

```sh
make
```

也可以通过命令行临时指定目录：

```sh
make GAME_DIR="D:/path/to/Slay the Spire 2"
```

也可直接运行 `Build.ps1 -GameDir '<游戏目录>'`，或设置 `STS2_DIR` 环境变量。脚本会在 Scoop 可用且能找到 `dotnet9-sdk` 时优先使用该 SDK，否则使用 PATH 中的 `dotnet`。构建成功后会生成：

```text
BuildOutput/DefectSkinModFix.dll
Dist/DefectSkinModFix/
├── DefectSkinModFix.dll
└── DefectSkinModFix.json
WorkshopWorkspace/content/
├── DefectSkinModFix.dll
└── DefectSkinModFix.json
```

测试时可自行导入或复制 `Dist/DefectSkinModFix`。切换到创意工坊订阅版本前，记得移除同 ID 的本地测试副本，以免重复加载。

## 问题反馈

如果遇到加载失败或皮肤异常，请在 Issue 中尽量提供：

- 游戏版本及所用分支；
- 原模组和本补丁的版本；
- 加载顺序、是否选择过「忽略并继续」；
- 能够复现问题的操作步骤，以及相关模组列表；
- 日志 `%APPDATA%\SlayTheSpire2\logs\godot.log` 中与 `DefectSkinMod` / `[DefectSkinModFix]` 有关的错误信息。

上传日志前请检查并删除其中不希望公开的个人信息或本地路径。

## 致谢与声明

- 原模组作者：**九九八十一**，原模组页面：[DefectSkinMod（故障机器人娘化）](https://steamcommunity.com/sharedfiles/filedetails/?id=3748976166)
- 原模组所注明的角色设计来源：**どべぽぽん**

本项目为非官方社区兼容补丁，与原作者及游戏开发者没有官方关联。所有原模组资源仍由原作者的创意工坊项目提供。建议在转发或发布时保留原模组链接和署名。

---

## English summary

**DefectSkinModFix v0.1.0** is an unofficial, DLL-only compatibility add-on for the [original DefectSkinMod](https://steamcommunity.com/sharedfiles/filedetails/?id=3748976166) on *Slay the Spire 2* v0.113.x (locally tested on v0.113.1). It does not bundle or modify the original skin assets or assembly.

**Known limitation:** The original mod still throws an initialization error **before** this fix loads. With a mod loader that supports continuing after errors (such as Mod Launch Manager), choose **Ignore and Continue** for the **original DefectSkinMod**, then allow `DefectSkinModFix` to load. Keep both mods enabled, in that order. Without this ability to continue loading, the fix may not work.

For a manual install, place `DefectSkinModFix.dll` and `DefectSkinModFix.json` in `Slay the Spire 2/mods/DefectSkinModFix/`. To build, set `GAME_DIR` in an ignored `local.mk` file, then run `make`; you can also pass `GAME_DIR` on the command line. A .NET 9 SDK is required. See the Chinese sections above for details.
