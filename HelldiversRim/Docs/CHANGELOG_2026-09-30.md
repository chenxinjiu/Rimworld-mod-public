# 变更日志 — 2026-09-30 代码简洁性优化

> 交接日期：2026-09-30
> 变更类型：**纯内部重构（无行为变更）**
> 影响范围：`Source/` 下 6 个 C# 文件，Defs / XML / Patches / Languages 均未动

---

## 一、本次变更概览

| 文件 | 改动类型 | 核心变化 |
|------|---------|---------|
| `Source/Comms/HelldiversCommsUtility.cs` | 新增公共方法 | 新增 `TrySpendSilverAndCallIn` 统一封装「扣白银 → 呼叫炮塔」完整流程 |
| `Source/Comms/CompHelldiversComms.cs` | 逻辑收敛 | `OnTargetSelected` 消除与 `CompStratagemCaller` 的重复扣费+呼叫代码；SOS2 分支直接调 `CallInTurret` |
| `Source/Stratagem/CompStratagemCaller.cs` | 逻辑收敛 | `OnTargetSelected` 从 20 行精简到 9 行；移除未使用的 `using System.Collections.Generic` |
| `Source/Comms/SOS2Compat.cs` | 删除冗余 | 删除 `TryOrbitalDrop` 空包装（内部仅转调 `CallInTurret`），调用方直接调底层 |
| `Source/Drugs/HediffComp_Stim.cs` | 提取辅助 + 简化 | 提取 `RemoveHediffs` 复用三个移除逻辑；用 `OrderByDescending + FirstOrDefault` 替代 `ToList + First` |
| `Source/Sentry/CECompat.cs` | 语法简化 | 反射属性/字段查找改用 C# 9 模式匹配 `is PropertyInfo pi / is FieldInfo fi` |

---

## 二、详细变更说明

### 1. `HelldiversCommsUtility.TrySpendSilverAndCallIn`（新增）

**动机**：`CompHelldiversComms` 与 `CompStratagemCaller` 的 `OnTargetSelected` 有几乎相同的代码块——扣白银判断 + 失败提示 + 呼叫炮塔，重复约 15 行。

**方案**：在 `HelldiversCommsUtility` 新增公共方法统一封装：

```csharp
public static bool TrySpendSilverAndCallIn(
    Map map, IntVec3 cell,
    ThingDef turretDef, ThingDef pillarDef,
    int silverCost, int openDelay = 60)
```

- `silverCost <= 0` 时视为免费，直接呼叫
- 白银不足时自动弹 `Messages.Message` 提示，返回 `false`
- 成功时调用 `CallInTurret` 并返回 `true`

**调用方**：`CompHelldiversComms.OnTargetSelected` 和 `CompStratagemCaller.OnTargetSelected` 均已切换。

---

### 2. `CompHelldiversComms.OnTargetSelected` 简化

**变更前**（20 行）：
```csharp
Map map = Map;
if (map == null) return;
bool? ship = SOS2Compat.PlayerHasOrbitalShip();
if (ship == true) { SOS2Compat.TryOrbitalDrop(...); return; }
if (!HelldiversCommsUtility.TrySpendSilver(map, Props.silverCost))
{
    Messages.Message(..., MessageTypeDefOf.RejectInput, false);
    return;
}
HelldiversCommsUtility.CallInTurret(target.Cell, map, Props.turretDef, Props.pillarDef);
```

**变更后**（18 行）：
```csharp
Map map = Map;
if (map == null) return;
// SOS2：装有 SOS2 且确认在轨飞船时直接空投（免白银）。
if (SOS2Compat.PlayerHasOrbitalShip() == true)
{
    HelldiversCommsUtility.CallInTurret(target.Cell, map, Props.turretDef, Props.pillarDef);
    return;
}
HelldiversCommsUtility.TrySpendSilverAndCallIn(
    map, target.Cell, Props.turretDef, Props.pillarDef, Props.silverCost);
```

**行为差异**：无。SOS2 轨道空投仍免白银；非 SOS2 路径仍先扣白银再呼叫。

---

### 3. `CompStratagemCaller.OnTargetSelected` 简化

**变更前**（20 行）：与 `CompHelldiversComms` 几乎相同的扣费+呼叫块，仅 `silverCost` 来源不同。

**变更后**（9 行）：
```csharp
HelldiversCommsUtility.TrySpendSilverAndCallIn(
    parent.Map, target.Cell,
    Props.mortarSentryDef, Props.beaconPillarDef,
    Props.silverCost, Props.podOpenDelay);
```

**行为差异**：无。

---

### 4. `SOS2Compat.TryOrbitalDrop` 删除

**变更前**：`TryOrbitalDrop` 是一个空包装，内部仅转调 `HelldiversCommsUtility.CallInTurret`，无额外逻辑。

**变更后**：方法已删除。调用方 `CompHelldiversComms` 在确认 `PlayerHasOrbitalShip() == true` 后直接调用 `CallInTurret`。

**保留接口**：`SOS2Compat.IsActive` 和 `SOS2Compat.PlayerHasOrbitalShip()` 不变，供未来 SOS2 实机对接时继续使用。

---

### 5. `HediffComp_Stim.cs` 重构

#### 5a. 提取 `RemoveHediffs` 辅助方法

**变更前**：`CompPostPostAdd` 中三段几乎相同的 LINQ 移除块（BloodLoss / MissingPart / Injury），每段 5 行，共 15 行。

**变更后**：
```csharp
RemoveHediffs(pawn,
    h => h.def == HediffDefOf.BloodLoss,
    h => h is Hediff_MissingPart,
    h => h is Hediff_Injury);
```

辅助方法内部使用 `params Func<Hediff, bool>[]` 接收任意多个条件，统一遍历移除。

#### 5b. 最严重伤口查找简化

**变更前**：
```csharp
var injuries = pawn.health.hediffSet.hediffs
    .OfType<Hediff_Injury>()
    .Where(i => i.Severity > 0f)
    .ToList();
if (injuries.Count > 0)
{
    var worst = injuries.OrderByDescending(i => i.Severity).First();
    ...
}
```

**变更后**：
```csharp
Hediff_Injury worst = pawn.health.hediffSet.hediffs
    .OfType<Hediff_Injury>()
    .Where(i => i.Severity > 0f)
    .OrderByDescending(i => i.Severity)
    .FirstOrDefault();
if (worst == null) return;
```

**优势**：语义更准确（`FirstOrDefault` 明确表达「可能为空」），避免不必要的 `ToList()` 分配。

---

### 6. `CECompat` 反射查找简化

**变更前**：
```csharp
PropertyInfo pi = t.GetProperty(candidate, flags);
if (pi != null && pi.PropertyType == typeof(int))
    return (int)pi.GetValue(ceComp);
FieldInfo fi = t.GetField(candidate, flags);
if (fi != null && fi.FieldType == typeof(int))
    return (int)fi.GetValue(ceComp);
```

**变更后**：
```csharp
if (t.GetProperty(candidate, flags) is PropertyInfo pi && pi.PropertyType == typeof(int))
    return (int)pi.GetValue(ceComp);
if (t.GetField(candidate, flags) is FieldInfo fi && fi.FieldType == typeof(int))
    return (int)fi.GetValue(ceComp);
```

**优势**：使用 C# 9 模式匹配，减少中间变量声明，行数从 8 行减到 6 行。

---

## 三、行为兼容性确认

| 检查项 | 状态 |
|--------|------|
| 公开 API 签名（`CallInTurret` / `TrySpendSilver` / `IsActive` / `PlayerHasOrbitalShip`） | 全部保持兼容 |
| XML / Defs / Patches / Languages | 未做任何修改 |
| 运行时行为（扣白银金额、空投延迟、自毁逻辑、Stim 效果） | 无变更 |
| RimThreaded 安全性（无静态状态、无跨 tick 缓存） | 维持原有设计 |
| CE 反射降级策略（读不到弹药时安全跳过 + 告警一次） | 维持原有设计 |

---

## 四、编译验证

由于当前环境未找到 RimWorld 安装目录（csproj 默认指向 `E:\steam\...` 但该路径不存在），**未执行 `dotnet build`**。

请在有 RimWorld 的机器上执行以下命令验证：

```powershell
cd HelldiversRim/Source
dotnet build -p:RimWorldPath="你的RimWorld安装路径"
```

预期结果：0 错误 0 警告。

---

## 五、后续建议

1. **实机验证**：重点测试通讯终端呼叫（含白银扣费）、便携呼叫器呼叫、SOS2 在轨飞船检测（如有 SOS2 环境）。
2. **可选进一步简化**：`CompSentryLifespan` 与 `Verb_SentryMortar` 逻辑已较简洁，暂无优化空间；若未来新增更多哨戒类型，可考虑将「弹尽自毁」抽象为策略接口。
3. **交接清单**：本文件可作为下次会话的上下文输入，帮助快速理解本次重构范围。

---

## 六、未完成项 / 占位符总结（交接重点）

> 本节汇总截至 2026-09-30 项目中**所有尚未完成、仍为占位符或未验证**的内容，按「类型 + 优先级」排列。
> 结论：**玩法逻辑已基本搭好，真正缺的是「美术素材」与「实机验证」两件事。**

### 6.1 美术占位（最高优先级：项目无 `Textures/` 目录，全部贴图引用原版）

| Def | 现用 texPath（占位来源） | 应为 |
|-----|------------------------|------|
| `Helldivers_MortarSentry`（核心炮塔） | 无 `graphicData`，继承原版 `BaseTurretGun` | 自绘炮台贴图（基座 + 炮管 + UI 图标） |
| `Helldivers_Gun_SentryMortar`（炮管） | `Things/Weapon_Ranged/BasicCannon`（原版） | 迫击炮管贴图 |
| `Helldivers_Proj_SentryMortarShell`（炮弹） | `Things/Projectile/MortarShell`（原版） | 迫击炮弹贴图 |
| `Helldivers_BeaconPillar`（蓝光柱） | `Things/Mote/OrbitalBeamTargeting`（原版） | 绝地潜兵蓝色光柱 Mote |
| `Helldivers_StratagemBeacon`（信标武器） | `Things/Item/Resource/Components/ComponentIndustrial`（原版） | 信号枪 / 信标图标 |
| `Helldivers_Stim` | `.../Medicine/MedicineIndustrial`（原版） | 治疗针图标（可暂用原版） |
| `Helldivers_RationBar` | `Things/Item/Resource/Meal/MealSurvivalPack`（原版） | 口粮图标（可暂用原版） |
| `Helldivers_Sample` | `Things/Item/Resource/Steel/Steel`（原版） | 勘测样本图标 |
| `Helldivers_CommsConsole` | `Things/Building/Production/CommsConsole`（原版） | 可接受，可选自绘 |
| `Helldivers_SuperEarth`（派系） | **无 `factionIconPath`** | 派系图标（白色剪影 64×64），UI 中当前缺图标 |

> 版权提示：直接搬运 HD2 官方素材有授权风险，建议做成致敬风格的自绘造型。

### 6.2 代码 / 配置占位（需按实际 API 与实机核对）

| 位置 | 占位内容 | 说明 / 待办 |
|------|---------|------------|
| `Patches/Helldivers_CE_Patch.xml` | 标记为「**起点版**」 | `verbClass=CombatExtended.Verb_ShootCE`、CB 抛射物 `Helldivers_Proj_CEMortarShell`、`ammoSet=AmmoSet_MortarGrenade` 均需在装 CE 的实机里按报错微调 |
| `Source/Sentry/CECompat.cs` → `AmmoRemaining` | 反射探测 3 个候选字段名（`CurMag` / `MagAmmoCount` / `AmmoCount`） | CE 版本间字段名有差异；探测不到会**安全降级**（不自毁）并 `Log.WarningOnce` 告警一次 |
| `Source/Sentry/CompSentryLifespan.cs` | 数值硬编码（`shots=18` / `explodeRadius=2.8` / `explodeDamage=30` / `scrapCount=60`） | 应抽成 `ARCHITECTURE.md` 第 4 节的可配置数据模型（cost/ammoCapacity/maxHP/upgradeTiers），为未来升级模块铺路 |
| `Source/Comms/SOS2Compat.cs` → `PlayerHasOrbitalShip` | **已实现**（遍历世界对象匹配 `defName=ShipOrbiting` + 玩家派系判定），非空壳 | ⚠️ 但**从未在装了 SOS2 的实机验证过**，包名 `kentington.saveourship2` 与判定逻辑待实机确认 |

### 6.3 文档与代码不同步（已修正）

以下为 `DEVELOPMENT_PLAN.md` / `ARCHITECTURE.md` 中**曾经过时**的描述，**已于本次一并修正**：

| 旧文档说法 | 实际状态 / 处理 |
|-----------|---------------|
| DEVELOPMENT_PLAN §3A：「`About/LoadFolders.xml` 缺 `<v1.6>` 条目，高优先级应立刻补」 | ✅ **已补**：`LoadFolders.xml` 现含 v1.4 / v1.5 / v1.6 三条；文档条目已勾销 |
| DEVELOPMENT_PLAN §4.2：「`SOS2Compat.PlayerHasOrbitalShip` 恒 `return false`（TODO），未实现」 | ✅ **已实现**三态判定；文档已改为「已实现，待实机验证」 |
| DEVELOPMENT_PLAN §4.2：CE 反射探测「4 个候选字段名」 | ✅ 实为 **3 个**（CurMag / MagAmmoCount / AmmoCount）；文档已修正 |
| DEVELOPMENT_PLAN §4.2：CE 补丁「ProjectileCE_Explosive / ammoSet=CE_Autocannon_Round」 | ✅ 实为 `Helldivers_Proj_CEMortarShell` / `AmmoSet_MortarGrenade`；文档已修正 |
| DEVELOPMENT_PLAN §2/§6：Phase 3「占位（API 未接）」、下一步「修 LoadFolders / 合并研究」 | ✅ Phase 3 状态改为「框架已实现」；已完成项已勾销 |

> 后续仍建议：把 `ARCHITECTURE.md` / `DEVELOPMENT_PLAN.md` / 本 CHANGELOG 三份文档统一口径或合并，减少交接时的信息重复。

### 6.4 尚未开始的 Phase（内容缺口）

| Phase | 内容 | 状态 |
|-------|------|------|
| Phase 2 | 升级模块（通讯终端花白银升级炮台弹药量/血量档位） | **暂缓**（按架构约定，等炮台做完再上） |
| Phase 4 | 炮台补全 + 美术 + 平衡 | **缺口最大**：目前只有迫击炮**一种**炮台；机枪/加特林/自动加农待补；弹量(18)/伤害(30)/射程(48)/白银成本(500)/造价(120钢+2工业)**均未按 HD2 平衡校对** |
| Phase 5 | 文案 / lore / 派系背景故事 | **尚未开始** |

### 6.5 其余需确认项

- [ ] **实机验证核心链路**（一次都还没在游戏里跑通）：空投落地 → 索敌开火 → 弹尽自毁 → 分解回钢；通讯终端扣白银；派系结盟。
- [ ] `About.xml` 的 `supportedVersions`：已写 `1.6`（✅ 已确认）。
- [ ] `Assemblies/` 目录内仅 `.gitkeep`（无编译产物，符合「仓库纯源码」约定；dll 只留本机）。
- [ ] 语言文件现状：中文有 `Keyed` + `DefInjected`（含 FactionDef/HediffDef/ResearchProjectDef/ResearchTabDef/ThingDef）；英文**仅** `Keyed`（英文走 XML 默认值，非硬缺口，可留意）。

---

## 七、一页速览（交接用）

**本次改动（已做）**：6 个 C# 文件纯内部重构，消除 ~30 行重复代码，无行为变更、无 API 破坏。

**项目当前状态**：
- ✅ 已完成：消耗品、一次性迫击炮哨戒（双路径自毁）、信标+蓝光柱、通讯终端呼叫+白银扣费、统一 `CallIn` 入口、Super Earth 盟友派系、科技解锁、1.6 加载支持、中英双语、CE 补丁框架、SOS2 软依赖适配层、独立研究 Tab。
- 🚧 占位/未验证：全部美术贴图（无 `Textures/`）、派系图标、CE 补丁值、CE 反射字段名、SOS2 判定实机、炮塔数值平衡。
- ⛔ 未开始：升级模块（暂缓）、更多炮台类型、lore 文案、全面实机验证。

**下一步建议顺序**：
1. 本机 `dotnet build` 编译验证本次重构。
2. 实机跑通核心链路（空投→开火→自毁→回钢）。
3. 补派系图标 + 至少一款自绘炮台贴图。
4. 核对 CE 补丁值 / 补 SOS2 实机判定。
5. 修正 `DEVELOPMENT_PLAN.md` 等旧文档中的过时描述。
