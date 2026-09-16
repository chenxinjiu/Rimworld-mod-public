# HelldiversRim — RimWorld 1.6 适配工作交接

> 交接日期：2026-09-16
> 背景：云端会话结束，转移到本机 Code TRAE 继续。本文档记录当前进度、关键修复与后续要点。

## 一、项目概况

- **Mod 名**：HelldiversRim（《绝地潜兵》主题）
- **目标游戏版本**：RimWorld 1.6（含 Odyssey 等新内容，drop pod 全面改名 "transporter"）
- **仓库**：`chenxinjiu/Rimworld-mod-public`，分支 `main`，**当前 HEAD = `a2ed40d`**
- **本地路径**（Windows）：`E:\ZZZ\Rim mod zhizuo\Rimworld-mod-public`

## 二、已完成的适配（编译错误修复，共 7 处 / 4 文件）

修复已提交并推送，三处（远程 GitHub / Windows 本机 / 云端工作区）均已对齐 `a2ed40d`。

| 文件 | 原错误 | 修复 | 关键点 |
|------|--------|------|--------|
| `Source/Comms/HelldiversCommsUtility.cs` | CS0246 `ActiveDropPodInfo` 找不到 | 改名为 **`ActiveTransporterInfo`** | 1.6 官方改名；`MakeDropPodAt` 保留，参数类型换成新类 |
| `Source/Faction/WorldComponent_HelldiversFaction.cs` | CS0115 `FinalizeInit` 已被移除；CS1503 参数类型 | 改 `WorldComponentUpdate`（每帧，幂等）；`NewGeneratedFaction(new FactionGeneratorParms(def))` | 1.6 的 WorldComponent 无 `FinalizeInit` |
| `Source/Sentry/CECompat.cs` | CS1061 `Thing` 无 `AllComps` | 先强转 `parent as ThingWithComps` 再判空，最后访问 `AllComps` | |
| `Source/Comms/CompHelldiversComms.cs` | CS1661/CS1678 validator lambda 类型；CS0201 | validator 参数改 `TargetInfo`；调整 `Find.Targeter.BeginTargeting(targetParams, OnTargetSelected)` 签名 | `OnTargetSelected` 保留 `LocalTargetInfo` |

**结论：`dotnet build` 已通过（0 警告 0 错误）。**

## 三、DLL 提交约定（已定）

- `.gitignore` 新增忽略 `Assemblies/*.dll` 和 `Assemblies/*.pdb`，保留 `.gitkeep` 占位。
- **仓库保持纯源码**；编译产物 `HelldiversRim.dll` 只留在本机 mod 文件夹，供自玩 / zip 发朋友 / Steam 创意工坊上传。
- 提交为 `a2ed40d chore: git 忽略编译产物 DLL，保持仓库纯源码`。

## 四、当前同步状态

| 位置 | 提交 |
|------|------|
| GitHub 远程 `main` | `a2ed40d` ✅ |
| Windows 本机 | `a2ed40d`（Everything up-to-date）✅ |
| TraeWork 云端工作区 | `a2ed40d`（已快进对齐）✅ |

说明：仓库历史里有一笔被放弃的误提交 `8208e9c`（误提交的截图导致分叉，已丢弃，无代码损失）；历史中还残留多笔重复 "feat: Create RimWorld X Helldivers Mod" 提交，不影响使用。

## 五、接续工作要点

1. **重新编译**（在本机 Mod 目录）：
   ```cmd
   cd /d "E:\ZZZ\Rim mod zhizuo\Rimworld-mod-public\HelldiversRim\Source" && dotnet build
   ```
   - 注意：`HelldiversRim.csproj` 里写死的游戏路径是 `E:\steam`，若不对用 `-p:RimWorldPath="你的游戏安装目录"` 覆盖。
   - csproj 只显式引用了 `Assembly-CSharp.dll`、`UnityEngine.dll`、`UnityEngine.CoreModule.dll`。

2. **进游戏实测**（编译通过不等于运行时正常，这些是新增逻辑，需实机验证）：
   - Super Earth 派系创建结盟（`WorldComponent_HelldiversFaction`）
   - 通讯终端「战备呼叫」空投炮塔到指定落点（`CompHelldiversComms` + `HelldiversCommsUtility`，含白银扣除）
   - 哨戒 CE 自毁逻辑（`CompSentryLifespan`，依赖 `CECompat` 反射读 CE 弹药余量）
   - 信标（便携版）与投掷公有 `Projectile_Stratagem` / `CompStratagemBeacon` 相关

3. **待实机核对的点**：
   - `ActiveTransporterInfo` 的字段名是否仍是 `SingleContainedThing` / `openDelay`（1.6 若改字段名需同步改）
   - SOS2 软依赖（`SOS2Compat`）当前是轨道空投**框架占位**，判定点待接 SOS2 实机核对
   - `CECompat` 靠反射探测候选字段名（AmmoRemaining/MagAmmoCount/AmmoCount/RemainingAmmo），读不到会安全降级并告警一次

## 六、遗留 / 可选事项

- 暂未做：游戏内功能全面实机验证、Steam 创意工坊/分享打包、`About.xml` 的 `supportedVersions` 确认（建议写 1.6）。
- 云端 `/workspace/.uploads/` 下有若干调试截图（未跟踪），不属代码，无需处理。
