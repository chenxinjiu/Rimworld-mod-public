# RimWorld 1.7（Waylith 引擎）升级工作交接

> 交接日期：2026-10-13
> 范围：`D:\rimworld` 下两套自有 mod —— **HelldiversRim** 与 **[Milira] 清清柳 · 特殊角色（MilliraSpecialCharacter）**
> 结论一句话：**官方说"绝大多数 mod 无需改动"基本属实，但"编译环境"必改；Millira 另有 2 处代码级 API 失效、HelldiversRim 有 2 处历史遗留死引用。以上全部已在本机对 1.7 程序集实测并验证修复，修复文件可直接套用。**

---

## 一、事件背景与环境事实（全部本机核验）

| 项 | 值 |
|----|----|
| 事件 | 2026-10-12 Ludeon 宣布 1.7，游戏从 Unity 迁移到自研 **Waylith 引擎**（性能 2–4 倍），同日放上 Steam **unstable 分支** |
| 本机游戏 | `C:\Program Files (x86)\Steam\steamapps\common\RimWorld`（appmanifest BetaKey=`unstable`，2026-10-13 09:15 更新完成） |
| 游戏版本 | `Version.txt` = **1.7.4976 rev1043**；`Assembly-CSharp.dll` FileVersion = **1.7.9781.31297**（1.6 基线为 1.6.9676.17735） |
| 运行时 | **net10.0 定制 CLR**（`Microsoft.NETCore.App 10.0.10-waylithclr.dev`），不再是 Mono/.NET Framework |
| 引擎程序集 | `Waylith.dll` + `Brutal.*`（Vulkan / GLFW / Slang / FreeType / KTX…）+ `Waylith.UnityCompat.dll` |
| 程序集布局 | **不再有 `RimWorldWin64_Data\Managed`**；所有程序集平铺在游戏根目录（`Assembly-CSharp.dll` 15.4MB 在根目录） |
| Unity 兼容 | `UnityStubs.dll`（提供 `UnityEngine.*` 类型）+ runtimeconfig 里 `UnityEngine*=UnityStubs` 程序集重定向 + 一批 `WaylithClr.Lax*` 宽容开关 → **老 DLL 二进制基本能继续加载** |
| Harmony | **不再随游戏分发**，改为创意工坊独立 mod：`brrainz.harmony`（ID 2009463077，v3.0.0.0，支持 1.2–1.7，含 `1.7\Assemblies\0Harmony.dll`；本机已订阅） |
| 资源结构 | `Assets.zip`（6131 项，Resources/…）+ `Data\Core`（Defs / Languages / Shaders）；mod 的 Defs/Textures/Patches/Languages/Assemblies 目录约定不变 |
| 官方口径 | 存档双向兼容（1.6↔1.7）；mod 作者只需**实测 + 把版本标成 1.7** 消除黄色提示；可用最新 C# 特性 |
| 本机 SDK | dotnet SDK **8.0.425** —— 足够（`net48` 目标编译已验证通过；**不需要**装 .NET 10 SDK） |
| 本机现状 | 游戏从未启动过（`%LocalLow%\Ludeon Studios\` 不存在）；游戏根目录**没有 `Mods\` 文件夹**（需自建）；工坊已订阅 23 个 mod |

> 范围说明：**创意工坊里的 mod 属他人开发，不在本次修复范围**。但注意：CE 仍标 1.6（未更新）、SOS2 未安装、Milira 系（Ancot/Ariandel）多数仍标 1.6——会显示黄色"可能不支持"，属预期现象，等作者更新。

---

## 二、修复清单（已实测验证，且已落入仓库）

### A. HelldiversRim（`D:\rimworld\rim mod public\Rimworld-mod-public`，原 HEAD=`de752d5`）

| # | 文件 | 问题 | 修复 | 级别 |
|---|------|------|------|------|
| A1 | `HelldiversRim\Source\HelldiversRim.csproj` | 引用指向已不存在的 `RimWorldWin64_Data\Managed\`（Assembly-CSharp / UnityEngine / UnityEngine.CoreModule 三个引用全部失效） | 改为游戏根目录：`Assembly-CSharp.dll` + **`UnityStubs.dll`**（UnityEngine 两件套整体删除） | **必改·编译** |
| A2 | `HelldiversRim\About\About.xml` | supportedVersions 只有 1.6 | 加 `<li>1.7</li>` | **必改·发布** |
| A3 | `HelldiversRim\About\LoadFolders.xml` | 只有 v1.4–v1.6 | 加 `<v1.7><li>/</li></v1.7>` | **必改·发布** |
| A4 | `HelldiversRim\Defs\Helldivers_Consumables.xml:84` | `<thingClass>Thing_Resource</thingClass>` —— **该类型在 1.7 双程序集（Assembly-CSharp + RimWorld.dll）及全部已装工坊 dll 中 0 命中**，加载必报红字 DefError（历史遗留，非 1.7 造成） | 改为 `<thingClass>Thing</thingClass>`（原版资源物品的标准做法） | **必改·加载** |
| A5 | `HelldiversRim\Defs\Helldivers_Turrets.xml:82` | `<li Class="CompToggleableOverlay"/>` —— 同样 0 命中，会报 DefError（该 comp 从未生效过） | 删除该行（如需其效果，日后用 `PatchOperationFindMod` 门控第三方 mod） | **必改·加载** |

**实测结果**：A1 完成后的 net48 编译 **0 错误**（`HelldiversRim.dll` 21504 字节）——**全部 12 个 .cs 文件一行都不用改**：8 处虚方法重写、`ActiveTransporterInfo`、`DropPodUtility.MakeDropPodAt`、`Find.Targeter.BeginTargeting`、`FactionGenerator.NewGeneratedFaction`、`Mathf.Min`（在 UnityStubs 里）等全部编译通过 = API 签名与 1.7 完全兼容。

### B. MilliraSpecialCharacter（`D:\rimworld\-cheng-RimWorld`，原 HEAD=`2f6bbf0`）

| # | 文件 | 问题 | 修复 | 级别 |
|---|------|------|------|------|
| B1 | `Source\MilliraSpecialCharacter.csproj` | 三个引用全指向死路径（`$(RMGAME)\RimWorldWin64_Data\Managed`） | ① Assembly-CSharp → 游戏根目录；② UnityEngine.CoreModule 删除（代码未用到）；③ **0Harmony → 改引工坊 Harmony mod 的 `…\content\294100\2009463077\1.7\Assemblies\0Harmony.dll`**（游戏不再随附）；环境变量 `RMGAME` 方式弃用，改 `RimWorldPath`/`HarmonyPath`（带默认值，可 `-p:` 覆盖） | **必改·编译** |
| B2 | `Source\MilliraSpecialCharacter\Mod_SpecialCharacter.cs` | `public override void OnContentConverted()` → **CS0115：1.7 的 `Verse.Mod` 已无此方法**（字符串 0 命中） | 删除该方法（原实现是空转占位，删除无行为影响；要检测联动直接在构造函数里 `ModsConfig.IsActive`） | **必改·编译** |
| B3 | `Source\MilliraSpecialCharacter\Patch_QingqingliuCompanion.cs:57` | `GenConstruct.CanStandAt(spawnCell, map)` → **CS0117：该方法在 1.7 已被完全移除**（0 命中） | 改为 **`GenGrid.Standable(spawnCell, map)`**（`Verse.GenGrid.Standable(IntVec3, Map)` 已用元数据核实存在） | **必改·编译** |
| B4 | `About\About.xml` | ① supportedVersions 缺 1.7；② **运行期硬依赖 HarmonyLib 但没声明 Harmony 依赖**（1.7 起 Harmony 是独立 mod） | 加 `<li>1.7</li>`；modDependencies + loadAfter 补 `brrainz.harmony`（`steam://url/CommunityFilePage/2009463077`） | **必改·发布** |
| B5 | `README.md` | 构建说明仍是 `RMGAME=…\RimWorldWin64_Data` 旧布局 | 更新为 1.7 新路径与新变量 | 建议 |
| B6 | `.gitignore`（仓库根） | 目前只有 `_reference/`、`.uploads/` | 建议加 `Assemblies/*.dll`、`Source/bin/`、`Source/obj/`（与 HelldiversRim 仓库的纯源码约定一致） | 建议 |

**实测结果**：B1–B3 完成后 net48 编译 **0 错误**（`MilliraSpecialCharacter.dll` 6656 字节）。Harmony 补丁挂点 `Verse.Pawn::SpawnSetup(Verse.Map, Boolean)` 经元数据核实**签名不变**，补丁本身安全。

---

## 三、已验证的 1.7 API 事实（1.7.4976，元数据/字符串实测）

| API | 1.7 状态 | 说明 |
|-----|---------|------|
| `Verse.Pawn::SpawnSetup(Map, bool)` | ✅ 存在，签名不变 | Millira 的 Harmony 挂点（`__instance, map, respawningAfterLoad` 匹配） |
| `Verse.Projectile::Impact(Thing, bool)` | ✅ protected virtual | HelldiversRim 重写兼容 |
| `Verse.Verb::TryCastShot()` | ✅ protected virtual abstract | 同上 |
| `Verse.ThingComp::CompTick()` / `CompGetGizmosExtra()` | ✅ public virtual | 注意 ThingComp 在 `Verse` 命名空间 |
| `Verse.HediffComp::CompPostPostAdd(DamageInfo?)` / `CompPostTick(ref float)` | ✅ | 1.6 的签名在 1.7 保持 |
| `RimWorld.Planet.WorldComponent::WorldComponentUpdate()` | ✅ public virtual | 1.6 修法继续有效 |
| `RimWorld.ActiveTransporterInfo` | ✅ `openDelay` 字段、`SingleContainedThing` 属性都在 | drop pod→transporter 改名后的现状保持 |
| `Verse.GenGrid::Standable / Walkable / InBounds(IntVec3, Map)` | ✅ public static | **CanStandAt 的替代** |
| `Verse.CellFinder::RandomClosewalkCellNear(IntVec3, Map, int)` | ✅ | Millira 现有调用兼容 |
| `Verse.Mod::OnContentConverted` | ❌ **已移除** | Millira 需删重写 |
| `GenConstruct.CanStandAt` | ❌ **已移除**（全程序集 0 命中） | Millira 需换 `GenGrid.Standable` |
| XML 引用类（CompGlower / CompPowerTrader / Thing_Ingestible / Projectile_Explosive / Verb_Shoot / IngestionOutcomeDoer_GiveHediff / HediffCompProperties_SeverityPerDay …） | ✅ 全部存在 | 仅 `Thing_Resource`、`CompToggleableOverlay` 两个是死引用 |

---

## 四、接手操作手册

### 1. 编译（本机已验证的命令）

HelldiversRim：

```powershell
dotnet build "D:\rimworld\rim mod public\Rimworld-mod-public\HelldiversRim\Source\HelldiversRim.csproj"
# 换机器时覆盖路径：
# dotnet build ... -p:RimWorldPath="你的RimWorld安装目录"
```

MilliraSpecialCharacter：

```powershell
dotnet build "D:\rimworld\-cheng-RimWorld\MilliraSpecialCharacter\Source\MilliraSpecialCharacter.csproj"
# 覆盖：-p:RimWorldPath="..." -p:HarmonyPath="...\294100\2009463077\1.7\Assemblies"
```

编译产物会自动拷进各自 mod 的 `Assemblies\`（HelldiversRim 的 `Assemblies\HelldiversRim.dll`、Millira 的 `Assemblies\MilliraSpecialCharacter.dll`）。

### 2. 装进游戏实测（1.7 unstable 已就绪）

1. 游戏根目录新建 `Mods\`，把两个 mod 文件夹拷进去（推荐 `mklink /J` 软链接到 D 盘源码目录，避免双份维护；若提示权限不足，用管理员权限操作或从 Steam 库移出 Program Files）。
2. 启动游戏（Steam 会提示 Play 1.7 Unstable），开 Dev Mode。
3. 看日志：首次运行后位于 `C:\Users\33060\AppData\LocalLow\Ludeon Studios\RimWorld\`（1.7 读我文档：`...\Ludeon Studios\RimWorld\`）。

### 3. 实机验证清单

- **HelldiversRim**：加载无红字（重点确认 A4/A5 修完）→ 研究 `Helldivers_OrbitalSentryTactics` → 通讯终端呼叫（扣 500 银）→ 蓝光柱 + 空投 → 索敌开火 → 弹尽自毁 → 分解 60 钢 → Super Earth 派系结盟 → 便携信标路径。CE 路径**暂缓**（工坊 CE 仍是 1.6，等更新）；SOS2 未装，跳过。
- **Millira**：通讯台金色羽毛招募清清柳 → 落地自动召唤绮罗誓卫 + 双向 Lover → 读档不重复召唤 → 日志无 Harmony 报错。

### 4. 发布

- 两个 mod 的 `About.xml` 已标 1.7 → 玩家端黄色"可能不支持"提示消失。
- Steam 创意工坊更新；GitHub 推送（本机网络访问 GitHub 失败时先开全局代理再推）。

---

## 五、残留风险 / 待实机确认（本环境无法覆盖的部分）

1. **net48 产物在 Waylith CLR 的实际加载**：兼容层（facades、重定向、Lax 开关）就是为老程序集设计的，但"编译通过"≠"运行通过"，首次启动务必看日志。
2. **Defs 字段级校验**：本次只验证了"类存在"，字段名/取值错误只会在加载时冒 DefError（实机第一查）。
3. **CE**：等官方 1.7 版更新后再验 `CECompat` 反射字段与 `Helldivers_CE_Patch.xml`。
4. **Milira 系依赖**：Ancot/Ariandel 等多数未更新 1.7；若其加载失败，Millira 只能等作者。另：工坊未见 `Ancot.KiiroRace`，测试前先确认绮罗种族本体已订阅。
5. **仓库杂项**：HelldiversRim 仓库根目录的 `Source\`、`Languages\` 是 1.6 前旧布局残留（不在 mod 加载路径内），建议清理；根目录 3 个"狩技盒子_太刀自动喊话"zip 是**怪物猎人 mod**，与环世界无关，未处理。
6. `HelldiversRim\Docs\UPGRADE_1.7_CHECKLIST.md` 已完成使命（其"1.7 爆炸点预测"与实际吻合），本文档为其收尾与继任。

---

## 六、附件（修复文件，均已实测）

```
修复文件\
├── HelldiversRim\
│   ├── HelldiversRim.csproj        # A1 修复版（直接覆盖 Source\ 下的同名文件）
│   ├── About.xml                   # A2 修复版
│   ├── LoadFolders.xml             # A3 修复版
│   └── 编译产物\HelldiversRim.dll  # 对 1.7 编译通过，可直接丢 Assemblies\ 试跑
└── MilliraSpecialCharacter\
    ├── MilliraSpecialCharacter.csproj   # B1 修复版
    ├── Mod_SpecialCharacter.cs          # B2 修复版（已删 OnContentConverted）
    ├── Patch_QingqingliuCompanion.cs    # B3 修复版（GenGrid.Standable）
    ├── About.xml                        # B4 修复版（+1.7、+Harmony 依赖）
    └── 编译产物\MilliraSpecialCharacter.dll  # 可先丢 Assemblies\ 试跑
```

> 覆盖文件时保留原目录结构（`About.xml` 进 `About\`、csproj 进 `Source\` 等）。
> A4/A5 两处 XML 死引用改动很小，按第二节表格直接改原文件即可（未出整文件）。

---

## 附录：本次验证的证据与复现方式

1. **原样编译失败**（两个仓库旧 csproj 指向 `RimWorldWin64_Data\Managed`）→ 全量 CS0246（找不到 RimWorld/Verse/UnityEngine 命名空间）。
2. **改新布局后编译**：HelldiversRim（Assembly-CSharp+UnityStubs）/ Millira（+Harmony mod 的 0Harmony）→ net48 目标 0 错误。
3. **元数据核对**：用 `System.Reflection.Metadata` 读取 `Assembly-CSharp.dll`，逐条导出第三节表中的方法签名（探针代码：`temp_20261013_02\probe\metacheck`）。
4. **字符串扫描**：`CanStandAt`、`CompToggleableOverlay`、`Thing_Resource`、`OnContentConverted` 在 `Assembly-CSharp.dll` + `RimWorld.dll` + 全部工坊 dll 中均为 0 命中；其余引用类均 >0。