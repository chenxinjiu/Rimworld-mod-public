# HelldiversRim — 开发计划文档（开发计划）

> 环世界(RimWorld 1.6) × 绝地潜兵2(Helldivers 2) 内容 mod。
> 核心玩法：**绝地潜兵2 的"绿色战备"——空投一次性哨戒炮台**(弹尽自毁)。
> 本文档与 `ARCHITECTURE.md` 配套：前者讲架构与 Phase，本文聚焦**当前缺口、占位符、素材提交清单、下一步执行计划**。

---

## 1. 一句话目标

玩家（殖民地）通过**通讯终端 / 便携信标 /（SOS2）轨道空投**三种方式，花费白银把一座一次性的迫击炮哨戒从 Super Earth 空投到指定落点；哨戒自带弹药、自动索敌开火，**弹尽自毁并掉落废料（钢）**。

---

## 2. 当前完成度（对照 ARCHITECTURE.md）

| Phase | 内容 | 状态 |
|---|---|---|
| Phase 0 | 骨架：消耗品 + 一次性迫击炮 + 信标/光柱 + CE 补丁框架 + 软依赖层 | 完成（代码），待实机测 |
| Phase 1 | 通讯终端呼叫 + 派系结盟 + 科技解锁 | 完成（代码），待实机测 |
| Phase 2 | 升级模块（弹药/血量档位） | 暂缓（按架构约定） |
| Phase 3 | SOS2 轨道空投 | 占位（API 未接） |
| Phase 4 | 炮台补全 + 美术 + 平衡 | 缺口最大 |
| Phase 5 | 文案 / lore | 尚未开始 |

> 结论：**玩法逻辑已约 80% 搭好，真正缺的是「美术素材」和「实机验证」两件事。**

---

## 3. 现在还缺什么（缺口清单）

### A. 必做的事（未做就出问题）

- [ ] **`About/LoadFolders.xml` 缺 `<v1.6>` 条目**（当前只有 v1.4 / v1.5）。
     1.6 下 mod 可能不加载或加载异常 —— **高优先级，应立刻补**：`<v1.6><li>/</li></v1.6>`。
- [ ] **整体实机验证**：编译通过不等于运行正常。核心链路一次都还没在游戏里跑通。
- [ ] **派系图标缺失**：`Helldivers_SuperEarth` 没配 `factionIconPath`，游戏 UI 里派系会缺图标。

### B. 内容/平衡（Phase 4，尚未做）

- [ ] 绿战备炮台目前**只有迫击炮一种**；机枪/加特林/自动加农哨戒待补（需要时再定）。
- [ ] **数值未平衡**：弹量(18)、伤害(30)、射程(48)、白银成本(500)、造价(120钢+2工业) 均未按 HD2/游戏平衡校对。
- [ ] 炮台的可配置数据模型（cost/ammoCapacity/maxHP/upgradeTiers/...）仍是硬编码在 `CompSentryLifespan`，需按 ARCHITECTURE 第4节抽成可配置，方便将来做升级模块。

### C. 冗余 / 一致性

- [x] **两笔重复的研究已合并**：统一为 `Helldivers_OrbitalSentryTactics`（解锁 信标 + 炮台 + 通讯终端），删除多余的 `Helldivers_CommsUplink`。

---

## 4. 占位符（Placeholder）清单

### 4.1 美术占位（现用贴图都是原版/临时代替，**不是自定义美术**）

| Def | 现用 texPath（占位来源） | 应为 |
|---|---|---|
| `Helldivers_MortarSentry`（核心） | 无 graphicData，继承原版 BaseTurretGun | 自绘炮台贴图（base + 顶部炮管 + UI 图标） |
| `Helldivers_Gun_SentryMortar`（炮管） | Things/Weapon_Ranged/BasicCannon（原版） | 迫击炮管贴图 |
| `Helldivers_StratagemBeacon`（信标武器） | Things/Item/Resource/Components/ComponentIndustrial（原版） | 信号枪/信标图标 |
| `Helldivers_Proj_Stratagem`（信标抛射物） | Things/Projectile/SonicImp（原版） | 绿色光点/信标动画 |
| `Helldivers_BeaconPillar`（蓝光柱） | Things/Mote/OrbitalBeamTargeting（原版） | 绝地潜兵蓝光柱 Mote |
| `Helldivers_Stim` | .../MedicineIndustrial | （可用原版凑合，可选自绘） |
| `Helldivers_Sample` | Things/Item/Resource/Steel/Steel | 勘测样本图标 |

> 通讯终端 CommsConsole 用原版通讯终端贴图可接受；RationBar 用 MealSurvivalPack 可接受。

### 4.2 代码 / 配置占位（需要按实际 API 补）

| 位置 | 占位内容 | 说明 |
|---|---|---|
| SOS2Compat.cs 的 PlayerHasOrbitalShip | 恒 return false（TODO） | SOS2 在轨飞船判定**未实现**，先回落通讯终端路径 |
| CECompat.cs 的 AmmoRemaining | 反射探测 4 个候选字段名 | CE 版本差异，探测不到会安全降级 |
| Patches/Helldivers_CE_Patch.xml | Verb_ShootCE / ProjectileCE_Explosive / ammoSet=CE_Autocannon_Round | **起点版**，值需按你的 CE 版本核对 |
| CompSentryLifespan.cs | 数值硬编码（shots/explodeRadius/scrapCount） | 应抽成上述可配置模型 |

---

## 5. 需要你提交的图片 / 美术素材

> RimWorld 贴图规范：`.png`，放在与 texPath 对应的 `Textures/<路径>.png`；
> 常用白底/单色剪影即可被游戏着色（UI 图标常用 64x64 白色剪影，游戏会按 faction/team 上色）。

**核心（决定观感，优先级最高）**
1. **哨戒迫击炮台（绿战备）**：base（基座）+ 顶部炮管两层的贴图，最好带绝地潜兵风格造型。（对应 MortarSentry / Gun_SentryMortar）
2. **绝地潜兵蓝色光柱**：落点信标 Mote 的贴图/参考图（对应 BeaconPillar）。
3. **信标武器（信号枪）图标**：白色剪影（对应 StratagemBeacon）。

**次要（可选但建议）**
4. 派系图标（Super Earth 标志性造型，白色剪影，64x64）。
5. 信标抛射物：绿色光点/弹道贴图（对应 Proj_Stratagem）。
6. 消耗品图标：stim / ration bar / sample 的自定义贴图（可暂时用原版）。

> 版权注意：若使用 HD2 官方素材，直接搬运有授权风险，建议做成致敬风格的自绘造型。

---

## 6. 下一步执行计划（建议顺序）

1. **修 LoadFolders.xml 补 v1.6**（最小改动，消除 1.6 加载风险）。
2. **实机验证核心链路**：空投落地→索敌开火→弹尽自毁→分解回钢；通讯终端扣白银；派系结盟。
3. **补派系图标 + 至少自绘一款炮台贴图**，把最占位的视觉效果替换掉。
4. 核对并**合并两笔重复研究科技**。
5. 抽样核对 CE 补丁值 / 补 SOS2 在轨判定（配合实机）。
6. Phase 4 数值平衡与更多炮台类型（按需）。

---

## 7. 验收标准

- 1.6 下拉入 mod 正常加载、无红字报错。
- 落点召唤→炮台开火→弹尽自毁回钢，全流程在游戏内跑通。
- 通讯终端/信标/SOS2 三种呼叫都生效。
- 无占位视觉（炮台、信标、光柱、派系图标均已上自定义美术）。