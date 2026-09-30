# HelldiversRim — 1.7 升级核对清单

> 建立日期：2026-09-30
> 用途：RimWorld 1.7 发布后，照此清单逐项核对，快速定位需要改动的代码点。
> 前提：当前基线为 **1.6**，项目在 1.7 正式发布前**按兵不动**（不做新功能，只保留可回退状态）。

---

## 一、当前基线快照（1.6）

| 项目 | 值 |
|------|-----|
| 游戏程序集 | `Assembly-CSharp.dll` FileVersion **1.6.9676.17735** |
| 程序集路径 | `E:\steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\Assembly-CSharp.dll` |
| 程序集时间戳 | 2026-07-15 |
| `About.xml` supportedVersions | 仅 `<li>1.6</li>`（✅ 符合"不提前写 1.7"的约定） |
| 目标框架 | .NET Framework 4.8.1 |
| 编译产物 | `Assemblies/HelldiversRim.dll`（不提交，仅留本机；仓库纯源码） |
| 最近一次编译验证 | 2026-09-30，重构提交 `ad249bc` 后：**0 警告 0 错误** |
| 最近一次提交 | `ad249bc 对某些已知问题进行优化`（另一位创作者的重构） |

**外部软依赖**（全部经 `ModsConfig.IsActive` 门控，未装则完全不生效）：

| 依赖 | workshop 路径 | 判定用 packageId / 名称 |
|------|--------------|----------------------|
| Combat Extended | `294100\2890901044` | `ModsConfig.IsActive("Combat Extended")` |
| Save Our Ship 2 | `294100\1909914131` | `kentington.saveourship2` |

---

## 二、API 接触面清单

> 范围：`Source/` 下 12 个业务 C# 文件（不含 `obj/` 自动生成文件）。
> 分级：**A** = 签名变更会直接编译报错；**B** = 方法/类名变更会编译报错或运行异常；**C** = 依赖第三方 mod，风险最高；**D** = 纯数据，低风险。

### A. 虚方法重写（8 处）

签名一旦变化，`dotnet build` 直接报错，最容易定位。

| 文件 | 重写方法 | 基类 |
|------|---------|------|
| [Projectile_Stratagem.cs](file:///E:/ZZZ/Rim%20mod%20zhizuo/Rimworld-mod-public/HelldiversRim/Source/Stratagem/Projectile_Stratagem.cs#L11) | `Impact(Thing hitThing, bool blockedByShield = false)` | `Projectile` |
| [Verb_SentryMortar.cs](file:///E:/ZZZ/Rim%20mod%20zhizuo/Rimworld-mod-public/HelldiversRim/Source/Sentry/Verb_SentryMortar.cs#L12) | `TryCastShot()` | `Verb` |
| [CompSentryLifespan.cs](file:///E:/ZZZ/Rim%20mod%20zhizuo/Rimworld-mod-public/HelldiversRim/Source/Sentry/CompSentryLifespan.cs#L59) | `CompTick()` | `ThingComp` |
| [CompHelldiversComms.cs](file:///E:/ZZZ/Rim%20mod%20zhizuo/Rimworld-mod-public/HelldiversRim/Source/Comms/CompHelldiversComms.cs#L38) | `CompGetGizmosExtra()` | `ThingComp` |
| [CompStratagemCaller.cs](file:///E:/ZZZ/Rim%20mod%20zhizuo/Rimworld-mod-public/HelldiversRim/Source/Stratagem/CompStratagemCaller.cs#L36) | `CompGetGizmosExtra()` | `ThingComp` |
| [HediffComp_Stim.cs](file:///E:/ZZZ/Rim%20mod%20zhizuo/Rimworld-mod-public/HelldiversRim/Source/Drugs/HediffComp_Stim.cs#L27) | `CompPostPostAdd(DamageInfo? dinfo)` | `HediffComp` |
| [HediffComp_Stim.cs](file:///E:/ZZZ/Rim%20mod%20zhizuo/Rimworld-mod-public/HelldiversRim/Source/Drugs/HediffComp_Stim.cs#L86) | `CompPostTick(ref float severityAdjustment)` | `HediffComp` |
| [WorldComponent_HelldiversFaction.cs](file:///E:/ZZZ/Rim%20mod%20zhizuo/Rimworld-mod-public/HelldiversRim/Source/Faction/WorldComponent_HelldiversFaction.cs#L19) | `WorldComponentUpdate()` | `WorldComponent` |

### B. 静态方法 / 工具类调用

| 调用 | 位置 | 备注 |
|------|------|------|
| `ActiveTransporterInfo`（构造 + `SingleContainedThing` + `openDelay`） | `HelldiversCommsUtility.cs:29` | ⚠️ **1.6 刚被改名**（原 `ActiveDropPodInfo`） |
| `DropPodUtility.MakeDropPodAt(cell, map, info)` | `HelldiversCommsUtility.cs:35` | 1.6 保留了旧名，留意 |
| `Find.Targeter.BeginTargeting(targetParams, callback)` | `CompHelldiversComms.cs:80`、`CompStratagemCaller.cs:71` | ⚠️ **1.6 刚改过签名** |
| `FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(def))` | `WorldComponent_HelldiversFaction.cs:32` | ⚠️ **1.6 刚改过** |
| `Find.World.factionManager.AllFactions` / `.Add(faction)` | `WorldComponent_HelldiversFaction.cs:29,35` | |
| `HediffMaker.MakeHediff(hediffDef, pawn)` | `HediffComp_Stim.cs:49` | |
| `HediffDefOf.BloodLoss` | `HediffComp_Stim.cs:36` | 原版 DefOf 引用 |
| `ThingMaker.MakeThing(def)` | `HelldiversCommsUtility.cs:20,27`、`CompSentryLifespan.cs:89` | |
| `GenSpawn.Spawn(thing, cell, map)` | `HelldiversCommsUtility.cs:21`、`CompSentryLifespan.cs:91` | |
| `Find.TickManager.TicksGame` | `CompSentryLifespan.cs:65` | |
| `Find.WorldObjects.AllWorldObjects` | `SOS2Compat.cs:49` | 标准 API |
| `DefDatabase<T>.GetNamedSilentFail(name)` | `HediffComp_Stim.cs:46`、`WorldComponent_HelldiversFaction.cs:25` | 用 SilentFail，找不到不崩 |

### C. 软依赖 / 反射（风险最高）

这些不是你自己的代码问题，而是**等 CE / SOS2 自己适配 1.7 后才能确认**。

| 依赖 | 机制 | 位置 | 1.7 应对 |
|------|------|------|---------|
| CE | 反射读弹药余量：候选字段 `CurMag` / `MagAmmoCount` / `AmmoCount`，按命名空间前缀 `CombatExtended` 匹配类型 | [CECompat.cs](file:///E:/ZZZ/Rim%20mod%20zhizuo/Rimworld-mod-public/HelldiversRim/Source/Sentry/CECompat.cs) | CE 改字段名只需扩充候选列表；读不到会安全降级（不自毁）+ `Log.WarningOnce` |
| CE | XML 补丁：`Verb_ShootCE` / `ProjectileCE_Explosive` / `ProjectilePropertiesCE` / `CompProperties_AmmoUser` / `AmmoSet_MortarGrenade` | [Helldivers_CE_Patch.xml](file:///E:/ZZZ/Rim%20mod%20zhizuo/Rimworld-mod-public/HelldiversRim/Patches/Helldivers_CE_Patch.xml) | 补丁整体被 `PatchOperationFindMod` 门控，CE 未适配时不影响裸跑 |
| SOS2 | `ModsConfig.IsActive("kentington.saveourship2")` + 世界对象 `defName == "ShipOrbiting"` | [SOS2Compat.cs](file:///E:/ZZZ/Rim%20mod%20zhizuo/Rimworld-mod-public/HelldiversRim/Source/Comms/SOS2Compat.cs) | 只用 RimWorld 标准 API，不反射 SOS2 私有类型，对版本最稳健 |

### D. 纯数据（低风险）

`Defs/`（6 个 XML + `Defs/Hediffs/`）、`Patches/`、`Languages/` 全部无 C# 依赖。
1.7 若改字段名会抛 DefError，但**报错信息直接指向 def 名**，修起来最快。

---

## 三、1.7 发布后的核对流程

按此顺序做，避免一上来就乱改代码：

1. **先不改任何代码**，用 1.7 直接加载 mod，收集报错：
   - 启动时的 `DefError` / `XML error`（→ D 类）
   - 日志里的 `MissingMethodException` / `TypeLoadException`（→ A/B 类）
2. **重编**：`dotnet build`。编译报错 = A/B 两类签名变更，**逐条对照上面表格修**。
3. **跑核心链路**（详见第四节的风险点）：空投落地 → 索敌开火 → 弹尽自毁 → 分解回钢；通讯台扣白银；派系结盟。
4. **最后才动 CE / SOS2**：等这两个 mod 自己出了 1.7 版再核对 C 类，别提前改。
5. 全部通过后，才把 `About.xml` 的 `supportedVersions` 加上 `1.7`。

---

## 四、1.6 已发生过的 API 变更（1.7 的预警参考）

这些是本项目在 **1.5 → 1.6** 迁移时实际踩过的坑。同类改动在 1.7 很可能重演，届时优先怀疑这些位置。

| 1.6 发生的变化 | 影响的本项目代码 | 采用的修法 |
|---------------|----------------|-----------|
| Drop pod 体系全面改名（"drop pod" → "transporter"），`ActiveDropPodInfo` 类被替换 | `HelldiversCommsUtility.cs` | 改用 `ActiveTransporterInfo`；字段名 `SingleContainedThing` / `openDelay` 待实机确认 |
| `WorldComponent` 的 `FinalizeInit` 被移除 | `WorldComponent_HelldiversFaction.cs` | 改用 `WorldComponentUpdate()`（每帧，逻辑幂等） |
| `FactionGenerator` 生成派系的签名变更 | `WorldComponent_HelldiversFaction.cs` | 改用 `new FactionGeneratorParms(def)` |
| `Thing.AllComps` 上移到 `ThingWithComps` | `CECompat.cs` | 先 `parent as ThingWithComps` 判空再访问 |
| `HediffComp.CompPostPostAdd` 新增 `DamageInfo?` 参数 | `HediffComp_Stim.cs` | 签名同步为 `CompPostPostAdd(DamageInfo? dinfo)`，`base` 调用传参 |
| `Find.Targeter.BeginTargeting` 的参数类型/回调签名调整 | `CompHelldiversComms.cs`、`CompStratagemCaller.cs` | validator 参数改 `TargetInfo`；回调保持 `LocalTargetInfo` |

**规律**：1.6 的破坏性改动集中在"**重命名**"（drop pod → transporter）和"**虚方法签名增参**"两类。1.7 出现同类问题时，优先怀疑 `Defs` 里的 `thingClass`、以及 A 类 8 个 override 点。

---

## 五、回退方案（1.6 快照）

若 1.7 出现大的破坏性变更、短期内修不动，可整体回退到 1.6：

| 项 | 值 |
|----|-----|
| 1.6 程序集版本 | `1.6.9676.17735` |
| Steam 回退 | RimWorld 官方在 Steam 提供 beta 分支，可切换到旧版本（`RimWorld` 属性 → 测试版） |
| mod 侧 | `About.xml` 的 `supportedVersions` 保持 `1.6`，在 1.6 下即可正常加载 |
| 编译 | 1.6 的 `Assembly-CSharp.dll` 环境可重编出可用的 `HelldiversRim.dll` |

> 建议：升级 1.7 前，先把当前可用的 `Assemblies/HelldiversRim.dll` 另存到**仓库之外**（例如 `E:\ZZZ\Rim mod zhizuo\_hd_backup\`），避免被误提交进 git。
> 本次已执行：`Assemblies/HelldiversRim.dll` 已更新为含最新重构的版本（2026-09-30 编译，21504 字节），并在 `E:\ZZZ\Rim mod zhizuo\_hd_backup\1.6_20260930\` 留了副本。

---

## 六、一句话总结

**代码对外暴露的接触面很小**——8 个虚方法重写、12 处静态/工具类调用、2 个软依赖适配层。
1.7 真要炸，大概率炸在：`ActiveTransporterInfo`（重命名史）、8 个 override 的签名、以及 CE/SOS2 的适配进度。其余（Defs / Languages）即使报错也一眼能修。