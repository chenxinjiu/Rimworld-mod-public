# HelldiversRim — 架构与实装路线图

> 环世界 × 绝地潜兵 内容mod。本文档描述**目标架构**与**分批实装计划**（Phase）。
> 当前代码只覆盖部分 Phase，其余按本文档逐步落地。

---

## 1. 目标

Super-Earth（绝地潜兵）风格的内容mod，计划包含：

- Super-Earth 消耗品/道具
- 绝地潜兵"绿色战备"炮塔体系（一次性，空投落地，弹尽自毁）
- **呼叫体系**：通讯终端（核心）+ 信标（便携版）+ SOS2 飞船空投（软依赖）
- **升级模块**：通讯终端花白银升级各战备
- 固定盟友派系 + 科技树解锁

---

## 2. 当前已落地（现状）

| 部分 | 内容 | 状态 |
| --- | --- | --- |
| 消耗品 | stim(治疗针)、ration bar(口粮)、sample(样本) | ✅ |
| 一次性迫击炮哨戒 | 空投落地、自带弹药、弹尽自毁+分解废料(Steel) | ✅ 代码，待实机测 |
| 自毁逻辑 | 非CE按**射击数**；CE按**弹药余量** | ✅ 双路径 |
| 信标武器(便携版) | 射击落点 → 蓝色光柱 → 空投舱载炮塔 | ✅ |
| 统一呼叫入口 | `HelldiversCommsUtility.CallInTurret`：通讯终端/信标共用 | ✅ |
| 通讯终端呼叫 | `Helldivers_CommsConsole` + 白银扣费 + 落点瞄准 | ✅ |
| 固定盟友派系 | Super Earth(`Helldivers_SuperEarth`) + 开局强制结盟 | ✅ |
| 科技解锁 | `Helldivers_OrbitalSentryTactics` 门控呼叫 | ✅ |
| SOS2 空投框架 | `SOS2Compat` 软依赖适配 + 轨道空投入口 | 🚧 框架已搭，API待实机核 |
| CE 兼容 | PatchOperationFindMod 门控 + C# 反射软依赖(CECompat) | ⚠️ 起点版，需实机验证 |

---

## 3. 目标架构

### 3.1 呼叫体系（核心）

三种呼叫方式，**本质都是"花钱呼叫一个战备落地"**：

1. **通讯终端呼叫（核心）**：花费白银，召唤炮塔。后续升级模块也走这里。
2. **信标武器（便携版）**：殖民地背包/装备里的信标，射击指定落点呼叫。与通讯终端并存。
3. **SOS2 飞船空投（软依赖）**：装了 SOS2 时，从同步轨道飞船向下空投炮塔。未装 SOS2 则退回通讯终端呼叫。

**计费**：呼叫 = 消耗白银（`Silver`）。单次成本在 Turret 数据表里配置。

> 现有 `verb`/抛射物落地触发即"便携版"通路，未来把"最终落地、扣费、生成"收敛到统一的 `CallIn` 入口，三种来源共用。

### 3.2 派系与解锁

- 新增一个**固定盟友派系**（Super Earth / 苏巴黎自由侧）。
- 通过点亮**某科技树**解锁"呼叫战备"能力（在当前 `Helldivers_OrbitalSentryTactics` 基础上扩展）。
- 派系文案、背景故事：后续 Phase 补。

### 3.3 升级模块（暂缓 → Backlog）

**明确：本模块暂不实装，等全部炮台做完后再上。**

- 入口：通讯终端。
- 花费：白银。
- 可升级属性：**弹药量、血量**（按炮台分别升级，多档位）。
- **不加**升级后的存在时长（不延长炮台存续时间）。

### 3.4 炮台数据与美术

- **待用户上传**：HD2 迫击炮 3D 模型、绝地潜兵里的数值、蓝色光柱参考图。
- 上传后：落到统一的 Turret 数据表、替换/增强光柱 Mote 与炮塔贴图。

---

## 4. 数据模型（草案）

所有战备收敛到一个配置模型，便于升级模块做"档位"：

```
Turret 配置（每座炮台一组）：
- cost            呼叫/建造花费（白银/材料）
- ammoCapacity    弹药量（= 一次性射击数上限）
- maxHP           血量
- damage / range  火力参数
- upgradeTiers    升级档位（弹药/血量逐档加成）—— 供未来升级模块用
- scrapResult     弹尽分解产物与数量
```

现有硬编码（`CompSentryLifespan.shots / scrapCount` 等）后续应抽成上面的可配置/可升级模型。

---

## 5. 分批实装计划（Phases）

- **Phase 0 —— 骨架与最小可用（已完成）**
  消耗品、一次性迫击炮、信标+蓝光柱、CE 补丁框架、软依赖适配层。

- **Phase 1 —— 通讯终端呼叫 + 派系 + 解锁（已完成）**
  通讯终端界面挂"呼叫战备"命令、白银扣费、统一 `CallIn` 入口；新建固定盟友派系 Super Earth + 科技解锁 `Helldivers_OrbitalSentryTactics`。

- **Phase 2 —— 升级模块**
  通讯终端升级各炮台弹药量/血量（暂缓，见 §3.3）。

- **Phase 3 —— SOS2 飞船空投（进行中）**
  软依赖适配层 `SOS2Compat` + 轨道空投入口已搭（框架）；需按 SOS2 实际 API 补"玩家在轨飞船"判定后实装。保留通讯终端呼叫。

- **Phase 4 —— 炮台补全 + 美术 + 平衡**
  机枪/加特林/自动加农等哨戒；替换用户素材；数值按 HD2 数据平衡。

- **Phase 5 —— 文案 / 故事 / 派系背景**
  派系、研究、炮台的 lore 文案。

---

## 6. 兼容性策略

- **CE（Combat Extended）**：`PatchOperationFindMod` 门控的补丁 + `CECompat` 反射软依赖，dll 在有无 CE 的机器都能加载；CE 具体字段需按版本对齐。
- **性能 / 多线程（RimThreaded 等）**：C# 全用实例状态、无跨 tick 静态缓存。
- **UI / 研究类（ResearchPal 等）**：研究用标准 `ResearchProjectDef`。
- **存储类**：暂不处理。
- **SOS2**：软依赖，未装不阻塞通讯终端呼叫。

---

## 7. 当前待你补充 / 待验证的清单

> 下列是本mod当前的"缺口"项，需你在本机 `dotnet build` + 实机测试后反馈，我据此修正。

### A. 素材 / 数据（你后续补）
- [ ] **3D 迫击炮模型/造型**：做炮塔贴图，替换现占用原版 `BaseTurret`/`BasicCannon` 占位。
- [ ] **绝地潜兵里的数值**：迫击炮弹药量、血量、伤害、射程、生产成本 → 填 Turret 数据表。
- [ ] **蓝色光柱参考图**：替换/增强 `Helldivers_BeaconPillar` 的 Mote 贴图。

### B. 本机编译 + 实机验证（你来做，反馈给我）
- [ ] `cd HelldiversRim/Source && dotnet build`（产物自动拷入 `Assemblies/`）。
- [ ] 炮塔：空投落地→索敌开火→弹尽自毁分解是否正常。
- [ ] 通讯终端：研究 `Helldivers_OrbitalSentryTactics` 后呼叫、白银扣费、落点瞄准是否正常。
- [ ] 派系：开局 Super Earth 是否出现且为盟友。
- [ ] 蓝色光柱 Mote：`color`/`CompGlower` 是否按 1.5 正确解析（报错贴我）。

### C. 需按版本核对的接口（编译/运行报错贴我即可）
- [ ] `Find.Targeter.BeginTargeting` 四参重载、`map.listerThings.AllThings`、`FactionGenerator.NewGeneratedFaction`。
- [ ] `ResearchProjectDef.IsFinished`、`CompPowerTrader.PowerOn`、`Command_Action.Disable`。
- [ ] CE 补丁字段与 Def 名（按你 CE 版本）。
- [ ] SOS2：`kentington.SaveOurShip2` 包名与"玩家在轨飞船"API（框架已留 TODO）。